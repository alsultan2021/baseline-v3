using System.Globalization;
using CMS.Commerce;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce;

/// <summary>
/// Default price formatter for displaying prices in the configured currency culture.
/// Uses <see cref="CurrencyCultureResolver"/> to resolve the correct culture
/// from the configured default currency at format time (not registration time).
/// </summary>
/// <remarks>
/// This is registered as the default implementation of <see cref="IPriceFormatter"/>.
/// Override by registering your own implementation with RegisterImplementation attribute
/// or via DI with a higher priority.
/// </remarks>
public class PriceFormatter(IOptions<BaselineEcommerceOptions> options) : IPriceFormatter
{
    private readonly string _defaultCurrency = options.Value.Pricing.DefaultCurrency;

    /// <inheritdoc/>
    public string Format(decimal price, PriceFormatContext context)
    {
        // Resolve culture from configured default currency at format time
        var culture = CurrencyCultureResolver.Resolve(_defaultCurrency);
        return price.ToString("C2", culture);
    }
}
