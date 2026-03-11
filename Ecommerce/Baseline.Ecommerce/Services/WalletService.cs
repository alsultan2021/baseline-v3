using CMS.DataEngine;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of IWalletService.
/// Manages member wallet balances and transactions using custom module data.
/// </summary>
public class WalletService(
    IInfoProvider<WalletInfo> walletProvider,
    IInfoProvider<WalletTransactionInfo> transactionProvider,
    ICurrencyService currencyService,
    ICurrencyExchangeService currencyExchangeService,
    IOptions<BaselineEcommerceOptions> options,
    IMemoryCache cache,
    ILogger<WalletService> logger) : IWalletService
{
    private readonly BaselineEcommerceOptions _options = options.Value;
    private const string WalletCacheKeyPrefix = "baseline.ecommerce.wallet.";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    #region Wallet Management

    /// <inheritdoc/>
    public async Task<WalletSummary> GetOrCreateWalletAsync(
        int memberId,
        string? walletType = null,
        string? currencyCode = null,
        CancellationToken cancellationToken = default)
    {
        walletType ??= WalletTypes.StoreCredit;
        currencyCode ??= _options.Pricing.DefaultCurrency;

        logger.LogDebug("Getting or creating wallet: MemberId={MemberId}, Type={Type}", memberId, walletType);

        // Try to get existing wallet
        var wallet = await GetWalletByMemberAsync(memberId, walletType, cancellationToken);
        if (wallet != null)
        {
            return await MapToSummaryAsync(wallet, cancellationToken);
        }

        // Get currency ID from code
        var currency = await currencyService.GetCurrencyByCodeAsync(currencyCode, cancellationToken);
        var currencyId = currency?.Id ?? 0;

        // Create new wallet
        var newWallet = new WalletInfo
        {
            WalletGuid = Guid.NewGuid(),
            WalletMemberID = memberId,
            WalletType = walletType,
            WalletCurrencyID = currencyId,
            WalletBalance = 0,
            WalletHeldBalance = 0,
            WalletEnabled = true,
            WalletFrozen = false,
            WalletCreatedWhen = DateTime.UtcNow,
            WalletLastModified = DateTime.UtcNow
        };

        walletProvider.Set(newWallet);

        logger.LogInformation("Created wallet: WalletId={WalletId}, MemberId={MemberId}, Type={Type}",
            newWallet.WalletGuid, memberId, walletType);

        return await MapToSummaryAsync(newWallet, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WalletSummary>> GetMemberWalletsAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var wallets = await walletProvider.Get()
            .WhereEquals(nameof(WalletInfo.WalletMemberID), memberId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var summaries = new List<WalletSummary>();
        foreach (var wallet in wallets)
        {
            summaries.Add(await MapToSummaryAsync(wallet, cancellationToken));
        }
        return summaries;
    }

    /// <inheritdoc/>
    public async Task<WalletSummary?> GetWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{WalletCacheKeyPrefix}{walletId}";

        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheExpiry;
            entry.Size = 1;

            var wallets = await walletProvider.Get()
                .WhereEquals(nameof(WalletInfo.WalletGuid), walletId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var wallet = wallets.FirstOrDefault();
            return wallet != null ? await MapToSummaryAsync(wallet, cancellationToken) : null;
        });
    }

    /// <inheritdoc/>
    public async Task<Money> GetTotalAvailableBalanceAsync(
        int memberId,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var wallets = await GetMemberWalletsAsync(memberId, cancellationToken);
        decimal totalBalance = 0;

        foreach (var wallet in wallets.Where(w => w.IsActive && !w.IsFrozen))
        {
            if (wallet.AvailableBalance.Currency == currencyCode)
            {
                totalBalance += wallet.AvailableBalance.Amount;
            }
            else
            {
                // Convert to target currency
                var converted = await currencyExchangeService.ConvertAsync(
                    wallet.AvailableBalance,
                    currencyCode,
                    cancellationToken);
                totalBalance += converted.Amount;
            }
        }

        return new Money { Amount = totalBalance, Currency = currencyCode };
    }

    #endregion

    #region Transactions

    /// <inheritdoc/>
    public async Task<WalletOperationResult> DepositAsync(
        WalletDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Processing deposit: MemberId={MemberId}, Amount={Amount}",
            request.MemberId, request.Amount);

        try
        {
            var wallet = await GetOrCreateWalletInternalAsync(
                request.MemberId,
                request.WalletType,
                request.CurrencyCode,
                cancellationToken);

            if (wallet.WalletFrozen)
            {
                return WalletOperationResult.Failed("Wallet is frozen", WalletErrorCodes.WalletFrozen);
            }

            // Update balance
            wallet.WalletBalance += request.Amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Deposit,
                TransactionAmount = request.Amount,
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = request.Description ?? "Deposit",
                TransactionReference = request.Reference,
                TransactionStatus = WalletTransactionStatuses.Completed,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogInformation("Deposit completed: WalletId={WalletId}, Amount={Amount}, NewBalance={Balance}",
                wallet.WalletGuid, request.Amount, wallet.WalletBalance);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deposit failed: MemberId={MemberId}", request.MemberId);
            return WalletOperationResult.Failed($"Deposit failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> WithdrawAsync(
        WalletWithdrawalRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Processing withdrawal: MemberId={MemberId}, Amount={Amount}",
            request.MemberId, request.Amount);

        try
        {
            var wallet = await GetWalletByMemberAsync(request.MemberId, request.WalletType, cancellationToken);
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            if (wallet.WalletFrozen)
            {
                return WalletOperationResult.Failed("Wallet is frozen", WalletErrorCodes.WalletFrozen);
            }

            if (request.Amount > wallet.AvailableBalance)
            {
                return WalletOperationResult.Failed("Insufficient balance", WalletErrorCodes.InsufficientFunds);
            }

            // Update balance
            wallet.WalletBalance -= request.Amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Purchase,
                TransactionAmount = -request.Amount,
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = request.Description ?? "Purchase",
                TransactionReference = request.Reference,
                TransactionOrderID = request.OrderId,
                TransactionStatus = WalletTransactionStatuses.Completed,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogInformation("Withdrawal completed: WalletId={WalletId}, Amount={Amount}, NewBalance={Balance}",
                wallet.WalletGuid, request.Amount, wallet.WalletBalance);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Withdrawal failed: MemberId={MemberId}", request.MemberId);
            return WalletOperationResult.Failed($"Withdrawal failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> HoldAsync(
        WalletHoldRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Processing hold: MemberId={MemberId}, Amount={Amount}",
            request.MemberId, request.Amount);

        try
        {
            var wallet = await GetWalletByMemberAsync(request.MemberId, request.WalletType, cancellationToken);
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            if (wallet.WalletFrozen)
            {
                return WalletOperationResult.Failed("Wallet is frozen", WalletErrorCodes.WalletFrozen);
            }

            if (request.Amount > wallet.AvailableBalance)
            {
                return WalletOperationResult.Failed("Insufficient available balance", WalletErrorCodes.InsufficientFunds);
            }

            // Place hold
            wallet.WalletHeldBalance += request.Amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record pending transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Hold,
                TransactionAmount = -request.Amount,
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = "Amount held for purchase",
                TransactionReference = request.Reference,
                TransactionOrderID = request.OrderId,
                TransactionStatus = WalletTransactionStatuses.Pending,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogDebug("Hold placed: WalletId={WalletId}, Amount={Amount}, HeldBalance={HeldBalance}",
                wallet.WalletGuid, request.Amount, wallet.WalletHeldBalance);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Hold failed: MemberId={MemberId}", request.MemberId);
            return WalletOperationResult.Failed($"Hold failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> ReleaseHoldAsync(
        int memberId,
        decimal amount,
        string? reference = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await GetWalletByMemberAsync(memberId, null, cancellationToken);
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            if (amount > wallet.WalletHeldBalance)
            {
                amount = wallet.WalletHeldBalance; // Release only what's held
            }

            // Release hold
            wallet.WalletHeldBalance -= amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record release transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Release,
                TransactionAmount = 0, // No balance change, just held amount release
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = "Hold released",
                TransactionReference = reference,
                TransactionStatus = WalletTransactionStatuses.Completed,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogDebug("Hold released: WalletId={WalletId}, Amount={Amount}", wallet.WalletGuid, amount);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Release hold failed: MemberId={MemberId}", memberId);
            return WalletOperationResult.Failed($"Release hold failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> CaptureHoldAsync(
        int memberId,
        decimal amount,
        int orderId,
        string? reference = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await GetWalletByMemberAsync(memberId, null, cancellationToken);
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            // Determine how much to capture from held balance vs direct withdrawal
            var captureFromHeld = Math.Min(amount, wallet.WalletHeldBalance);
            var directWithdrawal = amount - captureFromHeld;

            logger.LogDebug("CaptureHoldAsync: Amount={Amount}, HeldBalance={HeldBalance}, CaptureFromHeld={CaptureFromHeld}, DirectWithdrawal={DirectWithdrawal}",
                amount, wallet.WalletHeldBalance, captureFromHeld, directWithdrawal);

            // Check if we have sufficient total balance for any direct withdrawal needed
            if (directWithdrawal > 0 && directWithdrawal > wallet.AvailableBalance)
            {
                return WalletOperationResult.Failed("Insufficient balance for capture", WalletErrorCodes.InsufficientFunds);
            }

            // Capture from held balance
            if (captureFromHeld > 0)
            {
                wallet.WalletHeldBalance -= captureFromHeld;

                // Mark any pending hold transactions for this wallet as completed
                var pendingHolds = await transactionProvider.Get()
                    .WhereEquals(nameof(WalletTransactionInfo.TransactionWalletID), wallet.WalletID)
                    .WhereEquals(nameof(WalletTransactionInfo.TransactionType), WalletTransactionTypes.Hold)
                    .WhereEquals(nameof(WalletTransactionInfo.TransactionStatus), WalletTransactionStatuses.Pending)
                    .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

                foreach (var hold in pendingHolds)
                {
                    hold.TransactionStatus = WalletTransactionStatuses.Completed;
                    hold.TransactionOrderID = orderId;
                    transactionProvider.Set(hold);
                }
            }

            // Reduce total balance by full amount
            wallet.WalletBalance -= amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Purchase,
                TransactionAmount = -amount,
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = $"Payment captured for order #{orderId}",
                TransactionReference = reference,
                TransactionOrderID = orderId,
                TransactionStatus = WalletTransactionStatuses.Completed,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogInformation("Capture completed: WalletId={WalletId}, Amount={Amount}, OrderId={OrderId}, NewBalance={NewBalance}",
                wallet.WalletGuid, amount, orderId, wallet.WalletBalance);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Capture failed: MemberId={MemberId}, OrderId={OrderId}", memberId, orderId);
            return WalletOperationResult.Failed($"Capture failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> RefundAsync(
        int memberId,
        decimal amount,
        int orderId,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Processing refund: MemberId={MemberId}, Amount={Amount}, OrderId={OrderId}",
            memberId, amount, orderId);

        try
        {
            var wallet = await GetOrCreateWalletInternalAsync(memberId, null, null, cancellationToken);

            // Add refund amount to balance
            wallet.WalletBalance += amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Refund,
                TransactionAmount = amount,
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = description ?? $"Refund for order #{orderId}",
                TransactionOrderID = orderId,
                TransactionStatus = WalletTransactionStatuses.Completed,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogInformation("Refund completed: WalletId={WalletId}, Amount={Amount}, OrderId={OrderId}",
                wallet.WalletGuid, amount, orderId);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refund failed: MemberId={MemberId}, OrderId={OrderId}", memberId, orderId);
            return WalletOperationResult.Failed($"Refund failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    #endregion

    #region Transaction History

    /// <inheritdoc/>
    public async Task<PagedResult<WalletTransactionSummary>> GetTransactionHistoryAsync(
        int memberId,
        int page = 1,
        int pageSize = 20,
        string? walletType = null,
        CancellationToken cancellationToken = default)
    {
        var wallets = await walletProvider.Get()
            .WhereEquals(nameof(WalletInfo.WalletMemberID), memberId)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var walletIds = walletType != null
            ? wallets.Where(w => w.WalletType == walletType).Select(w => w.WalletID).ToList()
            : wallets.Select(w => w.WalletID).ToList();

        if (walletIds.Count == 0)
        {
            return new PagedResult<WalletTransactionSummary>
            {
                Items = [],
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        // Get total count
        var allTransactions = await transactionProvider.Get()
            .WhereIn(nameof(WalletTransactionInfo.TransactionWalletID), walletIds)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
        var totalCount = allTransactions.Count();

        // Get paged transactions
        var transactions = await transactionProvider.Get()
            .WhereIn(nameof(WalletTransactionInfo.TransactionWalletID), walletIds)
            .OrderByDescending(nameof(WalletTransactionInfo.TransactionCreatedWhen))
            .Page(page - 1, pageSize)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        // Get wallet currencies for mapping
        var walletCurrencies = wallets.ToDictionary(
            w => w.WalletID,
            w => w.WalletCurrencyID);

        var summaries = new List<WalletTransactionSummary>();
        foreach (var transaction in transactions)
        {
            var currencyCode = await GetCurrencyCodeAsync(walletCurrencies.GetValueOrDefault(transaction.TransactionWalletID), cancellationToken);
            summaries.Add(MapToTransactionSummary(transaction, currencyCode));
        }

        return new PagedResult<WalletTransactionSummary>
        {
            Items = summaries,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<WalletTransactionSummary?> GetTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var transactions = await transactionProvider.Get()
            .WhereEquals(nameof(WalletTransactionInfo.TransactionGuid), transactionId)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var transaction = transactions.FirstOrDefault();
        if (transaction == null) return null;

        // Get wallet for currency
        var wallets = await walletProvider.Get()
            .WhereEquals(nameof(WalletInfo.WalletID), transaction.TransactionWalletID)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        var wallet = wallets.FirstOrDefault();
        var currencyCode = wallet != null
            ? await GetCurrencyCodeAsync(wallet.WalletCurrencyID, cancellationToken)
            : "USD";

        return MapToTransactionSummary(transaction, currencyCode);
    }

    #endregion

    #region Validation

    /// <inheritdoc/>
    public async Task<bool> HasSufficientBalanceAsync(
        int memberId,
        decimal amount,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var totalBalance = await GetTotalAvailableBalanceAsync(memberId, currencyCode, cancellationToken);
        return totalBalance.Amount >= amount;
    }

    /// <inheritdoc/>
    public async Task<WalletValidationResult> ValidateWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        var wallet = await GetWalletAsync(walletId, cancellationToken);
        if (wallet == null)
        {
            return WalletValidationResult.Invalid("Wallet not found");
        }

        var errors = new List<string>();

        if (!wallet.IsActive)
        {
            errors.Add("Wallet is inactive");
        }

        if (wallet.IsFrozen)
        {
            errors.Add("Wallet is frozen");
        }

        if (wallet.ExpiresAt.HasValue && wallet.ExpiresAt.Value < DateTime.UtcNow)
        {
            errors.Add("Wallet has expired");
        }

        return errors.Count == 0
            ? WalletValidationResult.Valid()
            : WalletValidationResult.Invalid([.. errors]);
    }

    #endregion

    #region Admin Operations

    /// <inheritdoc/>
    public async Task<WalletOperationResult> AdjustBalanceAsync(
        WalletAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallets = await walletProvider.Get()
                .WhereEquals(nameof(WalletInfo.WalletGuid), request.WalletId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var wallet = wallets.FirstOrDefault();
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            wallet.WalletBalance += request.Amount;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            // Record admin adjustment transaction
            var transaction = new WalletTransactionInfo
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = wallet.WalletID,
                TransactionType = WalletTransactionTypes.Adjustment,
                TransactionAmount = request.Amount,
                TransactionBalanceAfter = wallet.WalletBalance,
                TransactionDescription = $"Admin adjustment: {request.Reason}",
                TransactionReference = $"ADMIN-{request.AdminUserId}",
                TransactionCreatedBy = request.AdminUserId,
                TransactionStatus = WalletTransactionStatuses.Completed,
                TransactionCreatedWhen = DateTime.UtcNow
            };
            transactionProvider.Set(transaction);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogWarning("Admin balance adjustment: WalletId={WalletId}, Amount={Amount}, AdminUserId={AdminUserId}, Reason={Reason}",
                wallet.WalletGuid, request.Amount, request.AdminUserId, request.Reason);

            return WalletOperationResult.Succeeded(
                transaction.TransactionGuid,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Admin adjustment failed: WalletId={WalletId}", request.WalletId);
            return WalletOperationResult.Failed($"Adjustment failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> FreezeWalletAsync(
        Guid walletId,
        string reason,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallets = await walletProvider.Get()
                .WhereEquals(nameof(WalletInfo.WalletGuid), walletId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var wallet = wallets.FirstOrDefault();
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            wallet.WalletFrozen = true;
            wallet.WalletFreezeReason = reason;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogWarning("Wallet frozen: WalletId={WalletId}, AdminUserId={AdminUserId}, Reason={Reason}",
                walletId, adminUserId, reason);

            return WalletOperationResult.Succeeded(
                Guid.Empty,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Freeze wallet failed: WalletId={WalletId}", walletId);
            return WalletOperationResult.Failed($"Freeze failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> UnfreezeWalletAsync(
        Guid walletId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var wallets = await walletProvider.Get()
                .WhereEquals(nameof(WalletInfo.WalletGuid), walletId)
                .TopN(1)
                .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

            var wallet = wallets.FirstOrDefault();
            if (wallet == null)
            {
                return WalletOperationResult.Failed("Wallet not found", WalletErrorCodes.WalletNotFound);
            }

            wallet.WalletFrozen = false;
            wallet.WalletFreezeReason = null;
            wallet.WalletLastModified = DateTime.UtcNow;
            walletProvider.Set(wallet);

            InvalidateWalletCache(wallet.WalletGuid);

            logger.LogWarning("Wallet unfrozen: WalletId={WalletId}, AdminUserId={AdminUserId}",
                walletId, adminUserId);

            return WalletOperationResult.Succeeded(
                Guid.Empty,
                wallet.WalletBalance,
                wallet.AvailableBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unfreeze wallet failed: WalletId={WalletId}", walletId);
            return WalletOperationResult.Failed($"Unfreeze failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    #endregion

    #region Private Helpers

    private async Task<WalletInfo?> GetWalletByMemberAsync(
        int memberId,
        string? walletType,
        CancellationToken cancellationToken)
    {
        var query = walletProvider.Get()
            .WhereEquals(nameof(WalletInfo.WalletMemberID), memberId);

        if (!string.IsNullOrEmpty(walletType))
        {
            query = query.WhereEquals(nameof(WalletInfo.WalletType), walletType);
        }

        var wallets = await query
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return wallets.FirstOrDefault();
    }

    private async Task<WalletInfo> GetOrCreateWalletInternalAsync(
        int memberId,
        string? walletType,
        string? currencyCode,
        CancellationToken cancellationToken)
    {
        walletType ??= WalletTypes.StoreCredit;
        currencyCode ??= _options.Pricing.DefaultCurrency;

        var wallet = await GetWalletByMemberAsync(memberId, walletType, cancellationToken);
        if (wallet != null)
        {
            return wallet;
        }

        // Get currency ID from code
        var currency = await currencyService.GetCurrencyByCodeAsync(currencyCode, cancellationToken);
        var currencyId = currency?.Id ?? 0;

        var newWallet = new WalletInfo
        {
            WalletGuid = Guid.NewGuid(),
            WalletMemberID = memberId,
            WalletType = walletType,
            WalletCurrencyID = currencyId,
            WalletBalance = 0,
            WalletHeldBalance = 0,
            WalletEnabled = true,
            WalletFrozen = false,
            WalletCreatedWhen = DateTime.UtcNow,
            WalletLastModified = DateTime.UtcNow
        };

        walletProvider.Set(newWallet);
        return newWallet;
    }

    private void InvalidateWalletCache(Guid walletId)
    {
        cache.Remove($"{WalletCacheKeyPrefix}{walletId}");
    }

    private async Task<string> GetCurrencyCodeAsync(int currencyId, CancellationToken cancellationToken)
    {
        if (currencyId == 0) return _options.Pricing.DefaultCurrency;

        var currency = await currencyService.GetCurrencyByIdAsync(currencyId, cancellationToken);
        return currency?.Code ?? _options.Pricing.DefaultCurrency;
    }

    private async Task<WalletSummary> MapToSummaryAsync(WalletInfo wallet, CancellationToken cancellationToken)
    {
        var currencyCode = await GetCurrencyCodeAsync(wallet.WalletCurrencyID, cancellationToken);

        return new WalletSummary
        {
            WalletId = wallet.WalletGuid,
            WalletType = wallet.WalletType,
            Balance = new Money { Amount = wallet.WalletBalance, Currency = currencyCode },
            AvailableBalance = new Money { Amount = wallet.AvailableBalance, Currency = currencyCode },
            HeldBalance = new Money { Amount = wallet.WalletHeldBalance, Currency = currencyCode },
            IsActive = wallet.WalletEnabled,
            IsFrozen = wallet.WalletFrozen,
            ExpiresAt = wallet.WalletExpiresAt,
            CreatedAt = wallet.WalletCreatedWhen
        };
    }

    private static WalletTransactionSummary MapToTransactionSummary(WalletTransactionInfo transaction, string currencyCode) => new()
    {
        TransactionId = transaction.TransactionGuid,
        Type = transaction.TransactionType,
        Amount = new Money { Amount = transaction.TransactionAmount, Currency = currencyCode },
        BalanceAfter = new Money { Amount = transaction.TransactionBalanceAfter, Currency = currencyCode },
        Description = transaction.TransactionDescription ?? string.Empty,
        Reference = transaction.TransactionReference,
        Status = transaction.TransactionStatus,
        CreatedAt = transaction.TransactionCreatedWhen
    };

    #endregion
}
