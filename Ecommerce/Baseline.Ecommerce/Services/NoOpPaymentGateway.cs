using Microsoft.AspNetCore.Http;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// No-op implementation of <see cref="IPaymentGateway"/> for scenarios where payment gateway is not configured.
/// Allows ecommerce services to run without a payment gateway dependency.
/// Returns not-implemented errors for any payment operations.
/// </summary>
internal sealed class NoOpPaymentGateway : IPaymentGateway
{
    public Task<CreateSessionResult> CreateOrReuseSessionAsync(OrderSnapshot order, CancellationToken ct = default)
        => throw new NotImplementedException(
            "No payment gateway is configured. " +
            "Please register a payment provider (Clover, Stripe, etc.) using services.AddCloverPayments() or similar.");

    public Task<WebhookResult> HandleWebhookAsync(HttpRequest request, CancellationToken ct = default)
        => Task.FromResult(new WebhookResult(false, null));

    public Task<bool> CapturePaymentAsync(string chargeId, long? amountMinor = null, CancellationToken ct = default)
        => throw new NotImplementedException(
            "No payment gateway is configured. " +
            "Please register a payment provider (Clover, Stripe, etc.) using services.AddCloverPayments() or similar.");

    public Task<string?> RefundPaymentAsync(string chargeId, long? amountMinor = null, string? reason = null, CancellationToken ct = default)
        => throw new NotImplementedException(
            "No payment gateway is configured. " +
            "Please register a payment provider (Clover, Stripe, etc.) using services.AddCloverPayments() or similar.");

    public Task<bool> VoidPaymentAsync(string chargeId, CancellationToken ct = default)
        => throw new NotImplementedException(
            "No payment gateway is configured. " +
            "Please register a payment provider (Clover, Stripe, etc.) using services.AddCloverPayments() or similar.");
}
