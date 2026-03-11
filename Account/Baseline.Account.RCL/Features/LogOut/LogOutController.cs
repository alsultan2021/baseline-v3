using Account.Features.Account.LogOut;
using Baseline.Account;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.LogOut;

/// <summary>
/// Controller for handling user logout.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class LogOutController(
    ISignInManagerService signInManagerService,
    ILogger<LogOutController> logger) : Controller
{
    /// <summary>
    /// Route URL for logout.
    /// </summary>
    public const string RouteUrl = "Account/LogOut";

    /// <summary>
    /// Displays the logout confirmation page.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    public IActionResult LogOut()
    {
        var model = new LogOutViewModel
        {
            IsSignedIn = User.Identity?.IsAuthenticated ?? false
        };
        return View("~/Features/Account/LogOut/LogOut.cshtml", model);
    }

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    [HttpPost]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogOut(LogOutViewModel model)
    {
        try
        {
            await signInManagerService.SignOutAsync();
            logger.LogInformation("User logged out");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
        }

        var redirectUrl = string.IsNullOrWhiteSpace(model.RedirectUrl) ? "/" : model.RedirectUrl;
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// Gets the logout URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
