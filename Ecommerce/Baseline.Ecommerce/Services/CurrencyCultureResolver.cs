using System.Collections.Concurrent;
using System.Globalization;

namespace Baseline.Ecommerce;

/// <summary>
/// Resolves ISO 4217 currency codes to <see cref="CultureInfo"/> instances
/// using .NET's <see cref="RegionInfo"/> metadata — no hardcoded lookup tables.
/// Results are cached for the lifetime of the application.
/// </summary>
public static class CurrencyCultureResolver
{
    private static readonly ConcurrentDictionary<string, CultureInfo> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a <see cref="CultureInfo"/> whose <see cref="RegionInfo"/> uses the
    /// given ISO 4217 <paramref name="currencyCode"/>.
    /// Falls back to <see cref="CultureInfo.CurrentCulture"/> when no match is found.
    /// </summary>
    public static CultureInfo Resolve(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return CultureInfo.CurrentCulture;
        }

        return Cache.GetOrAdd(currencyCode, static code =>
        {
            var upper = code.Trim().ToUpperInvariant();

            // Scan all specific cultures for the first match on ISOCurrencySymbol
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var region = new RegionInfo(ci.Name);
                    if (string.Equals(region.ISOCurrencySymbol, upper, StringComparison.OrdinalIgnoreCase))
                    {
                        return ci;
                    }
                }
                catch (ArgumentException)
                {
                    // Some culture names don't map to a RegionInfo — skip
                }
            }

            return CultureInfo.CurrentCulture;
        });
    }

    /// <summary>
    /// Returns the culture name string (e.g. "en-CA") for the given currency code.
    /// </summary>
    public static string ResolveName(string currencyCode) => Resolve(currencyCode).Name;
}
