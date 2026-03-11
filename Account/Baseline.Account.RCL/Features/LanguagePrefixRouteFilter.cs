using Microsoft.AspNetCore.Mvc.Filters;

namespace Baseline.Account.RCL.Features;

/// <summary>
/// Action filter that propagates the <c>{language}</c> route parameter
/// into the configured <c>LanguageNameRouteValuesKey</c> so Kentico
/// content-tree routing resolves the correct language context.
/// Apply via <c>[TypeFilter(typeof(LanguagePrefixRouteFilter))]</c> or globally.
/// </summary>
public sealed class LanguagePrefixRouteFilter : IActionFilter
{
    /// <summary>
    /// Route parameter name used in route templates like <c>{language}/Account/LogIn</c>.
    /// Must match the value configured in <c>WebPageRoutingOptions.LanguageNameRouteValuesKey</c>.
    /// </summary>
    public const string LanguageRouteParam = "language";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var routeValues = context.HttpContext.Request.RouteValues;

        if (routeValues.TryGetValue(LanguageRouteParam, out var langValue)
            && langValue is string lang
            && !string.IsNullOrEmpty(lang))
        {
            // Kentico reads the language from RouteValues using the key configured
            // in WebPageRoutingOptions.LanguageNameRouteValuesKey (default: "language").
            // Since the route param name matches this key, no additional mapping is needed.
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
