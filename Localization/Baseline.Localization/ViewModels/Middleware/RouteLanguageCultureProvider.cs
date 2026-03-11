using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Localization.Middleware;

/// <summary>
/// Culture provider that reads the language from route values (e.g., <c>{language}</c>)
/// and maps it to the matching supported culture.
/// Kentico route values use short language code names (e.g., "fr") which must be
/// mapped to full .NET culture codes (e.g., "fr-FR") via <see cref="Baseline.Localization.BaselineCultureInfo.ShortCode"/>.
/// </summary>
public sealed class RouteLanguageCultureProvider : RequestCultureProvider
{
    /// <summary>
    /// The route value key to read. Must match the <c>{language}</c> parameter in route templates
    /// and the <c>WebPageRoutingOptions.LanguageNameRouteValuesKey</c> configured for Kentico.
    /// </summary>
    public string RouteDataKey { get; set; } = "language";

    /// <summary>
    /// The supported cultures from <see cref="Baseline.Localization.BaselineLocalizationOptions.SupportedCultures"/>.
    /// Used to map short Kentico language codes to full .NET culture codes.
    /// </summary>
    public IReadOnlyList<Baseline.Localization.BaselineCultureInfo> SupportedCultures { get; set; } = [];

    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var routeValue = httpContext.Request.RouteValues.TryGetValue(RouteDataKey, out var val)
            ? val?.ToString()
            : null;
        if (string.IsNullOrEmpty(routeValue))
        {
            return NullProviderCultureResult;
        }

        // Look up by Kentico short code (e.g., "fr" → "fr-FR")
        var match = SupportedCultures.FirstOrDefault(c =>
            string.Equals(c.ShortCode, routeValue, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            return Task.FromResult<ProviderCultureResult?>(
                new ProviderCultureResult(match.Code, match.Code));
        }

        // Fallback: try the route value as-is (might already be a full culture code)
        return Task.FromResult<ProviderCultureResult?>(
            new ProviderCultureResult(routeValue, routeValue));
    }
}
