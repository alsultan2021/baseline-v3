using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Kentico.Membership;
using CMS.Membership;

namespace Baseline.Account;

/// <summary>
/// Implementation of external authentication service.
/// </summary>
/// <typeparam name="TUser">The application user type.</typeparam>
/// <remarks>
/// Note: ApplicationUser's extended properties (FirstName, LastName, Created) require
/// extending MemberInfo via the Modules application and creating a custom ApplicationUser subclass.
/// This implementation uses the base ApplicationUser and leaves extended fields empty.
/// Override this service in your project to use extended ApplicationUser properties.
/// </remarks>
public class ExternalAuthenticationService<TUser>(
    SignInManager<TUser> signInManager,
    UserManager<TUser> userManager,
    IOptions<BaselineAccountOptions> options) : IExternalAuthenticationService
    where TUser : ApplicationUser, new()
{
    public IEnumerable<ExternalProvider> GetProviders()
    {
        var providers = new List<ExternalProvider>();
        var authOptions = options.Value.ExternalAuth;

        if (authOptions.Microsoft is not null)
        {
            providers.Add(new ExternalProvider("Microsoft", "Microsoft", "fab fa-microsoft"));
        }

        if (authOptions.Google is not null)
        {
            providers.Add(new ExternalProvider("Google", "Google", "fab fa-google"));
        }

        if (authOptions.Facebook is not null)
        {
            providers.Add(new ExternalProvider("Facebook", "Facebook", "fab fa-facebook"));
        }

        foreach (var oidc in authOptions.CustomOidc)
        {
            providers.Add(new ExternalProvider(oidc.Scheme, oidc.DisplayName));
        }

        return providers;
    }

    public Task<ChallengeResult> ChallengeAsync(string provider, string returnUrl)
    {
        var callbackUrl = $"/Account/ExternalLoginCallback?returnUrl={Uri.EscapeDataString(returnUrl)}";
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, callbackUrl);

        return Task.FromResult(new ChallengeResult(provider, properties));
    }

    public async Task<ExternalAuthResult> HandleCallbackAsync()
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            return ExternalAuthResult.Failed("External login information not available.");
        }

        // Try to sign in with the external login
        var signInResult = await signInManager.ExternalLoginSignInAsync(
            info.LoginProvider,
            info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: false);

        if (signInResult.Succeeded)
        {
            var user = await userManager.FindByLoginAsync(
                info.LoginProvider,
                info.ProviderKey);

            return user is not null
                ? ExternalAuthResult.Succeeded(MapToBaselineUser(user))
                : ExternalAuthResult.Failed("User not found.");
        }

        // User doesn't have an account, try to create one
        var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return ExternalAuthResult.Failed("Email not provided by external provider.");
        }

        // Check if user with this email already exists
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            // Link the external login to existing account
            var linkResult = await userManager.AddLoginAsync(existingUser, info);
            if (!linkResult.Succeeded)
            {
                return ExternalAuthResult.Failed(
                    "Failed to link external login to existing account.");
            }

            await signInManager.SignInAsync(existingUser, isPersistent: false);
            return ExternalAuthResult.Succeeded(MapToBaselineUser(existingUser));
        }

        // Create new user
        var newUser = new TUser()
        {
            UserName = email,
            Email = email,
            // Note: FirstName/LastName require extending ApplicationUser in your project
            // See: https://docs.kentico.com/documentation/developers-and-admins/development/registration-and-authentication/add-fields-to-member-objects
            EmailConfirmed = true, // Email is verified by external provider
            Enabled = true
        };

        var createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            return ExternalAuthResult.Failed(
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        var addLoginResult = await userManager.AddLoginAsync(newUser, info);
        if (!addLoginResult.Succeeded)
        {
            return ExternalAuthResult.Failed("Failed to add external login.");
        }

        await signInManager.SignInAsync(newUser, isPersistent: false);
        return ExternalAuthResult.Succeeded(MapToBaselineUser(newUser), isNew: true);
    }

    public async Task<LinkResult> LinkExternalLoginAsync(
        int userId,
        string provider,
        string providerKey)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return LinkResult.Failed("User not found.");
        }

        var info = new UserLoginInfo(provider, providerKey, provider);
        var result = await userManager.AddLoginAsync(user, info);

        return result.Succeeded
            ? LinkResult.Succeeded()
            : LinkResult.Failed(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<UnlinkResult> UnlinkExternalLoginAsync(int userId, string provider)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UnlinkResult.Failed("User not found.");
        }

        var logins = await userManager.GetLoginsAsync(user);
        var loginToRemove = logins.FirstOrDefault(l => l.LoginProvider == provider);

        if (loginToRemove is null)
        {
            return UnlinkResult.Failed("External login not found.");
        }

        // Ensure user has another way to sign in
        var hasPassword = await userManager.HasPasswordAsync(user);
        if (!hasPassword && logins.Count == 1)
        {
            return UnlinkResult.Failed(
                "Cannot remove the only login method. Please set a password first.");
        }

        var result = await userManager.RemoveLoginAsync(
            user,
            loginToRemove.LoginProvider,
            loginToRemove.ProviderKey);

        return result.Succeeded
            ? UnlinkResult.Succeeded()
            : UnlinkResult.Failed(string.Join(", ", result.Errors.Select(e => e.Description)));
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
