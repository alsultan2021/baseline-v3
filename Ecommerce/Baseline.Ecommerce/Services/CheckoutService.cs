using System.Text.Json;
using Baseline.Ecommerce.Automation;
using Baseline.Ecommerce.Models;
using CMS.Commerce;
using CMS.DataEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using XperienceCommunity.ChannelSettings.Repositories;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of ICheckoutService using session-based checkout flow.
/// Integrates with Kentico Commerce for shipping and payment methods.
/// Uses CommerceChannelSettings for checkout configuration.
/// Delegates order creation to IOrderService with full promotion persistence.
/// </summary>
public class CheckoutService(
    IHttpContextAccessor httpContextAccessor,
    ICartService cartService,
    IOrderService orderService,
    IPricingService pricingService,
    IChannelCustomSettingsRepository channelSettingsRepository,
    IAutomationEventInterceptor automationEvents,
    IMemoryCache cache,
    IInfoProvider<ShippingMethodInfo> shippingMethodInfoProvider,
    IInfoProvider<PaymentMethodInfo> paymentMethodInfoProvider,
    ILogger<CheckoutService> logger) : ICheckoutService
{
    private const string CheckoutSessionKey = "Baseline_CheckoutSession";
    private const string CheckoutSessionCacheKey = "Baseline_CheckoutSession_Cache";
    private const string ChannelSettingsCacheKey = "Baseline.Ecommerce.CheckoutSettings";

    /// <inheritdoc/>
    public Task<CheckoutSession?> GetSessionAsync()
    {
        logger.LogDebug("Getting checkout session");

        var session = GetSessionFromHttpContext();
        if (session != null && session.ExpiresAt < DateTimeOffset.UtcNow)
        {
            logger.LogDebug("Checkout session expired");
            ClearSessionFromHttpContext();
            return Task.FromResult<CheckoutSession?>(null);
        }

        return Task.FromResult(session);
    }

    /// <inheritdoc/>
    public async Task<CheckoutSession> StartCheckoutAsync()
    {
        logger.LogDebug("Starting checkout session");

        var cart = await cartService.GetCartAsync();

        var session = new CheckoutSession
        {
            Id = Guid.NewGuid(),
            CartId = cart.Id,
            CurrentStep = "shipping",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        SaveSessionToHttpContext(session);
        return session;
    }

    /// <inheritdoc/>
    public async Task<CheckoutResult> SetShippingAddressAsync(Address address)
    {
        logger.LogDebug("Setting shipping address");

        var session = await GetOrCreateSessionAsync();
        session.ShippingAddress = address;

        if (session.UseSameAddressForBilling)
        {
            session.BillingAddress = address;
        }

        session.CurrentStep = "shipping-method";
        SaveSessionToHttpContext(session);

        return CheckoutResult.Succeeded(session);
    }

    /// <inheritdoc/>
    public async Task<CheckoutResult> SetBillingAddressAsync(Address address)
    {
        logger.LogDebug("Setting billing address");

        var session = await GetOrCreateSessionAsync();
        session.BillingAddress = address;
        session.UseSameAddressForBilling = false;
        SaveSessionToHttpContext(session);

        return CheckoutResult.Succeeded(session);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ShippingMethod>> GetShippingMethodsAsync()
    {
        logger.LogDebug("Getting shipping methods");

        try
        {
            var methods = await shippingMethodInfoProvider.Get()
                .WhereEquals(nameof(ShippingMethodInfo.ShippingMethodEnabled), true)
                .OrderBy(nameof(ShippingMethodInfo.ShippingMethodDisplayName))
                .GetEnumerableTypedResultAsync();

            return methods.Select(m => new ShippingMethod
            {
                Id = m.ShippingMethodGUID,
                Name = m.ShippingMethodDisplayName ?? m.ShippingMethodName,
                Description = m.ShippingMethodDescription,
                Cost = Money.Zero() // Cost calculation requires destination - done at checkout completion
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve shipping methods");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<CheckoutResult> SetShippingMethodAsync(Guid shippingMethodId)
    {
        logger.LogDebug("Setting shipping method: {ShippingMethodId}", shippingMethodId);

        var session = await GetOrCreateSessionAsync();
        session.ShippingMethodId = shippingMethodId;
        session.CurrentStep = "payment";
        SaveSessionToHttpContext(session);

        return CheckoutResult.Succeeded(session);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PaymentMethod>> GetPaymentMethodsAsync()
    {
        logger.LogDebug("Getting payment methods");

        try
        {
            var methods = await paymentMethodInfoProvider.Get()
                .WhereEquals(nameof(PaymentMethodInfo.PaymentMethodEnabled), true)
                .OrderBy(nameof(PaymentMethodInfo.PaymentMethodDisplayName))
                .GetEnumerableTypedResultAsync();

            return methods.Select(m => new PaymentMethod
            {
                Id = m.PaymentMethodGUID,
                Name = m.PaymentMethodDisplayName ?? m.PaymentMethodName,
                Description = m.PaymentMethodDescription,
                Type = "Generic"
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve payment methods");
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task<CheckoutResult> SetPaymentMethodAsync(Guid paymentMethodId)
    {
        logger.LogDebug("Setting payment method: {PaymentMethodId}", paymentMethodId);

        var session = await GetOrCreateSessionAsync();
        session.PaymentMethodId = paymentMethodId;
        session.CurrentStep = "review";
        SaveSessionToHttpContext(session);

        return CheckoutResult.Succeeded(session);
    }

    /// <inheritdoc/>
    public async Task<OrderResult> CompleteCheckoutAsync(CompleteCheckoutRequest request)
    {
        logger.LogDebug("Completing checkout");

        var session = await GetSessionAsync();
        if (session == null)
        {
            return OrderResult.Failed("No active checkout session");
        }

        try
        {
            // Get cart for order items
            var cart = await cartService.GetCartAsync();
            if (!cart.Items.Any())
            {
                return OrderResult.Failed("Cart is empty");
            }

            // Calculate final pricing with checkout mode (includes shipping, tax, promotions)
            // Include coupon codes so Kentico DC coupon-gated promotions are evaluated at checkout
            var couponCodes = cart.Discounts
                .Where(d => d.Type is DiscountType.KenticoDcCoupon
                    or DiscountType.Percentage
                    or DiscountType.FixedAmount)
                .Select(d => d.Code)
                .ToList();

            var priceRequest = new PriceCalculationRequest
            {
                Items = cart.Items.Select(i => new PriceCalculationRequestItem
                {
                    ProductIdentifier = i.ProductId.ToString(),
                    Quantity = i.Quantity,
                    // Use override price for virtual products (gift cards)
                    OverridePrice = i.ProductId < 0 ? i.UnitPrice.Amount : null
                }).ToList(),
                Mode = PriceCalculationMode.Checkout,
                ShippingAddress = session.ShippingAddress,
                CouponCodes = couponCodes
            };

            var priceResult = await pricingService.CalculateAsync(priceRequest);

            // Validate session is ready for completion (includes min order amount check)
            var validationErrors = await ValidateSessionForCompletionAsync(session, request, priceResult.GrandTotal);
            if (validationErrors.Count > 0)
            {
                return OrderResult.Failed(string.Join("; ", validationErrors));
            }

            // Get member ID from HttpContext
            var user = httpContextAccessor.HttpContext?.User;
            int? memberId = null;
            var memberIdClaim = user?.FindFirst("MemberId") ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (memberIdClaim != null && int.TryParse(memberIdClaim.Value, out var parsedMemberId))
            {
                memberId = parsedMemberId;
            }

            // Create order request with promotion data
            var createOrderRequest = new CreateOrderRequest
            {
                BillingAddress = session.BillingAddress ?? session.ShippingAddress!,
                ShippingAddress = session.UseSameAddressForBilling ? null : session.ShippingAddress,
                Items = cart.Items.ToList().AsReadOnly(),
                ShippingMethodId = session.ShippingMethodId,
                PaymentMethodId = session.PaymentMethodId,
                PriceCalculation = priceResult,
                Notes = request.OrderNotes,
                MemberId = memberId
            };

            // Create the order with promotion persistence
            var orderResult = await orderService.CreateOrderAsync(createOrderRequest);

            if (orderResult.Success)
            {
                // Clear cart and checkout session on success
                await cartService.ClearCartAsync();
                ClearSessionFromHttpContext();

                logger.LogInformation("Checkout completed successfully. Order: {OrderNumber}",
                    orderResult.Order?.OrderNumber);

                // Fire CouponUsed triggers for any applied coupons (best-effort)
                if (orderResult.Order is not null && couponCodes.Count > 0)
                {
                    foreach (var code in couponCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
                    {
                        await automationEvents.OnCouponRedeemedAsync(
                            code!,
                            orderResult.Order.Id,
                            priceResult.GrandTotal,
                            memberId);
                    }
                }
            }

            return orderResult;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to complete checkout: SessionId={SessionId}", session.Id);
            return OrderResult.Failed($"Checkout failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task CancelCheckoutAsync()
    {
        logger.LogDebug("Cancelling checkout");
        ClearSessionFromHttpContext();
        return Task.CompletedTask;
    }

    #region Private Helpers

    private async Task<CheckoutSession> GetOrCreateSessionAsync()
    {
        var session = await GetSessionAsync();
        if (session == null)
        {
            session = await StartCheckoutAsync();
        }
        return session;
    }

    private CheckoutSession? GetSessionFromHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            return null;
        }

        // Check request-scoped cache first to avoid repeated deserialization
        if (httpContext.Items.TryGetValue(CheckoutSessionCacheKey, out var cached) && cached is CheckoutSession cachedSession)
        {
            return cachedSession;
        }

        var json = httpContext.Session.GetString(CheckoutSessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var session = JsonSerializer.Deserialize<CheckoutSession>(json);
            if (session != null)
            {
                // Cache in HttpContext.Items for request duration
                httpContext.Items[CheckoutSessionCacheKey] = session;
            }
            return session;
        }
        catch
        {
            return null;
        }
    }

    private void SaveSessionToHttpContext(CheckoutSession session)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Session == null)
        {
            logger.LogError("Cannot save checkout session - no HTTP session available. Ensure session middleware is configured.");
            throw new InvalidOperationException("HTTP session is required for checkout but is not available.");
        }

        var json = JsonSerializer.Serialize(session);
        httpContext.Session.SetString(CheckoutSessionKey, json);

        // Update request-scoped cache
        httpContext.Items[CheckoutSessionCacheKey] = session;
    }

    private void ClearSessionFromHttpContext()
    {
        var httpContext = httpContextAccessor.HttpContext;
        httpContext?.Session?.Remove(CheckoutSessionKey);
        httpContext?.Items.Remove(CheckoutSessionCacheKey);
    }

    private async Task<CommerceChannelSettings?> GetChannelSettingsAsync()
    {
        return await cache.GetOrCreateAsync(ChannelSettingsCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.Size = 1;
            return await channelSettingsRepository.GetSettingsModel<CommerceChannelSettings>();
        });
    }

    /// <summary>
    /// Checks if guest checkout is enabled for the current channel.
    /// </summary>
    public async Task<bool> IsGuestCheckoutEnabledAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.EnableGuestCheckout ?? true;
    }

    /// <summary>
    /// Gets the minimum order amount for the current channel.
    /// </summary>
    public async Task<decimal> GetMinimumOrderAmountAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.MinimumOrderAmount ?? 0;
    }

    /// <summary>
    /// Checks if an account is required for checkout.
    /// </summary>
    public async Task<bool> IsAccountRequiredForCheckoutAsync()
    {
        var settings = await GetChannelSettingsAsync();
        return settings?.RequireAccountForCheckout ?? false;
    }

    private async Task<List<string>> ValidateSessionForCompletionAsync(CheckoutSession session, CompleteCheckoutRequest request, decimal orderTotal)
    {
        var errors = new List<string>();

        if (session.ShippingAddress == null)
        {
            errors.Add("Shipping address is required");
        }

        if (session.BillingAddress == null && !session.UseSameAddressForBilling)
        {
            errors.Add("Billing address is required");
        }

        if (!session.ShippingMethodId.HasValue)
        {
            errors.Add("Shipping method is required");
        }

        if (!session.PaymentMethodId.HasValue)
        {
            errors.Add("Payment method is required");
        }

        if (!request.AcceptedTerms)
        {
            errors.Add("Terms and conditions must be accepted");
        }

        // Check minimum order amount from channel settings
        var minimumOrderAmount = await GetMinimumOrderAmountAsync();
        if (minimumOrderAmount > 0 && orderTotal < minimumOrderAmount)
        {
            errors.Add($"Minimum order amount is {minimumOrderAmount:C}");
        }

        // Check if account is required
        var requireAccount = await IsAccountRequiredForCheckoutAsync();
        if (requireAccount)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                errors.Add("An account is required to complete checkout");
            }
        }

        return errors;
    }

    #endregion
}
