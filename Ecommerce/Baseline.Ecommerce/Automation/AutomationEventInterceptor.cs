using Baseline.Automation;
using Baseline.Automation.Services;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Automation;

/// <summary>
/// Bridges ecommerce events to the Baseline Automation Engine by converting
/// <see cref="IAutomationEventInterceptor"/> calls into
/// <see cref="IAutomationTriggerDispatcher"/> trigger dispatches.
/// </summary>
internal sealed class AutomationEventInterceptor(
    IAutomationTriggerDispatcher dispatcher,
    ILogger<AutomationEventInterceptor> logger) : IAutomationEventInterceptor
{
    public async Task OnOrderCreatedAsync(Order order, int? memberId)
    {
        if (memberId is not > 0)
            return;

        await dispatcher.FireForMemberAsync(AutomationTriggerType.OrderPlaced, memberId.Value, new
        {
            OrderId = order.Id.ToString(),
            order.OrderNumber,
            OrderTotal = order.Totals.Total.Amount.ToString("F2"),
            ItemCount = order.Items.Count.ToString()
        });

        // Fire ProductPurchased for each item
        foreach (var item in order.Items)
        {
            await dispatcher.FireForMemberAsync(AutomationTriggerType.ProductPurchased, memberId.Value, new
            {
                ProductId = item.ProductId.ToString(),
                item.ProductName,
                item.Quantity
            });
        }
    }

    public async Task OnOrderStatusChangedAsync(
        Guid orderId, string orderNumber, string fromStatus, string toStatus, int? memberId)
    {
        if (memberId is not > 0)
            return;

        await dispatcher.FireForMemberAsync(AutomationTriggerType.OrderStatusChanged, memberId.Value, new
        {
            OrderId = orderId.ToString(),
            OrderNumber = orderNumber,
            FromStatus = fromStatus,
            ToStatus = toStatus
        });
    }

    public async Task OnOrderCancelledAsync(Order order, string? reason, int? memberId)
    {
        if (memberId is not > 0)
            return;

        await dispatcher.FireForMemberAsync(AutomationTriggerType.OrderStatusChanged, memberId.Value, new
        {
            OrderId = order.Id.ToString(),
            order.OrderNumber,
            FromStatus = order.Status.ToString(),
            ToStatus = nameof(OrderStatus.Cancelled),
            Reason = reason ?? ""
        });
    }

    public async Task OnPaymentFailedAsync(
        Guid orderId, string orderNumber, string errorMessage, int? memberId)
    {
        if (memberId is not > 0)
            return;

        await dispatcher.FireForMemberAsync(AutomationTriggerType.PaymentFailed, memberId.Value, new
        {
            OrderId = orderId.ToString(),
            OrderNumber = orderNumber,
            ErrorMessage = errorMessage
        });
    }

    public async Task OnWishlistUpdatedAsync(int memberId, int productId, bool added)
    {
        await dispatcher.FireForMemberAsync(AutomationTriggerType.WishlistUpdated, memberId, new
        {
            ProductId = productId.ToString(),
            Action = added ? "Add" : "Remove"
        });
    }

    public async Task OnCouponRedeemedAsync(
        string couponCode, Guid orderId, decimal orderTotal, int? memberId)
    {
        if (memberId is not > 0)
            return;

        await dispatcher.FireForMemberAsync(AutomationTriggerType.CouponUsed, memberId.Value, new
        {
            CouponCode = couponCode,
            OrderId = orderId.ToString(),
            OrderTotal = orderTotal.ToString("F2")
        });
    }

    public async Task OnProductBackInStockAsync(int productId, string productName)
    {
        // ProductBackInStock is not member-specific; skip if no dispatcher method for broadcast
        logger.LogDebug("ProductBackInStock event for product {ProductId} ({Name}) — no contact-specific dispatch",
            productId, productName);
    }

    public async Task OnRefundIssuedAsync(
        Guid orderId, string orderNumber, decimal refundAmount, int? memberId)
    {
        if (memberId is not > 0)
            return;

        await dispatcher.FireForMemberAsync(AutomationTriggerType.RefundIssued, memberId.Value, new
        {
            OrderId = orderId.ToString(),
            OrderNumber = orderNumber,
            RefundAmount = refundAmount.ToString("F2")
        });
    }

    public async Task OnSubscriptionCreatedAsync(int memberId, string planName, decimal amount)
    {
        await dispatcher.FireForMemberAsync(AutomationTriggerType.SubscriptionCreated, memberId, new
        {
            PlanName = planName,
            Amount = amount.ToString("F2")
        });
    }

    public async Task OnSubscriptionCancelledAsync(int memberId, string planName, string? reason)
    {
        await dispatcher.FireForMemberAsync(AutomationTriggerType.SubscriptionCancelled, memberId, new
        {
            PlanName = planName,
            Reason = reason ?? ""
        });
    }

    public async Task OnSubscriptionRenewedAsync(int memberId, string planName, decimal amount)
    {
        await dispatcher.FireForMemberAsync(AutomationTriggerType.SubscriptionRenewed, memberId, new
        {
            PlanName = planName,
            Amount = amount.ToString("F2")
        });
    }

    public async Task OnLoyaltyTierChangedAsync(
        int memberId, string previousTier, string newTier, int totalPoints)
    {
        await dispatcher.FireForMemberAsync(AutomationTriggerType.LoyaltyTierChanged, memberId, new
        {
            PreviousTier = previousTier,
            NewTier = newTier,
            TotalPoints = totalPoints.ToString()
        });
    }
}
