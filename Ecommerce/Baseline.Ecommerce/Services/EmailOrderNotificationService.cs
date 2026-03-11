using CMS.Commerce;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;
using XbKOrderNotificationService = CMS.Commerce.IOrderNotificationService;

namespace Baseline.Ecommerce;

/// <summary>
/// Email-based implementation of <see cref="IOrderNotificationService"/>.
/// Delegates to Kentico Commerce's built-in notification system
/// (<see cref="CMS.Commerce.IOrderNotificationService"/>).
/// </summary>
public class EmailOrderNotificationService(
    XbKOrderNotificationService xbkOrderNotificationService,
    IInfoProvider<OrderInfo> orderInfoProvider,
    ILogger<EmailOrderNotificationService> logger) : IOrderNotificationService
{
    /// <inheritdoc/>
    public async Task SendOrderConfirmationAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Sending order confirmation for order: {OrderNumber}", orderNumber);

        var order = await GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order == null)
        {
            logger.LogWarning("Cannot send order confirmation - order not found: {OrderNumber}", orderNumber);
            return;
        }

        try
        {
            await xbkOrderNotificationService.SendNotification(order.OrderID, cancellationToken);
            logger.LogInformation("Order confirmation sent for order: {OrderNumber}", orderNumber);
        }
        catch (OrderNotificationSendException ex)
        {
            logger.LogError(ex, "Failed to send order confirmation for order: {OrderNumber}", orderNumber);
        }
    }

    /// <inheritdoc/>
    public async Task SendOrderStatusUpdateAsync(string orderNumber, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Sending order status update for order: {OrderNumber}, New Status: {Status}", orderNumber, newStatus);

        var order = await GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order == null)
        {
            logger.LogWarning("Cannot send status update - order not found: {OrderNumber}", orderNumber);
            return;
        }

        try
        {
            await xbkOrderNotificationService.SendNotification(order.OrderID, cancellationToken);
            logger.LogInformation("Order status update notification sent for order: {OrderNumber}", orderNumber);
        }
        catch (OrderNotificationSendException ex)
        {
            logger.LogError(ex, "Failed to send status update notification for order: {OrderNumber}", orderNumber);
        }
    }

    /// <inheritdoc/>
    public async Task SendShippingNotificationAsync(string orderNumber, string trackingNumber, string? carrier = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Sending shipping notification for order: {OrderNumber}, Tracking: {TrackingNumber}", orderNumber, trackingNumber);

        var order = await GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order == null)
        {
            logger.LogWarning("Cannot send shipping notification - order not found: {OrderNumber}", orderNumber);
            return;
        }

        try
        {
            await xbkOrderNotificationService.SendNotification(order.OrderID, cancellationToken);
            logger.LogInformation("Shipping notification sent for order: {OrderNumber}", orderNumber);
        }
        catch (OrderNotificationSendException ex)
        {
            logger.LogError(ex, "Failed to send shipping notification for order: {OrderNumber}", orderNumber);
        }
    }

    /// <inheritdoc/>
    public async Task SendOrderCancelledAsync(string orderNumber, string? reason = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Sending order cancellation notification for order: {OrderNumber}", orderNumber);

        var order = await GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order == null)
        {
            logger.LogWarning("Cannot send cancellation notification - order not found: {OrderNumber}", orderNumber);
            return;
        }

        try
        {
            await xbkOrderNotificationService.SendNotification(order.OrderID, cancellationToken);
            logger.LogInformation("Order cancellation notification sent for order: {OrderNumber}", orderNumber);
        }
        catch (OrderNotificationSendException ex)
        {
            logger.LogError(ex, "Failed to send cancellation notification for order: {OrderNumber}", orderNumber);
        }
    }

    /// <inheritdoc/>
    public async Task SendRefundConfirmationAsync(string orderNumber, Money refundAmount, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Sending refund confirmation for order: {OrderNumber}, Amount: {Amount} {Currency}",
            orderNumber, refundAmount.Amount, refundAmount.Currency);

        var order = await GetOrderByNumberAsync(orderNumber, cancellationToken);
        if (order == null)
        {
            logger.LogWarning("Cannot send refund confirmation - order not found: {OrderNumber}", orderNumber);
            return;
        }

        try
        {
            await xbkOrderNotificationService.SendNotification(order.OrderID, cancellationToken);
            logger.LogInformation("Refund confirmation sent for order: {OrderNumber}", orderNumber);
        }
        catch (OrderNotificationSendException ex)
        {
            logger.LogError(ex, "Failed to send refund confirmation for order: {OrderNumber}", orderNumber);
        }
    }

    private async Task<OrderInfo?> GetOrderByNumberAsync(string orderNumber, CancellationToken cancellationToken)
    {
        var orders = await orderInfoProvider.Get()
            .WhereEquals(nameof(OrderInfo.OrderNumber), orderNumber)
            .TopN(1)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return orders.FirstOrDefault();
    }
}
