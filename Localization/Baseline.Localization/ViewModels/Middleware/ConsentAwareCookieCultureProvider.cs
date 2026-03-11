using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Localization.Middleware;

/// <summary>
/// A cookie-based culture provider that respects cookie consent.
/// Only reads the culture cookie when the user has granted consent
/// (indicated by a configurable consent cookie).
/// Falls through to the next provider when consent is not granted.
/// </summary>
public class ConsentAwareCookieCultureProvider : RequestCultureProvider
{
    /// <summary>
    /// The name of the culture cookie to read.
    /// </summary>
    public string CookieName { get; set; } = ".Baseline.Culture";

    /// <summary>
    /// The name of the consent cookie to check.
    /// When this cookie is present and its value matches <see cref="ConsentValue"/>,
    /// the culture cookie is read. Otherwise this provider returns null (falls through).
    /// </summary>
    public string ConsentCookieName { get; set; } = ".AspNet.Consent";

    /// <summary>
    /// The value the consent cookie must have to be considered "granted".
    /// Default: "yes" (matches ASP.NET Core's CookiePolicyMiddleware default).
    /// Set to null to treat any non-empty cookie value as consent granted.
    /// </summary>
    public string? ConsentValue { get; set; } = "yes";

    /// <inheritdoc />
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Check cookie consent before reading culture cookie
        if (!HasConsent(httpContext))
        {
            return NullProviderCultureResult;
        }

        // Delegate to standard cookie culture provider logic
        var cookie = httpContext.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(cookie))
        {
            return NullProviderCultureResult;
        }

        var result = CookieRequestCultureProvider.ParseCookieValue(cookie);
        return Task.FromResult<ProviderCultureResult?>(result);
    }

    private bool HasConsent(HttpContext httpContext)
    {
        var consentCookie = httpContext.Request.Cookies[ConsentCookieName];
        if (string.IsNullOrEmpty(consentCookie))
        {
            return false;
        }

        // If no specific consent value is required, any non-empty value is consent
        if (ConsentValue is null)
        {
            return true;
        }

        return string.Equals(consentCookie, ConsentValue, StringComparison.OrdinalIgnoreCase);
    }
}
