using System.Security.Cryptography;
using System.Text;
using Baseline.Account;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MsSignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using V3SignInResult = Baseline.Account.SignInResult;
// External view models for chevalroyal template overrides
using ExternalLogInViewModel = Account.Features.Account.LogIn.LogInViewModel;
using ExternalTwoFactorViewModel = Account.Features.Account.LogIn.TwoFormAuthenticationViewModel;

namespace Baseline.Account.RCL.Features.LogIn;

#region View Models

/// <summary>
/// View model for login (used by LogInManual.cshtml).
/// </summary>
public sealed class LogInViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool StayLogedIn { get; set; }
    public string? ReturnUrl { get; set; }
    public MsSignInResult? ResultOfSignIn { get; set; }
    public string? ResendConfirmationToken { get; set; }
}

/// <summary>
/// View model for two-factor authentication (used by TwoFactorAuthenticationManual.cshtml).
/// </summary>
public sealed class TwoFactorAuthenticationViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public bool StayLoggedIn { get; set; }
    public bool RememberDevice { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
}

#endregion

/// <summary>
/// Controller for user login.
/// </summary>
[TypeFilter(typeof(LanguagePrefixRouteFilter))]
public sealed class LogInController(
    IUserRepository userRepository,
    IAccountSettingsRepository accountSettingsRepository,
    IUserService userService,
    ISignInManagerService signInManagerService,
    IUserManagerService userManagerService,
    IAuthenticationConfigurations authenticationConfigurations,
    IModelStateService modelStateService,
    ILoginAuditService loginAuditService,
    IOptions<LoginAuditOptions> loginAuditOptions,
    ILogger<LogInController> logger) : Controller
{
    /// <summary>
    /// Route URL for login.
    /// </summary>
    public const string RouteUrl = "Account/LogIn";

    /// <summary>
    /// Route URL for two-factor authentication.
    /// </summary>
    public const string TwoFactorRouteUrl = "Account/TwoFactorAuthentication";

    /// <summary>
    /// Route URL for external login callback.
    /// </summary>
    public const string ExternalLoginCallbackUrl = "Account/ExternalLoginCallback";

    /// <summary>
    /// Displays the login form.
    /// </summary>
    [HttpGet]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    public async Task<IActionResult> LogIn([FromQuery] string returnUrl = "")
    {
        // Server-side redirect if already authenticated
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return await RedirectAfterLoginAsync(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;

        // Create v2-compatible view model with all properties for chevalroyal templates
        var model = new ExternalLogInViewModel
        {
            ReturnUrl = returnUrl,
            AlreadyLogedIn = false,
            MyAccountUrl = await accountSettingsRepository.GetAccountMyAccountUrlAsync("/Account/MyAccount"),
            ForgotPassword = await accountSettingsRepository.GetAccountForgotPasswordUrlAsync("/Account/ForgotPassword"),
            RegistrationUrl = await accountSettingsRepository.GetAccountRegistrationUrlAsync("/Account/Registration")
        };

        // Try to restore model from TempData (for post-redirect-get pattern)
        var storedModel = modelStateService.RetrieveViewModel<ExternalLogInViewModel>(TempData);
        if (storedModel != null)
        {
            model = storedModel;
            model.AlreadyLogedIn = User.Identity?.IsAuthenticated ?? false;
            model.MyAccountUrl = await accountSettingsRepository.GetAccountMyAccountUrlAsync("/Account/MyAccount");
            model.ForgotPassword = await accountSettingsRepository.GetAccountForgotPasswordUrlAsync("/Account/ForgotPassword");
            model.RegistrationUrl = await accountSettingsRepository.GetAccountRegistrationUrlAsync("/Account/Registration");
        }

        // Return view with v2-compatible path that allows overrides
        return View("~/Features/Account/LogIn/LogIn.cshtml", model);
    }

    /// <summary>
    /// Processes the login attempt.
    /// </summary>
    [HttpPost]
    [Route(RouteUrl)]
    [Route("{language}/" + RouteUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogIn(ExternalLogInViewModel model, [FromQuery] string? returnUrl = null)
    {
        var loginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(GetUrl());

        if (!ModelState.IsValid)
        {
            return Redirect(loginUrl);
        }

        model.ResultOfSignIn = MsSignInResult.Failed;

        try
        {
            // Try to find user by username or email
            var userResult = await userRepository.GetUserAsync(model.UserName ?? string.Empty);
            if (userResult.IsFailure)
            {
                userResult = await userRepository.GetUserByEmailAsync(model.UserName ?? string.Empty);
            }

            if (userResult.IsFailure)
            {
                ModelState.AddModelError(nameof(model.UserName), "Invalid username or password");
                model.Password = string.Empty;
                modelStateService.StoreViewModel(TempData, model);
                return Redirect(loginUrl);
            }

            var user = userResult.Value;
            var passwordValid = await userManagerService.CheckPasswordByNameAsync(user.UserName, model.Password ?? string.Empty);

            // Check if user is enabled
            if (passwordValid && !user.Enabled)
            {
                model.ResultOfSignIn = MsSignInResult.NotAllowed;
                model.ResendConfirmationToken = await GenerateResendTokenAsync(user.UserName);
            }

            // Handle two-factor authentication
            if (user.Enabled && passwordValid && authenticationConfigurations.UseTwoFormAuthentication())
            {
                // Check if client has remembered 2FA cookie
                if (await signInManagerService.IsTwoFactorClientRememberedByNameAsync(user.UserName))
                {
                    await signInManagerService.SignInByNameAsync(user.UserName, model.StayLogedIn);
                    await LogSuccessfulLoginAsync(user.UserId, user.UserName);
                    return await RedirectAfterLoginAsync(model.ReturnUrl ?? returnUrl);
                }

                // Check if IP is trusted (user completed 2FA from this IP recently)
                // This allows switching browsers on the same network without re-triggering 2FA
                var clientIp = loginAuditService.GetCurrentClientIpAddress();
                if (!string.IsNullOrEmpty(clientIp) && user.UserId > 0)
                {
                    var trustWindow = loginAuditOptions.Value.TwoFactorIpTrustWindow;
                    if (await loginAuditService.IsTwoFactorIpTrustedAsync(user.UserId, clientIp, trustWindow))
                    {
                        logger.LogInformation(
                            "Skipping 2FA for user {UserName} - IP {IpAddress} is trusted",
                            user.UserName, clientIp);
                        await signInManagerService.SignInByNameAsync(user.UserName, model.StayLogedIn);
                        await LogSuccessfulLoginAsync(user.UserId, user.UserName);
                        return await RedirectAfterLoginAsync(model.ReturnUrl ?? returnUrl);
                    }
                }

                // Send verification code and redirect to 2FA page
                var token = await userManagerService.GenerateTwoFactorTokenByNameAsync(user.UserName, "Email");
                await userService.SendVerificationCodeEmailAsync(user, token);

                var twoFactorModel = new ExternalTwoFactorViewModel
                {
                    ReturnUrl = model.ReturnUrl ?? returnUrl ?? string.Empty,
                    UserName = user.UserName,
                    StayLoggedIn = model.StayLogedIn
                };

                modelStateService.StoreViewModel(TempData, twoFactorModel);
                ModelState.Clear();

                var twoFactorUrl = await accountSettingsRepository.GetAccountTwoFormAuthenticationUrlAsync($"/{TwoFactorRouteUrl}");
                return Redirect(twoFactorUrl);
            }

            // Normal sign-in
            if (passwordValid && user.Enabled)
            {
                var v3Result = await signInManagerService.PasswordSignInByNameAsync(
                    user.UserName, model.Password ?? string.Empty, model.StayLogedIn, lockoutOnFailure: true);
                model.ResultOfSignIn = ConvertToIdentitySignInResult(v3Result);

                if (v3Result.Succeeded)
                {
                    await LogSuccessfulLoginAsync(user.UserId, user.UserName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login for user {UserName}", model.UserName);
        }

        model.Password = string.Empty;
        modelStateService.StoreViewModel(TempData, model);

        if (model.ResultOfSignIn != MsSignInResult.Success)
        {
            logger.LogWarning("Login failed for {UserName}", model.UserName);
            ModelState.AddModelError(string.Empty, "Authentication failed");
            return Redirect(loginUrl);
        }

        return await RedirectAfterLoginAsync(model.ReturnUrl ?? returnUrl);
    }

    /// <summary>
    /// Displays the two-factor authentication form.
    /// </summary>
    [HttpGet]
    [Route(TwoFactorRouteUrl)]
    [Route("{language}/" + TwoFactorRouteUrl)]
    public async Task<IActionResult> TwoFactorAuthentication()
    {
        var storedModel = modelStateService.RetrieveViewModel<ExternalTwoFactorViewModel>(TempData);
        var model = storedModel ?? new ExternalTwoFactorViewModel();

        // If no username, redirect to login - user likely refreshed the page
        if (string.IsNullOrEmpty(model.UserName))
        {
            var loginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(GetUrl());
            return Redirect(loginUrl);
        }

        // Re-store the model so it persists if page is refreshed
        modelStateService.StoreViewModel(TempData, model);

        return View("~/Features/Account/LogIn/TwoFormAuthentication.cshtml", model);
    }

    /// <summary>
    /// Processes the two-factor authentication.
    /// </summary>
    [HttpPost]
    [Route(TwoFactorRouteUrl)]
    [Route("{language}/" + TwoFactorRouteUrl)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TwoFactorAuthentication(ExternalTwoFactorViewModel model)
    {
        var twoFactorUrl = await accountSettingsRepository.GetAccountTwoFormAuthenticationUrlAsync($"/{TwoFactorRouteUrl}");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            model.ErrorMessage = string.Join(", ", errors);
            if (string.IsNullOrEmpty(model.ErrorMessage))
            {
                model.ErrorMessage = "Please enter a valid 6-digit code";
            }
            modelStateService.StoreViewModel(TempData, model);
            return Redirect(twoFactorUrl);
        }

        try
        {
            var userResult = await userRepository.GetUserAsync(model.UserName ?? string.Empty);
            if (userResult.IsFailure)
            {
                userResult = await userRepository.GetUserByEmailAsync(model.UserName ?? string.Empty);
            }

            if (userResult.IsFailure)
            {
                model.ErrorMessage = "User not found";
                modelStateService.StoreViewModel(TempData, model);
                return Redirect(twoFactorUrl);
            }

            var user = userResult.Value;
            var isValid = await userManagerService.VerifyTwoFactorTokenByNameAsync(
                user.UserName, "Email", model.Code ?? string.Empty);

            if (isValid)
            {
                // Log successful 2FA - this also establishes IP trust for future logins
                await loginAuditService.LogTwoFactorAttemptAsync(user.UserId, user.UserName, success: true);

                await signInManagerService.SignInByNameAsync(user.UserName, model.StayLoggedIn);

                if (model.RememberDevice)
                {
                    await signInManagerService.RememberTwoFactorClientByNameAsync(user.UserName);
                }

                return await RedirectAfterLoginAsync(model.ReturnUrl);
            }

            // Log failed 2FA attempt
            await loginAuditService.LogTwoFactorAttemptAsync(user.UserId, user.UserName, success: false);
            model.ErrorMessage = "Invalid verification code";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during two-factor authentication for user {UserName}", model.UserName ?? "unknown");
            model.ErrorMessage = "An error occurred during verification";
        }

        modelStateService.StoreViewModel(TempData, model);
        return Redirect(twoFactorUrl);
    }

    /// <summary>
    /// Resends the verification code.
    /// </summary>
    [HttpPost]
    [Route("Account/ResendVerificationCode")]
    [Route("{language}/Account/ResendVerificationCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendVerificationCode(ExternalTwoFactorViewModel model)
    {
        var twoFactorUrl = await accountSettingsRepository.GetAccountTwoFormAuthenticationUrlAsync($"/{TwoFactorRouteUrl}");

        try
        {
            var userResult = await userRepository.GetUserAsync(model.UserName ?? string.Empty);
            if (userResult.IsFailure)
            {
                userResult = await userRepository.GetUserByEmailAsync(model.UserName ?? string.Empty);
            }

            if (userResult.TryGetValue(out var user))
            {
                var token = await userManagerService.GenerateTwoFactorTokenByNameAsync(user.UserName, "Email");
                await userService.SendVerificationCodeEmailAsync(user, token);
                model.ErrorMessage = null; // Clear any previous errors
            }
            else
            {
                model.ErrorMessage = "User not found";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resending verification code for user {UserName}", model.UserName);
            model.ErrorMessage = "Failed to resend code. Please try again.";
        }

        // Store model back in TempData so it persists
        modelStateService.StoreViewModel(TempData, model);
        return Redirect(twoFactorUrl);
    }

    private async Task<string> GenerateResendTokenAsync(string userName)
    {
        var securityStamp = await userManagerService.GetSecurityStampAsync(userName);
        var salt = "f3a8-09GHFB:O#$fp939o4gq4q2h;fa"u8;
        var input = Encoding.UTF8.GetBytes($"{userName}{securityStamp}".ToLowerInvariant());
        var hash = HMACSHA256.HashData(salt, input);
        return Convert.ToBase64String(hash);
    }

    private async Task<IActionResult> RedirectAfterLoginAsync(string? returnUrl)
    {
        modelStateService.ClearViewModel(TempData);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var myAccountUrl = await accountSettingsRepository.GetAccountMyAccountUrlAsync(
            MyAccount.MyAccountController.GetUrl());
        return Redirect(myAccountUrl);
    }

    /// <summary>
    /// Logs a successful login attempt.
    /// </summary>
    private async Task LogSuccessfulLoginAsync(int memberId, string username)
    {
        try
        {
            await loginAuditService.LogLoginAttemptAsync(new LoginAttemptContext
            {
                MemberId = memberId,
                Username = username,
                IsSuccess = true,
                ActionType = LoginAuditActionType.LoginSuccess
            });
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to log login attempt for {Username}", username);
        }
    }

    /// <summary>
    /// Converts v3 SignInResult to Microsoft.AspNetCore.Identity.SignInResult for v2 compatibility.
    /// </summary>
    private static MsSignInResult ConvertToIdentitySignInResult(V3SignInResult v3Result)
    {
        if (v3Result.Succeeded)
            return MsSignInResult.Success;
        if (v3Result.IsLockedOut)
            return MsSignInResult.LockedOut;
        if (v3Result.IsNotAllowed)
            return MsSignInResult.NotAllowed;
        if (v3Result.RequiresTwoFactor)
            return MsSignInResult.TwoFactorRequired;
        return MsSignInResult.Failed;
    }

    /// <summary>
    /// Gets the login URL.
    /// </summary>
    public static string GetUrl() => $"/{RouteUrl}";

    /// <summary>
    /// Initiates external login (Google, Microsoft, Facebook, etc.).
    /// </summary>
    [HttpPost]
    [Route("Account/ExternalLogin")]
    [Route("{language}/Account/ExternalLogin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLogin(string provider, string? returnUrl = null)
    {
        var extAuth = HttpContext.RequestServices.GetService<IExternalAuthenticationService>();
        if (extAuth is null)
        {
            logger.LogWarning("External auth not configured, provider={Provider}", provider);
            TempData["LoginError"] = "External login is not configured.";
            var loginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(GetUrl());
            return Redirect(loginUrl);
        }

        var safeReturnUrl = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : "/";
        var challenge = await extAuth.ChallengeAsync(provider, safeReturnUrl);
        return Challenge(challenge.Properties, challenge.Scheme);
    }

    /// <summary>
    /// Handles callback from external provider after user authenticates.
    /// </summary>
    [HttpGet]
    [Route(ExternalLoginCallbackUrl)]
    [Route("{language}/" + ExternalLoginCallbackUrl)]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? returnUrl = null)
    {
        var loginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(GetUrl());
        var extAuth = HttpContext.RequestServices.GetService<IExternalAuthenticationService>();

        if (extAuth is null)
        {
            TempData["LoginError"] = "External login is not configured.";
            return Redirect(loginUrl);
        }

        try
        {
            var result = await extAuth.HandleCallbackAsync();
            if (result.Success)
            {
                return await RedirectAfterLoginAsync(returnUrl);
            }

            logger.LogWarning("External login failed: {Error}", result.ErrorMessage);
            TempData["LoginError"] = result.ErrorMessage ?? "External login failed.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during external login callback");
            TempData["LoginError"] = "An error occurred during external login.";
        }

        return Redirect(loginUrl);
    }

    /// <summary>
    /// Completes sign-in after successful passkey/WebAuthn authentication.
    /// </summary>
    [HttpPost]
    [Route("Account/PasskeySignIn")]
    [Route("{language}/Account/PasskeySignIn")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasskeySignIn(int memberId, string username, string? returnUrl = null)
    {
        var loginUrl = await accountSettingsRepository.GetAccountLoginUrlAsync(GetUrl());

        try
        {
            // Verify the user exists and is enabled
            var userResult = await userRepository.GetUserAsync(username);
            if (userResult.IsFailure)
            {
                userResult = await userRepository.GetUserByEmailAsync(username);
            }

            if (userResult.IsFailure)
            {
                logger.LogWarning("Passkey sign-in failed: user {Username} not found", username);
                TempData["LoginError"] = "Authentication failed.";
                return Redirect(loginUrl);
            }

            var user = userResult.Value;

            // Verify the member ID matches
            if (user.UserId != memberId)
            {
                logger.LogWarning("Passkey sign-in failed: member ID mismatch for user {Username}", username);
                TempData["LoginError"] = "Authentication failed.";
                return Redirect(loginUrl);
            }

            // Check if user is enabled
            if (!user.Enabled)
            {
                logger.LogWarning("Passkey sign-in failed: user {Username} is not enabled", username);
                TempData["LoginError"] = "Your account is not enabled. Please confirm your email address.";
                return Redirect(loginUrl);
            }

            // Sign in the user (passkey authentication is already verified)
            await signInManagerService.SignInByNameAsync(user.UserName, isPersistent: true);

            // Log the successful passkey login
            await loginAuditService.LogLoginAttemptAsync(new LoginAttemptContext
            {
                MemberId = user.UserId,
                Username = user.UserName,
                IsSuccess = true,
                ActionType = "PasskeyLogin"
            });

            logger.LogInformation("Passkey sign-in successful for user {Username}", username);

            return await RedirectAfterLoginAsync(returnUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during passkey sign-in for user {Username}", username);
            TempData["LoginError"] = "An error occurred during authentication.";
            return Redirect(loginUrl);
        }
    }
}
