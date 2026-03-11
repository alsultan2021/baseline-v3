using CMS.Commerce;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ICacheDependencyBuilderFactory = CMS.Helpers.ICacheDependencyBuilderFactory;

namespace Baseline.Ecommerce;

/// <summary>
/// Default implementation of <see cref="IPaymentMethodResolver"/>.
/// Retrieves payment methods from Kentico's PaymentMethodInfo and maps them to payment options.
/// </summary>
public class PaymentMethodResolver(
    IInfoProvider<PaymentMethodInfo> paymentMethodInfoProvider,
    IWebsiteChannelContext websiteChannelContext,
    IProgressiveCache cache,
    ICacheDependencyBuilderFactory cacheDependencyBuilderFactory,
    IOptions<BaselineEcommerceOptions> options,
    ILogger<PaymentMethodResolver> logger) : IPaymentMethodResolver
{
    private const int CacheMinutes = 5;

    private readonly BaselineEcommerceOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<IEnumerable<PaymentMethodOption>> GetAvailableMethodsAsync(
        PaymentMethodContext context,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Getting available payment methods for channel: {ChannelName}", context.ChannelName);

        var paymentMethods = await GetPaymentMethodsAsync(cancellationToken);
        var options = new List<PaymentMethodOption>();

        foreach (var method in paymentMethods.Where(m => m.PaymentMethodEnabled))
        {
            var option = MapToPaymentOption(method);

            // Apply context-based filtering
            if (IsMethodAvailable(option, context))
            {
                options.Add(option);
            }
        }

        // Order by display order (using ID as fallback)
        return options.OrderBy(o => o.DisplayOrder);
    }

    /// <inheritdoc/>
    public async Task<PaymentMethodValidationResult> ValidateMethodAsync(
        Guid paymentMethodId,
        PaymentMethodContext context,
        CancellationToken cancellationToken = default)
    {
        var paymentMethods = await GetPaymentMethodsAsync(cancellationToken);
        var method = paymentMethods.FirstOrDefault(m => m.PaymentMethodGUID == paymentMethodId);

        if (method == null)
        {
            return PaymentMethodValidationResult.Invalid("Payment method not found.");
        }

        if (!method.PaymentMethodEnabled)
        {
            return PaymentMethodValidationResult.Invalid("Payment method is not enabled.");
        }

        var option = MapToPaymentOption(method);

        // Validate based on context
        if (!IsMethodAvailable(option, context))
        {
            return PaymentMethodValidationResult.Invalid("Payment method is not available for this order.");
        }

        return PaymentMethodValidationResult.Valid();
    }

    /// <inheritdoc/>
    public async Task<PaymentMethodOption?> GetDefaultMethodAsync(
        PaymentMethodContext context,
        CancellationToken cancellationToken = default)
    {
        var methods = await GetAvailableMethodsAsync(context, cancellationToken);
        return methods.FirstOrDefault(m => m.IsDefault) ?? methods.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<PaymentMethodConfiguration?> GetMethodConfigurationAsync(
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        var paymentMethods = await GetPaymentMethodsAsync(cancellationToken);
        var method = paymentMethods.FirstOrDefault(m => m.PaymentMethodGUID == paymentMethodId);

        if (method == null)
        {
            return null;
        }

        return new PaymentMethodConfiguration
        {
            Id = method.PaymentMethodGUID,
            Code = method.PaymentMethodName,
            DisplayName = method.PaymentMethodName,
            Description = method.PaymentMethodDescription,
            IsEnabled = method.PaymentMethodEnabled,
            Type = InferPaymentMethodType(method.PaymentMethodName),
            Settings = new Dictionary<string, object>()
        };
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SavedPaymentMethod>> GetSavedMethodsAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        // Saved payment methods require additional implementation
        // This would typically integrate with a payment gateway's vault API
        logger.LogDebug("GetSavedMethodsAsync called for customer {CustomerId} - not implemented", customerId);
        return Task.FromResult<IEnumerable<SavedPaymentMethod>>([]);
    }

    #region Private Helpers

    /// <summary>
    /// Gets cached payment methods from Kentico.
    /// </summary>
    private async Task<IEnumerable<PaymentMethodInfo>> GetPaymentMethodsAsync(CancellationToken cancellationToken)
    {
        if (websiteChannelContext.IsPreview)
        {
            return await GetPaymentMethodsInternalAsync(cancellationToken);
        }

        var cacheSettings = new CacheSettings(
            CacheMinutes,
            websiteChannelContext.WebsiteChannelName,
            nameof(PaymentMethodResolver),
            nameof(GetPaymentMethodsAsync));

        return await cache.LoadAsync(async cs =>
        {
            var result = await GetPaymentMethodsInternalAsync(cancellationToken);
            var resultList = result.ToList();

            if (resultList.Count > 0)
            {
                cs.CacheDependency = cacheDependencyBuilderFactory.Create()
                    .ForInfoObjects<PaymentMethodInfo>()
                    .All()
                    .Builder()
                    .Build();
            }

            return resultList;
        }, cacheSettings);
    }

    private async Task<IEnumerable<PaymentMethodInfo>> GetPaymentMethodsInternalAsync(CancellationToken cancellationToken) =>
        await paymentMethodInfoProvider.Get()
            .WhereTrue(nameof(PaymentMethodInfo.PaymentMethodEnabled))
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken) ?? [];

    /// <summary>
    /// Maps PaymentMethodInfo to PaymentMethodOption.
    /// </summary>
    private static PaymentMethodOption MapToPaymentOption(PaymentMethodInfo method)
    {
        return new PaymentMethodOption
        {
            Id = method.PaymentMethodGUID,
            Code = method.PaymentMethodName,
            DisplayName = method.PaymentMethodName,
            Description = method.PaymentMethodDescription,
            Type = InferPaymentMethodType(method.PaymentMethodName),
            IsEnabled = method.PaymentMethodEnabled,
            IsDefault = false, // Can be extended via custom fields
            DisplayOrder = method.PaymentMethodID,
            IconUrl = null, // Can be extended via custom fields
            SupportedCurrencies = [], // Can be extended via custom fields
            SupportedCountries = [], // Can be extended via custom fields
            MinimumAmount = null,
            MaximumAmount = null
        };
    }

    /// <summary>
    /// Infers the payment method type from the method name.
    /// </summary>
    private static PaymentMethodType InferPaymentMethodType(string methodName)
    {
        var name = methodName.ToUpperInvariant();

        return name switch
        {
            var n when n.Contains("CREDIT") || n.Contains("CARD") || n.Contains("VISA") || n.Contains("MASTER") =>
                PaymentMethodType.CreditCard,
            var n when n.Contains("DEBIT") =>
                PaymentMethodType.DebitCard,
            var n when n.Contains("PAYPAL") =>
                PaymentMethodType.PayPal,
            var n when n.Contains("STRIPE") =>
                PaymentMethodType.Stripe,
            var n when n.Contains("BANK") || n.Contains("TRANSFER") || n.Contains("ACH") =>
                PaymentMethodType.BankTransfer,
            var n when n.Contains("INVOICE") =>
                PaymentMethodType.Invoice,
            var n when n.Contains("COD") || n.Contains("DELIVERY") =>
                PaymentMethodType.CashOnDelivery,
            var n when n.Contains("APPLE") =>
                PaymentMethodType.ApplePay,
            var n when n.Contains("GOOGLE") =>
                PaymentMethodType.GooglePay,
            var n when n.Contains("WALLET") =>
                PaymentMethodType.Wallet,
            var n when n.Contains("CRYPTO") || n.Contains("BITCOIN") =>
                PaymentMethodType.Cryptocurrency,
            var n when n.Contains("GIFT") =>
                PaymentMethodType.GiftCard,
            var n when n.Contains("STORE") || n.Contains("CREDIT") =>
                PaymentMethodType.StoreCredit,
            _ => PaymentMethodType.Other
        };
    }

    /// <summary>
    /// Determines if a payment method is available based on context.
    /// </summary>
    private static bool IsMethodAvailable(PaymentMethodOption option, PaymentMethodContext context)
    {
        // Check currency restrictions
        if (option.SupportedCurrencies.Count > 0 &&
            !string.IsNullOrEmpty(context.CurrencyCode) &&
            !option.SupportedCurrencies.Contains(context.CurrencyCode))
        {
            return false;
        }

        // Check country restrictions
        if (option.SupportedCountries.Count > 0 &&
            !string.IsNullOrEmpty(context.BillingCountry) &&
            !option.SupportedCountries.Contains(context.BillingCountry))
        {
            return false;
        }

        // Check minimum amount
        if (option.MinimumAmount.HasValue &&
            context.OrderTotal != null &&
            context.OrderTotal.Amount < option.MinimumAmount.Value)
        {
            return false;
        }

        // Check maximum amount
        if (option.MaximumAmount.HasValue &&
            context.OrderTotal != null &&
            context.OrderTotal.Amount > option.MaximumAmount.Value)
        {
            return false;
        }

        // Check guest checkout restrictions
        if (context.IsGuestCheckout && option.RequiresAccount)
        {
            return false;
        }

        // Check subscription requirements
        if (context.HasSubscriptionItems && !option.SupportsRecurring)
        {
            return false;
        }

        return true;
    }

    #endregion
}
