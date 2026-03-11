using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce;

/// <summary>
/// No-op implementation of IOrderNotificationService for sites that don't need order emails.
/// </summary>
internal sealed class NoOpOrderNotificationService(ILogger<NoOpOrderNotificationService> logger) : IOrderNotificationService
{
    public Task SendOrderConfirmationAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Order confirmation not sent (no-op): {OrderNumber}", orderNumber);
        return Task.CompletedTask;
    }

    public Task SendOrderStatusUpdateAsync(string orderNumber, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Order status update not sent (no-op): {OrderNumber} -> {Status}", orderNumber, newStatus);
        return Task.CompletedTask;
    }

    public Task SendShippingNotificationAsync(string orderNumber, string trackingNumber, string? carrier = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Shipping notification not sent (no-op): {OrderNumber}, Tracking: {TrackingNumber}", orderNumber, trackingNumber);
        return Task.CompletedTask;
    }

    public Task SendOrderCancelledAsync(string orderNumber, string? reason = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Order cancelled notification not sent (no-op): {OrderNumber}", orderNumber);
        return Task.CompletedTask;
    }

    public Task SendRefundConfirmationAsync(string orderNumber, Money refundAmount, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Refund confirmation not sent (no-op): {OrderNumber}, Amount: {Amount}", orderNumber, refundAmount.Amount);
        return Task.CompletedTask;
    }
}
