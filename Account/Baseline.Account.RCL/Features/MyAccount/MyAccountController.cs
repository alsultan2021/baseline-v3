using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baseline.Account.RCL.Features.MyAccount;

/// <summary>
/// Controller for the user's account dashboard.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class MyAccountController : Controller
{
    /// <summary>
    /// Route URL for my account.
    /// </summary>
    public const string RouteUrl = "Account/MyAccount";

    /// <summary>
    /// Alternate route URL for profile (for compatibility with default config).
    /// </summary>
    public const string ProfileRouteUrl = "Account/Profile";

    /// <summary>
    /// Displays the user's account dashboard.
    /// Requires authentication.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route(ProfileRouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [Route("{language}/" + ProfileRouteUrl)]
    [Authorize]
    public IActionResult MyAccount() =>
        View("~/Features/Account/MyAccount/MyAccount.cshtml");

    /// <summary>
    /// Gets the my account URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
