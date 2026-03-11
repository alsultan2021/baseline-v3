using Baseline.Account;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Baseline.Account.RCL.Features.ResetPassword;

#region View Models

/// <summary>
/// View model for authenticated password reset.
/// </summary>
public sealed class ResetPasswordViewModel
{
    /// <summary>
    /// Current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// New password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation.
    /// </summary>
    public string PasswordConfirm { get; set; } = string.Empty;

    /// <summary>
    /// Whether the reset succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Error message if the reset failed.
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// Validator for reset password.
/// </summary>
public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordViewModel>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("New password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number")
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");

        RuleFor(x => x.PasswordConfirm)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");
    }
}

#endregion

/// <summary>
/// Controller for authenticated password reset.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class ResetPasswordController(
    IUserRepository userRepository,
    IAccountSettingsRepository accountSettingsRepository,
    IUserService userService,
    IValidator<ResetPasswordViewModel> validator,
    IModelStateService modelStateService,
    ILogger<ResetPasswordController> logger) : Controller
{
    /// <summary>
    /// Route URL for reset password.
    /// </summary>
    public const string RouteUrl = "Account/ResetPassword";

    /// <summary>
    /// Displays the password reset form.
    /// Requires authentication.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [Authorize]
    public IActionResult ResetPassword() =>
        View("~/Features/ResetPassword/ResetPasswordManual.cshtml");

    /// <summary>
    /// Processes the password reset.
    /// </summary>
    [HttpPost]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var resetPasswordUrl = await accountSettingsRepository.GetAccountResetPasswordUrlAsync(GetUrl());

        var validationResult = await validator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return Redirect(resetPasswordUrl);
        }

        var userName = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(userName))
        {
            model.Succeeded = false;
            model.Error = "User not authenticated";
            modelStateService.StoreViewModel(TempData, model);
            return Redirect(resetPasswordUrl);
        }

        try
        {
            var userResult = await userRepository.GetUserAsync(userName);
            if (userResult.TryGetValue(out var user))
            {
                await userService.ResetPasswordAsync(user, model.Password, model.CurrentPassword);
                model.Succeeded = true;
            }
            else
            {
                model.Succeeded = false;
                model.Error = userResult.Error;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting password for user {UserName}", userName);
            model.Succeeded = false;
            model.Error = "An error occurred while changing the password.";
        }

        // Clear passwords from temp storage
        model.Password = string.Empty;
        model.PasswordConfirm = string.Empty;
        model.CurrentPassword = string.Empty;

        modelStateService.StoreViewModel(TempData, model);
        return Redirect(resetPasswordUrl);
    }

    /// <summary>
    /// Gets the reset password URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
