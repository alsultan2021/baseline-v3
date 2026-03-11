namespace Baseline.Ecommerce.Automation;

/// <summary>
/// High-level service for firing automation triggers from ecommerce operations.
/// Provides typed methods for each ecommerce event so callers don't need to
/// know about trigger types or data serialization.
/// </summary>
public interface IAutomationEventInterceptor
{
    Task OnOrderCreatedAsync(Order order, int? memberId);
    Task OnOrderStatusChangedAsync(Guid orderId, string orderNumber, string fromStatus, string toStatus, int? memberId);
    Task OnOrderCancelledAsync(Order order, string? reason, int? memberId);
    Task OnPaymentFailedAsync(Guid orderId, string orderNumber, string errorMessage, int? memberId);
    Task OnWishlistUpdatedAsync(int memberId, int productId, bool added);
    Task OnCouponRedeemedAsync(string couponCode, Guid orderId, decimal orderTotal, int? memberId);
    Task OnProductBackInStockAsync(int productId, string productName);
    Task OnRefundIssuedAsync(Guid orderId, string orderNumber, decimal refundAmount, int? memberId);
    Task OnSubscriptionCreatedAsync(int memberId, string planName, decimal amount);
    Task OnSubscriptionCancelledAsync(int memberId, string planName, string? reason);
    Task OnSubscriptionRenewedAsync(int memberId, string planName, decimal amount);
    Task OnLoyaltyTierChangedAsync(int memberId, string previousTier, string newTier, int totalPoints);
}

/// <summary>
/// No-op implementation of <see cref="IAutomationEventInterceptor"/> for when
/// the Baseline Automation Engine is not enabled. All methods return immediately.
/// </summary>
internal sealed class NullAutomationEventInterceptor : IAutomationEventInterceptor
{
    public Task OnOrderCreatedAsync(Order order, int? memberId) => Task.CompletedTask;
    public Task OnOrderStatusChangedAsync(Guid orderId, string orderNumber, string fromStatus, string toStatus, int? memberId) => Task.CompletedTask;
    public Task OnOrderCancelledAsync(Order order, string? reason, int? memberId) => Task.CompletedTask;
    public Task OnPaymentFailedAsync(Guid orderId, string orderNumber, string errorMessage, int? memberId) => Task.CompletedTask;
    public Task OnWishlistUpdatedAsync(int memberId, int productId, bool added) => Task.CompletedTask;
    public Task OnCouponRedeemedAsync(string couponCode, Guid orderId, decimal orderTotal, int? memberId) => Task.CompletedTask;
    public Task OnProductBackInStockAsync(int productId, string productName) => Task.CompletedTask;
    public Task OnRefundIssuedAsync(Guid orderId, string orderNumber, decimal refundAmount, int? memberId) => Task.CompletedTask;
    public Task OnSubscriptionCreatedAsync(int memberId, string planName, decimal amount) => Task.CompletedTask;
    public Task OnSubscriptionCancelledAsync(int memberId, string planName, string? reason) => Task.CompletedTask;
    public Task OnSubscriptionRenewedAsync(int memberId, string planName, decimal amount) => Task.CompletedTask;
    public Task OnLoyaltyTierChangedAsync(int memberId, string previousTier, string newTier, int totalPoints) => Task.CompletedTask;
}
