using Baseline.Account;
using Baseline.Core;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
// Use Core's IUrlResolver which is registered in DI
using IUrlResolver = Baseline.Core.IUrlResolver;
// External view model for chevalroyal template overrides
using ExternalForgotPasswordViewModel = Account.Features.Account.ForgotPassword.ForgotPasswordViewModel;

namespace Baseline.Account.RCL.Features.ForgotPassword;

#region View Models

/// <summary>
/// View model for forgot password request.
/// </summary>
public sealed class ForgotPasswordViewModel
{
    /// <summary>
    /// Email address for password reset.
    /// </summary>
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Whether the password reset email was sent successfully.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Error message if the request failed.
    /// </summary>
    public string? Error { get; set; }
}

#endregion

/// <summary>
/// Controller for handling forgot password requests.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class ForgotPasswordController(
    IAccountSettingsRepository accountSettingsRepository,
    IUserService userService,
    IUserRepository userRepository,
    Baseline.Core.IUrlResolver urlResolver,
    IModelStateService modelStateService,
    ILogger<ForgotPasswordController> logger) : Controller
{
    /// <summary>
    /// Route URL for forgot password.
    /// </summary>
    public const string RouteUrl = "Account/ForgotPassword";

    /// <summary>
    /// Displays the forgot password form.
    /// Falls back to manual view if not using Page Templates.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    public IActionResult ForgotPassword()
    {
        // Try to retrieve stored model from POST redirect (PRG pattern)
        var storedModel = modelStateService.RetrieveViewModel<ForgotPasswordViewModel>(TempData);
        
        var viewModel = new ExternalForgotPasswordViewModel
        {
            EmailAddress = storedModel?.EmailAddress,
            Succeeded = storedModel?.Succeeded,
            Error = storedModel?.Error
        };
        
        return View("~/Features/Account/ForgotPassword/ForgotPassword.cshtml", viewModel);
    }

    /// <summary>
    /// Processes the forgot password request.
    /// For security, always shows success even if email not found.
    /// </summary>
    [HttpPost]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        var forgotPasswordUrl = await accountSettingsRepository.GetAccountForgotPasswordUrlAsync(GetUrl());

        if (!ModelState.IsValid)
        {
            return Redirect(forgotPasswordUrl);
        }

        try
        {
            var userResult = await userRepository.GetUserByEmailAsync(model.EmailAddress);
            if (userResult.TryGetValue(out var user))
            {
                if (user.IsExternal)
                {
                    model.Succeeded = false;
                    model.Error = "Your user is an External User (authenticated externally). There is no password to reset. Please log in with the appropriate Single Sign On Provider.";
                }
                else
                {
                    var resetUrl = await accountSettingsRepository.GetAccountForgottenPasswordResetUrlAsync(
                        ForgottenPasswordResetController.GetUrl());
                    await userService.SendPasswordResetEmailAsync(user, urlResolver.GetAbsoluteUrl(resetUrl));
                    model.Succeeded = true;
                }
            }
            else
            {
                // For security, still show success to prevent user enumeration
                model.Succeeded = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing forgot password request for {Email}", model.EmailAddress);
            model.Succeeded = false;
            model.Error = "An error occurred. Please try again later.";
        }

        modelStateService.StoreViewModel(TempData, model);
        return Redirect(forgotPasswordUrl);
    }

    /// <summary>
    /// Gets the forgot password URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
