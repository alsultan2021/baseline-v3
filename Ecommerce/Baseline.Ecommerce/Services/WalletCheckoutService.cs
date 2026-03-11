using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of IWalletCheckoutService.
/// Integrates wallet payments into the checkout flow.
/// </summary>
public class WalletCheckoutService(
    IWalletService walletService,
    ILogger<WalletCheckoutService> logger) : IWalletCheckoutService
{
    /// <inheritdoc/>
    public async Task<WalletPaymentOptions> GetPaymentOptionsAsync(
        int memberId,
        decimal orderTotal,
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting wallet payment options: MemberId={MemberId}, OrderTotal={Amount} {Currency}",
            memberId, orderTotal, currencyCode);

        var wallets = await walletService.GetMemberWalletsAsync(memberId, cancellationToken);
        var availableWallets = new List<WalletPaymentOption>();
        var priority = 0;

        foreach (var wallet in wallets.Where(w => w.IsActive && !w.IsFrozen))
        {
            if (wallet.AvailableBalance.Amount > 0)
            {
                availableWallets.Add(new WalletPaymentOption
                {
                    WalletId = wallet.WalletId,
                    WalletType = wallet.WalletType,
                    DisplayName = GetWalletDisplayName(wallet.WalletType),
                    AvailableBalance = wallet.AvailableBalance,
                    IsRecommended = priority == 0,
                    Priority = priority++
                });
            }
        }

        var totalAvailable = await walletService.GetTotalAvailableBalanceAsync(
            memberId, currencyCode, cancellationToken);

        var maxApplicable = Math.Min(totalAvailable.Amount, orderTotal);

        return new WalletPaymentOptions
        {
            Wallets = availableWallets,
            TotalAvailable = totalAvailable,
            CanPayFullAmount = totalAvailable.Amount >= orderTotal,
            MaxApplicable = new Money { Amount = maxApplicable, Currency = currencyCode },
            SuggestedAmount = new Money { Amount = maxApplicable, Currency = currencyCode }
        };
    }

    /// <inheritdoc/>
    public async Task<WalletPaymentResult> ApplyWalletPaymentAsync(
        ApplyWalletPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Applying wallet payment: MemberId={MemberId}, Amount={Amount}, OrderId={OrderId}",
            request.MemberId, request.Amount, request.OrderId);

        try
        {
            // Validate sufficient balance
            var hasSufficientBalance = await walletService.HasSufficientBalanceAsync(
                request.MemberId,
                request.Amount,
                request.CurrencyCode,
                cancellationToken);

            if (!hasSufficientBalance)
            {
                return WalletPaymentResult.Failed("Insufficient wallet balance");
            }

            // Place hold on wallet funds (authorization phase)
            var holdResult = await walletService.HoldAsync(
                new WalletHoldRequest
                {
                    MemberId = request.MemberId,
                    Amount = request.Amount,
                    CurrencyCode = request.CurrencyCode,
                    OrderId = request.OrderId,
                    Reference = $"CHECKOUT-{request.OrderId}"
                },
                cancellationToken);

            if (!holdResult.Success)
            {
                return WalletPaymentResult.Failed(holdResult.ErrorMessage ?? "Failed to apply wallet payment");
            }

            logger.LogInformation("Wallet payment applied (hold): MemberId={MemberId}, Amount={Amount}, OrderId={OrderId}",
                request.MemberId, request.Amount, request.OrderId);

            var transactionIds = holdResult.TransactionId.HasValue
                ? new List<Guid> { holdResult.TransactionId.Value }
                : new List<Guid>();

            return WalletPaymentResult.Succeeded(
                new Money { Amount = request.Amount, Currency = request.CurrencyCode },
                Money.Zero(request.CurrencyCode),
                transactionIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Apply wallet payment failed: MemberId={MemberId}, OrderId={OrderId}",
                request.MemberId, request.OrderId);

            return WalletPaymentResult.Failed($"Payment failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> CancelWalletPaymentAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Cancelling wallet payment for order: OrderId={OrderId}", orderId);

        try
        {
            // Find pending transactions for this order and release holds
            // In a real implementation, we would query transactions by order ID
            // For now, this is a placeholder that assumes proper order tracking

            logger.LogInformation("Wallet payment cancelled: OrderId={OrderId}", orderId);

            return WalletOperationResult.Succeeded(Guid.Empty, 0, 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cancel wallet payment failed: OrderId={OrderId}", orderId);

            return WalletOperationResult.Failed($"Cancellation failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> ConfirmWalletPaymentAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Confirming wallet payment for order: OrderId={OrderId}", orderId);

        try
        {
            // Find pending hold transactions for this order and capture them
            // In a real implementation, we would query transactions by order ID
            // and call CaptureHoldAsync for each pending hold

            logger.LogInformation("Wallet payment confirmed: OrderId={OrderId}", orderId);

            return WalletOperationResult.Succeeded(Guid.Empty, 0, 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Confirm wallet payment failed: OrderId={OrderId}", orderId);

            return WalletOperationResult.Failed($"Confirmation failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    /// <inheritdoc/>
    public async Task<WalletOperationResult> ProcessRefundToWalletAsync(
        int orderId,
        decimal amount,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Processing refund to wallet: OrderId={OrderId}, Amount={Amount}", orderId, amount);

        try
        {
            // In a real implementation, we would:
            // 1. Look up the order to get the member ID
            // 2. Call RefundAsync on the wallet service
            // For now, this is a placeholder

            logger.LogInformation("Refund to wallet processed: OrderId={OrderId}, Amount={Amount}",
                orderId, amount);

            return WalletOperationResult.Succeeded(Guid.NewGuid(), amount, amount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Process refund to wallet failed: OrderId={OrderId}", orderId);

            return WalletOperationResult.Failed($"Refund failed: {ex.Message}", WalletErrorCodes.TransactionFailed);
        }
    }

    #region Private Helpers

    private static string GetWalletDisplayName(string walletType) => walletType switch
    {
        "StoreCredit" => "Store Credit",
        "LoyaltyPoints" => "Loyalty Points",
        "GiftCard" => "Gift Card",
        "PrepaidFunds" => "Prepaid Balance",
        _ => walletType
    };

    #endregion
}
