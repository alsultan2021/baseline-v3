using System.Security.Claims;
using CMS.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kentico.Membership;

namespace Baseline.Account;

/// <summary>
/// Implementation of authentication service using Xperience by Kentico Identity.
/// </summary>
/// <typeparam name="TUser">The application user type.</typeparam>
/// <remarks>
/// Note: ApplicationUser's extended properties (FirstName, LastName, Created) require
/// extending MemberInfo via the Modules application and creating a custom ApplicationUser subclass.
/// This implementation uses the base ApplicationUser and leaves extended fields empty.
/// Override this service in your project to use extended ApplicationUser properties.
/// </remarks>
public class AuthenticationService<TUser>(
    IHttpContextAccessor httpContextAccessor,
    UserManager<TUser> userManager,
    SignInManager<TUser> signInManager,
    ILoginAuditService loginAuditService,
    INewDeviceAlertService newDeviceAlertService,
    IOptions<BaselineAccountOptions> options,
    ILogger<AuthenticationService<TUser>> logger) : IAuthenticationService
    where TUser : ApplicationUser, new()
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public ClaimsPrincipal? CurrentPrincipal =>
        httpContextAccessor.HttpContext?.User;

    public async Task<AuthenticationResult> SignInAsync(
        string username,
        string password,
        bool rememberMe = false)
    {
        var user = await userManager.FindByNameAsync(username)
            ?? await userManager.FindByEmailAsync(username);

        if (user is null)
        {
            // Log failed attempt for unknown user
            await LogLoginAttemptAsync(username, null, LoginAuditActionType.LoginFailed, false, "UserNotFound");

            return AuthenticationResult.Failed(
                "Invalid username or password.",
                AuthErrorCode.InvalidCredentials);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Log successful login and check for new device alert
            await LogSuccessfulLoginAsync(user, username);

            return AuthenticationResult.Succeeded(MapToBaselineUser(user));
        }

        if (result.IsLockedOut)
        {
            await LogLoginAttemptAsync(username, user.Id, LoginAuditActionType.AccountLocked, false, "AccountLocked");

            return AuthenticationResult.Failed(
                "Account is locked. Please try again later.",
                AuthErrorCode.AccountLocked) with
            { IsLockedOut = true };
        }

        if (result.IsNotAllowed)
        {
            await LogLoginAttemptAsync(username, user.Id, LoginAuditActionType.LoginFailed, false, "EmailNotConfirmed");

            return AuthenticationResult.Failed(
                "Sign in is not allowed. Please confirm your email.",
                AuthErrorCode.EmailNotConfirmed) with
            { IsNotAllowed = true };
        }

        if (result.RequiresTwoFactor)
        {
            await LogLoginAttemptAsync(username, user.Id, LoginAuditActionType.TwoFactorRequired, false, "TwoFactorRequired");

            return AuthenticationResult.Failed(
                "Two-factor authentication required.",
                AuthErrorCode.TwoFactorRequired) with
            { RequiresTwoFactor = true };
        }

        await LogLoginAttemptAsync(username, user.Id, LoginAuditActionType.LoginFailed, false, "InvalidPassword");

        return AuthenticationResult.Failed(
            "Invalid username or password.",
            AuthErrorCode.InvalidCredentials);
    }

    private async Task LogLoginAttemptAsync(string username, int? memberId, string actionType, bool success, string? failureReason = null)
    {
        if (!options.Value.EnableLoginAuditing)
            return;

        try
        {
            await loginAuditService.LogLoginAttemptAsync(new LoginAttemptContext
            {
                MemberId = memberId,
                Username = username,
                ActionType = actionType,
                IsSuccess = success,
                FailureReason = failureReason
            });
        }
        catch (Exception ex)
        {
            // Don't let audit logging break authentication flow
            logger.LogWarning(ex, "Failed to log login attempt for {Username}", username);
        }
    }

    private async Task LogSuccessfulLoginAsync(TUser user, string username)
    {
        if (!options.Value.EnableLoginAuditing)
            return;

        try
        {
            // Check if this is a new device before logging
            var httpContext = httpContextAccessor.HttpContext;
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString() ?? "";
            var ipAddress = GetClientIpAddress(httpContext);
            var fingerprint = GenerateSimpleFingerprint(userAgent, ipAddress);

            bool isNewDevice = await loginAuditService.IsNewDeviceAsync(user.Id, fingerprint);

            await loginAuditService.LogLoginAttemptAsync(new LoginAttemptContext
            {
                MemberId = user.Id,
                Username = username,
                ActionType = LoginAuditActionType.LoginSuccess,
                IsSuccess = true
            });

            // Send new device alert if enabled
            if (isNewDevice && options.Value.EnableNewDeviceAlerts && !string.IsNullOrEmpty(user.Email))
            {
                await newDeviceAlertService.SendNewDeviceAlertAsync(new NewDeviceAlertContext
                {
                    Email = user.Email,
                    Username = user.UserName ?? username,
                    IpAddress = ipAddress,
                    LoginTime = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to log successful login for {Username}", username);
        }
    }

    private static string? GetClientIpAddress(HttpContext? httpContext)
    {
        if (httpContext == null)
            return null;

        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
            return forwardedFor.Split(',').First().Trim();

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static string GenerateSimpleFingerprint(string userAgent, string? ipAddress)
    {
        var ipPrefix = !string.IsNullOrEmpty(ipAddress) && ipAddress.Contains('.')
            ? string.Join(".", ipAddress.Split('.').Take(3)) + ".x"
            : ipAddress ?? "";
        var data = $"{userAgent}|{ipPrefix}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task SignOutAsync()
    {
        // Log logout before signing out (so we still have user context)
        if (options.Value.EnableLoginAuditing && IsAuthenticated)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser != null)
                {
                    await loginAuditService.LogLogoutAsync(currentUser.Id, currentUser.UserName);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to log logout event");
            }
        }

        await signInManager.SignOutAsync();
    }

    public async Task<RegistrationResult> RegisterAsync(RegistrationRequest request)
    {
        var user = new TUser()
        {
            UserName = request.UserName ?? request.Email,
            Email = request.Email,
            // Note: FirstName/LastName require extending ApplicationUser in your project
            // See: https://docs.kentico.com/documentation/developers-and-admins/development/registration-and-authentication/add-fields-to-member-objects
            Enabled = true
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return RegistrationResult.Failed(result.Errors.Select(e => e.Description));
        }

        var requiresConfirmation = options.Value.RequireEmailConfirmation;

        return RegistrationResult.Succeeded(
            MapToBaselineUser(user),
            requiresConfirmation);
    }

    public async Task<BaselineUser?> GetCurrentUserAsync()
    {
        if (!IsAuthenticated)
        {
            return null;
        }

        var userId = CurrentPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return null;
        }

        return await GetUserByIdAsync(id);
    }

    public async Task<BaselineUser?> GetUserByIdAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : MapToBaselineUser(user);
    }

    public async Task<BaselineUser?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : MapToBaselineUser(user);
    }

    private static BaselineUser MapToBaselineUser(ApplicationUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        // Extended properties require custom ApplicationUser subclass - leave empty by default
        FirstName = null,
        LastName = null,
        EmailConfirmed = user.EmailConfirmed,
        IsEnabled = user.Enabled,
        CreatedDate = DateTimeOffset.MinValue // ApplicationUser doesn't have Created by default
    };
}
