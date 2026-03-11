using Baseline.Account;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.ForgotPassword;

#region View Models

/// <summary>
/// View model for forgotten password reset (from email link).
/// </summary>
public sealed class ForgottenPasswordResetViewModel
{
    /// <summary>
    /// The user's unique identifier.
    /// </summary>
    public Guid UserID { get; set; }

    /// <summary>
    /// The password reset token from the email link.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The new password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation.
    /// </summary>
    public string PasswordConfirm { get; set; } = string.Empty;

    /// <summary>
    /// Result of the password reset operation.
    /// </summary>
    public IdentityResult? ResultIdentity { get; set; }

    /// <summary>
    /// Whether the reset succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// URL to the login page.
    /// </summary>
    public string? LoginUrl { get; set; }
}

/// <summary>
/// Validator for forgotten password reset.
/// </summary>
public sealed class ForgottenPasswordResetValidator : AbstractValidator<ForgottenPasswordResetViewModel>
{
    public ForgottenPasswordResetValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.PasswordConfirm)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required");

        RuleFor(x => x.UserID)
            .NotEmpty().WithMessage("User ID is required");
    }
}

#endregion

/// <summary>
/// Controller for resetting password from email link.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class ForgottenPasswordResetController(
    IUserRepository userRepository,
    IAccountSettingsRepository accountSettingsRepository,
    IUserService userService,
    IValidator<ForgottenPasswordResetViewModel> validator,
    IModelStateService modelStateService,
    ILogger<ForgottenPasswordResetController> logger) : Controller
{
    /// <summary>
    /// Route URL for forgotten password reset.
    /// </summary>
    public const string RouteUrl = "Account/ForgottenPasswordReset";

    /// <summary>
    /// Displays the password reset form.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    public IActionResult ForgottenPasswordReset() =>
        View("~/Features/ForgotPassword/ForgottenPasswordResetManual.cshtml");

    /// <summary>
    /// Processes the password reset.
    /// </summary>
    [HttpPost]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgottenPasswordReset(ForgottenPasswordResetViewModel model)
    {
        var resetUrl = await accountSettingsRepository.GetAccountForgottenPasswordResetUrlAsync(GetUrl());

        var validationResult = await validator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return Redirect(resetUrl);
        }

        try
        {
            model.ResultIdentity = IdentityResult.Failed();
            var userResult = await userRepository.GetUserAsync(model.UserID);

            if (userResult.IsFailure)
            {
                model.ResultIdentity = IdentityResult.Failed(
                    new IdentityError { Code = "NoUser", Description = userResult.Error });
            }
            else
            {
                model.ResultIdentity = await userService.ResetPasswordFromTokenAsync(
                    userResult.Value, model.Token, model.Password);
                model.LoginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(
                    LogIn.LogInController.GetUrl());
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting password for user {UserID}", model.UserID);
            model.ResultIdentity = IdentityResult.Failed(
                new IdentityError { Code = "Unknown", Description = "An error occurred." });
        }

        model.Succeeded = model.ResultIdentity?.Succeeded ?? false;

        // Clear passwords from temp storage
        model.Password = string.Empty;
        model.PasswordConfirm = string.Empty;

        modelStateService.StoreViewModel(TempData, model);
        return Redirect(resetUrl);
    }

    /// <summary>
    /// Gets the forgotten password reset URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
