using Baseline.Account;
using Baseline.Core;
using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
// External view models for chevalroyal template overrides
using ExternalRegistrationViewModel = Account.Features.Account.Registration.RegistrationViewModel;
// Use Core's IUrlResolver which is registered in DI
using IUrlResolver = Baseline.Core.IUrlResolver;
using ExternalResendConfirmationViewModel = Account.Features.Account.Registration.ResendConfirmationViewModel;

namespace Baseline.Account.RCL.Features.Registration;

#region View Models

/// <summary>
/// View model for user registration.
/// </summary>
public sealed class RegistrationViewModel
{
    /// <summary>
    /// The user's registration data.
    /// </summary>
    public UserRegistrationData User { get; set; } = new();

    /// <summary>
    /// Password.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation.
    /// </summary>
    public string PasswordConfirm { get; set; } = string.Empty;

    /// <summary>
    /// Whether registration was successful.
    /// </summary>
    public bool RegistrationSuccessful { get; set; }

    /// <summary>
    /// Error message if registration failed.
    /// </summary>
    public string? RegistrationFailureMessage { get; set; }

    /// <summary>
    /// Whether the user agreed to terms.
    /// </summary>
    public bool AgreeToTerms { get; set; }
}

/// <summary>
/// User registration data.
/// </summary>
public sealed class UserRegistrationData
{
    /// <summary>
    /// Username.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// First name.
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name.
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Converts to user interface for service calls.
    /// </summary>
    public IUser GetUser() => new BasicUser
    {
        UserName = UserName,
        Email = Email,
        FirstName = FirstName,
        LastName = LastName
    };
}

/// <summary>
/// Basic user implementation for registration.
/// </summary>
internal sealed class BasicUser : IUser
{
    public int UserId { get; set; }
    public Guid UserGuid { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool IsExternal { get; set; }
}

/// <summary>
/// View model for resending confirmation.
/// </summary>
public sealed class ResendConfirmationViewModel
{
    /// <summary>
    /// Username or email.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Verification token.
    /// </summary>
    public string VerificationCheck { get; set; } = string.Empty;
}

/// <summary>
/// Validator for registration.
/// </summary>
public sealed class RegistrationValidator : AbstractValidator<RegistrationViewModel>
{
    public RegistrationValidator()
    {
        RuleFor(x => x.User.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");

        RuleFor(x => x.User.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address");

        RuleFor(x => x.User.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters");

        RuleFor(x => x.User.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number");

        RuleFor(x => x.PasswordConfirm)
            .NotEmpty().WithMessage("Password confirmation is required")
            .Equal(x => x.Password).WithMessage("Passwords do not match");

        RuleFor(x => x.AgreeToTerms)
            .Equal(true).WithMessage("You must agree to the terms and conditions");
    }
}

#endregion

/// <summary>
/// Controller for user registration.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class RegistrationController(
    IAccountSettingsRepository accountSettingsRepository,
    IUserService userService,
    IUserRepository userRepository,
    IUserManagerService userManagerService,
    Baseline.Core.IUrlResolver urlResolver,
    IValidator<RegistrationViewModel> validator,
    IModelStateService modelStateService,
    ILogger<RegistrationController> logger) : Controller
{
    /// <summary>
    /// Route URL for registration.
    /// </summary>
    public const string RouteUrl = "Account/Registration";

    /// <summary>
    /// Alternate route URL for registration.
    /// </summary>
    public const string AltRouteUrl = "Account/Register";

    /// <summary>
    /// Salt for confirmation hash.
    /// </summary>
    private const string ConfirmationHashSalt = "f3a8-09GHFB:O#$fp939o4gq4q2h;fa";

    /// <summary>
    /// Displays the registration form.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route(AltRouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [Route("{language}/" + AltRouteUrl)]
    public IActionResult Registration()
    {
        var storedModel = modelStateService.RetrieveViewModel<ExternalRegistrationViewModel>(TempData);
        var model = storedModel ?? new ExternalRegistrationViewModel();
        return View("~/Features/Account/Registration/Registration.cshtml", model);
    }

    /// <summary>
    /// Processes the registration.
    /// </summary>
    [HttpPost]
    [Route(RouteUrl)]
    [Route(AltRouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [Route("{language}/" + AltRouteUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registration(RegistrationViewModel model)
    {
        var registrationUrl = await accountSettingsRepository.GetAccountRegistrationUrlAsync(GetUrl());

        var validationResult = await validator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            model.Password = string.Empty;
            model.PasswordConfirm = string.Empty;
            modelStateService.StoreViewModel(TempData, model);
            return Redirect(registrationUrl);
        }

        try
        {
            var newUserResult = await userService.CreateUser(
                model.User.GetUser(),
                model.Password,
                enabled: false);

            // If creation failed, check if a passwordless guest member exists with this email
            if (!newUserResult.IsSuccess
                && !await userService.HasPasswordAsync(model.User.Email))
            {
                logger.LogInformation(
                    "Upgrading guest member {Email} to registered account",
                    model.User.Email);
                newUserResult = await userService.UpgradeGuestUserAsync(
                    model.User.Email, model.User.UserName, model.Password);
            }

            if (!newUserResult.TryGetValue(out var newUser, out var error))
            {
                throw new Exception(error);
            }

            // Send confirmation email
            var confirmationUrl = await accountSettingsRepository.GetAccountConfirmationUrlAsync(
                Confirmation.ConfirmationController.GetUrl());
            await userService.SendRegistrationConfirmationEmailAsync(
                newUser, urlResolver.GetAbsoluteUrl(confirmationUrl));

            // Redirect to confirmation page to show "check your email" message
            return Redirect(confirmationUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering user {UserName}", model.User.UserName);
            model.RegistrationFailureMessage = ex.Message;
            model.RegistrationSuccessful = false;
        }

        // Clear passwords
        model.Password = string.Empty;
        model.PasswordConfirm = string.Empty;
        modelStateService.StoreViewModel(TempData, model);

        return Redirect(registrationUrl);
    }

    /// <summary>
    /// Resends the registration confirmation email.
    /// </summary>
    [HttpPost]
    [Route("Account/ResendRegistration")]
    [Route("{language}/Account/ResendRegistration")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendRegistration(ResendConfirmationViewModel model)
    {
        var loginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(LogIn.LogInController.GetUrl());

        // Find user by username or email
        var userResult = await userRepository.GetUserAsync(model.UserName);
        if (userResult.IsFailure)
        {
            userResult = await userRepository.GetUserByEmailAsync(model.UserName);
        }

        if (!userResult.TryGetValue(out var user))
        {
            return Redirect(loginUrl);
        }

        // Verify hash
        var securityStamp = await userManagerService.GetSecurityStampAsync(user.UserName);
        var hash = $"{user.UserName}{securityStamp}{ConfirmationHashSalt}".ToLowerInvariant().GetHashCode();
        if (!hash.ToString().Equals(model.VerificationCheck))
        {
            return Redirect(loginUrl);
        }

        try
        {
            var confirmationUrl = await accountSettingsRepository.GetAccountConfirmationUrlAsync(
                Confirmation.ConfirmationController.GetUrl());
            await userService.SendRegistrationConfirmationEmailAsync(
                user, urlResolver.GetAbsoluteUrl(confirmationUrl));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending confirmation for user {UserName}", model.UserName);
        }

        modelStateService.ClearViewModel(TempData);
        return Redirect(loginUrl);
    }

    /// <summary>
    /// Gets the registration URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";
}
