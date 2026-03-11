using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.Confirmation;

/// <summary>
/// Controller for email confirmation after registration.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class ConfirmationController(
    IUserManagerService userManagerService,
    ILogger<ConfirmationController> logger) : Controller
{
    /// <summary>
    /// Route URL for confirmation.
    /// </summary>
    public const string RouteUrl = "Account/Confirmation";

    /// <summary>
    /// Handles the confirmation page.
    /// If userId and token are present, confirms the email and enables the user.
    /// Otherwise shows a "check your email" message.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    public async Task<IActionResult> Confirmation(
        [FromQuery] int? userId,
        [FromQuery] string? token)
    {
        if (userId.HasValue && !string.IsNullOrEmpty(token))
        {
            try
            {
                var user = await userManagerService.FindByIdAsync(userId.Value.ToString());
                if (user is null)
                {
                    TempData["EmailConfirmationError"] = "User not found.";
                    return View("~/Features/Account/Confirmation/Confirmation.cshtml");
                }

                var result = await userManagerService.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    // Enable user after email confirmation
                    await userManagerService.EnableUserByIdAsync(user.UserId);

                    TempData["EmailConfirmationSuccess"] = "Your email has been successfully confirmed. You can now log in to your account.";
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["EmailConfirmationError"] = $"Email confirmation failed: {errors}";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error confirming email for userId {UserId}", userId);
                TempData["EmailConfirmationError"] = "An error occurred while confirming your email. Please try again.";
            }
        }

        return View("~/Features/Account/Confirmation/Confirmation.cshtml");
    }

    /// <summary>
    /// Gets the confirmation URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
