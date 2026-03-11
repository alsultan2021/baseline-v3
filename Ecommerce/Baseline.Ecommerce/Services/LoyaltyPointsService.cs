using CMS.Commerce;
using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of <see cref="ILoyaltyPointsService"/>.
/// Extends wallet functionality with earning rules, tiers, and redemption options.
/// </summary>
public class LoyaltyPointsService(
    IWalletService walletService,
    IInfoProvider<OrderInfo> orderProvider,
    IOptions<LoyaltyPointsOptions> loyaltyOptions,
    IOptions<BaselineEcommerceOptions> ecommerceOptions,
    IMemoryCache cache,
    ILogger<LoyaltyPointsService> logger) : ILoyaltyPointsService
{
    private readonly LoyaltyPointsOptions _loyaltyOptions = loyaltyOptions.Value;
    private readonly BaselineEcommerceOptions _ecommerceOptions = ecommerceOptions.Value;
    private const string TierCacheKeyPrefix = "baseline.ecommerce.loyalty.tier.";
    private static readonly TimeSpan TierCacheExpiry = TimeSpan.FromMinutes(30);

    #region ILoyaltyPointsService Implementation

    /// <inheritdoc/>
    public async Task<LoyaltyPointsBalance> GetBalanceAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {

        var wallet = await walletService.GetOrCreateWalletAsync(
            memberId,
            WalletTypes.LoyaltyPoints,
            _ecommerceOptions.Pricing.DefaultCurrency,
            cancellationToken);

        // Get transaction history for pending/expiring calculations
        var transactionHistory = await walletService.GetTransactionHistoryAsync(
            memberId,
            page: 1,
            pageSize: 100,
            walletType: WalletTypes.LoyaltyPoints,
            cancellationToken: cancellationToken);

        // Calculate pending points (transactions in Pending status)
        int pendingPoints = CalculatePendingPoints(transactionHistory.Items);

        // Calculate points expiring this month
        int expiringPoints = CalculateExpiringPoints(transactionHistory.Items);

        // Calculate yearly earned/redeemed
        var (earnedThisYear, redeemedThisYear) = CalculateYearlyPoints(transactionHistory.Items);

        // Calculate equivalent value
        var pointValue = await GetPointValueAsync(_ecommerceOptions.Pricing.DefaultCurrency, cancellationToken);
        var equivalentValue = new Money
        {
            Amount = (int)wallet.AvailableBalance.Amount * pointValue,
            Currency = _ecommerceOptions.Pricing.DefaultCurrency
        };

        return new LoyaltyPointsBalance
        {
            TotalPoints = (int)wallet.Balance.Amount,
            AvailablePoints = (int)wallet.AvailableBalance.Amount,
            PendingPoints = pendingPoints,
            PointsExpiringThisMonth = expiringPoints,
            EquivalentValue = equivalentValue,
            PointsEarnedThisYear = earnedThisYear,
            PointsRedeemedThisYear = redeemedThisYear
        };
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> AwardPointsForOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {


        try
        {
            // Get order details
            var order = (await orderProvider.Get()
                .WhereEquals(nameof(OrderInfo.OrderID), orderId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken))
                .FirstOrDefault();

            if (order == null)
            {
                logger.LogWarning("Order {OrderId} not found for points award", orderId);
                return WalletOperationResult.Failed("Order not found", WalletErrorCodes.OrderNotFound);
            }

            if (order.OrderCustomerID <= 0)
            {
                logger.LogDebug("Order {OrderId} is a guest order, skipping points award", orderId);
                return WalletOperationResult.Failed("Guest orders do not earn points", WalletErrorCodes.InvalidMember);
            }

            // Calculate points to award
            var earnablePoints = await CalculateEarnablePointsForOrderAsync(order, cancellationToken);

            if (earnablePoints <= 0)
            {
                logger.LogDebug("No points to award for order {OrderId}", orderId);
                return WalletOperationResult.Succeeded(Guid.Empty, 0, 0);
            }

            // Deposit points to member's loyalty wallet
            var result = await walletService.DepositAsync(new WalletDepositRequest
            {
                MemberId = order.OrderCustomerID,
                WalletType = WalletTypes.LoyaltyPoints,
                Amount = earnablePoints,
                CurrencyCode = _ecommerceOptions.Pricing.DefaultCurrency,
                Description = $"Points earned from order #{order.OrderNumber}",
                Reference = order.OrderNumber
            }, cancellationToken);

            if (result.Success)
            {
                logger.LogInformation(
                    "Awarded {Points} loyalty points to customer {CustomerId} for order {OrderNumber}",
                    earnablePoints, order.OrderCustomerID, order.OrderNumber);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to award points for order {OrderId}", orderId);
            return WalletOperationResult.Failed($"Failed to award points: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> RedeemPointsAsync(
        int memberId,
        int points,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Redeeming {Points} loyalty points for member {MemberId} on order {OrderId}",
            points, memberId, orderId);

        if (points <= 0)
        {
            return WalletOperationResult.Failed("Points must be greater than zero", WalletErrorCodes.InvalidAmount);
        }

        // Check available balance
        var balance = await GetBalanceAsync(memberId, cancellationToken);
        if (points > balance.AvailablePoints)
        {
            return WalletOperationResult.Failed(
                $"Insufficient points. Available: {balance.AvailablePoints}",
                WalletErrorCodes.InsufficientFunds);
        }

        // Withdraw points from loyalty wallet
        var result = await walletService.WithdrawAsync(new WalletWithdrawalRequest
        {
            MemberId = memberId,
            WalletType = WalletTypes.LoyaltyPoints,
            Amount = points,
            Description = $"Points redeemed for order #{orderId}",
            Reference = orderId.ToString(),
            OrderId = orderId
        }, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation(
                "Redeemed {Points} loyalty points for member {MemberId} on order {OrderId}",
                points, memberId, orderId);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<int> CalculateEarnablePointsAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        if (cart == null || cart.Items.Count == 0)
        {
            return 0;
        }

        // Get member tier for multiplier (use UserId from cart)
        var tier = cart.UserId.HasValue && cart.UserId.Value > 0
            ? await GetMemberTierAsync(cart.UserId.Value, cancellationToken)
            : GetDefaultTier();

        // Calculate base points from cart total
        var basePoints = (int)(cart.Totals.Total.Amount * _loyaltyOptions.PointsPerCurrencyUnit);

        // Apply tier multiplier
        var totalPoints = (int)(basePoints * tier.PointsMultiplier);

        logger.LogDebug(
            "Calculated {Points} earnable points for cart (base: {BasePoints}, multiplier: {Multiplier})",
            totalPoints, basePoints, tier.PointsMultiplier);

        return totalPoints;
    }

    /// <inheritdoc/>
    public Task<Money> CalculateRedemptionValueAsync(
        int points,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var pointValue = _loyaltyOptions.PointValueInCurrency;
        var value = new Money
        {
            Amount = points * pointValue,
            Currency = currencyCode
        };

        logger.LogDebug(
            "Calculated redemption value: {Points} points = {Value} {Currency}",
            points, value.Amount, currencyCode);

        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public async Task<LoyaltyTier> GetMemberTierAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{TierCacheKeyPrefix}{memberId}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TierCacheExpiry;
            entry.Size = 1;

            var balance = await GetBalanceAsync(memberId, cancellationToken);
            return DetermineTier(balance.PointsEarnedThisYear);
        }) ?? GetDefaultTier();
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> AwardBonusPointsAsync(
        AwardBonusPointsRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug(
            "Awarding {Points} bonus points to member {MemberId} for {Reason}",
            request.Points, request.MemberId, request.Reason);

        if (request.Points <= 0)
        {
            return WalletOperationResult.Failed("Points must be greater than zero", WalletErrorCodes.InvalidAmount);
        }

        var result = await walletService.DepositAsync(new WalletDepositRequest
        {
            MemberId = request.MemberId,
            WalletType = WalletTypes.LoyaltyPoints,
            Amount = request.Points,
            CurrencyCode = _ecommerceOptions.Pricing.DefaultCurrency,
            Description = $"Bonus points: {request.Reason}",
            Reference = request.BonusCategory
        }, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation(
                "Awarded {Points} bonus points to member {MemberId}: {Reason}",
                request.Points, request.MemberId, request.Reason);

            // Invalidate tier cache since points changed
            cache.Remove($"{TierCacheKeyPrefix}{request.MemberId}");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<decimal> GetPointsPerCurrencyUnitAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var tier = await GetMemberTierAsync(memberId, cancellationToken);
        return _loyaltyOptions.PointsPerCurrencyUnit * tier.PointsMultiplier;
    }

    /// <inheritdoc/>
    public Task<decimal> GetPointValueAsync(
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        // For now, point value is constant regardless of currency
        // Future enhancement: support different values per currency
        return Task.FromResult(_loyaltyOptions.PointValueInCurrency);
    }

    #endregion

    #region Private Helpers

    private async Task<int> CalculateEarnablePointsForOrderAsync(
        OrderInfo order,
        CancellationToken cancellationToken)
    {
        // Get member tier for multiplier (use CustomerID as member proxy)
        var tier = order.OrderCustomerID > 0
            ? await GetMemberTierAsync(order.OrderCustomerID, cancellationToken)
            : GetDefaultTier();

        // Calculate base points from order total
        var basePoints = (int)(order.OrderGrandTotal * _loyaltyOptions.PointsPerCurrencyUnit);

        // Apply tier multiplier
        return (int)(basePoints * tier.PointsMultiplier);
    }

    private LoyaltyTier DetermineTier(int pointsEarnedThisYear)
    {
        // Find the highest tier the member qualifies for
        foreach (var tier in _loyaltyOptions.Tiers.OrderByDescending(t => t.MinimumPoints))
        {
            if (pointsEarnedThisYear >= tier.MinimumPoints)
            {
                // Calculate points to next tier
                var nextTier = _loyaltyOptions.Tiers
                    .Where(t => t.Level > tier.Level)
                    .OrderBy(t => t.Level)
                    .FirstOrDefault();

                var pointsToNext = nextTier != null
                    ? nextTier.MinimumPoints - pointsEarnedThisYear
                    : 0;

                return tier with { PointsToNextTier = Math.Max(0, pointsToNext) };
            }
        }

        return GetDefaultTier();
    }

    private LoyaltyTier GetDefaultTier() => _loyaltyOptions.Tiers
        .OrderBy(t => t.Level)
        .FirstOrDefault()
        ?? new LoyaltyTier
        {
            Name = "Member",
            Level = 1,
            PointsMultiplier = 1.0m,
            MinimumPoints = 0,
            Benefits = ["Earn 1 point per $1 spent"]
        };

    private static int CalculatePendingPoints(IEnumerable<WalletTransactionSummary> transactions)
    {
        return (int)transactions
            .Where(t => t.Status == WalletTransactionStatuses.Pending && t.Amount.Amount > 0)
            .Sum(t => t.Amount.Amount);
    }

    private static int CalculateExpiringPoints(IEnumerable<WalletTransactionSummary> transactions)
    {
        var endOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1)
            .AddMonths(1)
            .AddDays(-1);

        // This would require an expiration date on transactions, which we don't have yet
        // For now, return 0 - this can be enhanced with a proper expiration system
        return 0;
    }

    private static (int earned, int redeemed) CalculateYearlyPoints(IEnumerable<WalletTransactionSummary> transactions)
    {
        var startOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        var thisYearTransactions = transactions.Where(t => t.CreatedAt >= startOfYear);

        int earned = (int)thisYearTransactions
            .Where(t => t.Amount.Amount > 0)
            .Sum(t => t.Amount.Amount);

        int redeemed = (int)Math.Abs(thisYearTransactions
            .Where(t => t.Amount.Amount < 0)
            .Sum(t => t.Amount.Amount));

        return (earned, redeemed);
    }

    #endregion
}

/// <summary>
/// Configuration options for the loyalty points program.
/// </summary>
public class LoyaltyPointsOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Baseline:Ecommerce:LoyaltyPoints";

    /// <summary>
    /// Base points earned per currency unit (e.g., 1 point per $1).
    /// </summary>
    public decimal PointsPerCurrencyUnit { get; set; } = 1.0m;

    /// <summary>
    /// Monetary value of one point in the default currency.
    /// </summary>
    public decimal PointValueInCurrency { get; set; } = 0.01m;

    /// <summary>
    /// Whether loyalty points are enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Available loyalty tiers.
    /// </summary>
    public List<LoyaltyTier> Tiers { get; set; } =
    [
        new LoyaltyTier
        {
            Name = "Bronze",
            Level = 1,
            PointsMultiplier = 1.0m,
            MinimumPoints = 0,
            Benefits = ["Earn 1 point per $1 spent", "Member-only promotions"]
        },
        new LoyaltyTier
        {
            Name = "Silver",
            Level = 2,
            PointsMultiplier = 1.25m,
            MinimumPoints = 500,
            Benefits = ["Earn 1.25 points per $1 spent", "Free shipping on orders $50+", "Early access to sales"]
        },
        new LoyaltyTier
        {
            Name = "Gold",
            Level = 3,
            PointsMultiplier = 1.5m,
            MinimumPoints = 1500,
            Benefits = ["Earn 1.5 points per $1 spent", "Free shipping on all orders", "Priority customer service"]
        },
        new LoyaltyTier
        {
            Name = "Platinum",
            Level = 4,
            PointsMultiplier = 2.0m,
            MinimumPoints = 5000,
            Benefits = ["Earn 2 points per $1 spent", "Free express shipping", "Exclusive member events", "Birthday bonus points"]
        }
    ];
}
