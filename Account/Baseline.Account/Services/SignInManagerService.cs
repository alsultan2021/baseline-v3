using Kentico.Membership;
using Microsoft.AspNetCore.Identity;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Baseline.Account;

/// <summary>
/// v3 implementation of ISignInManagerService using ASP.NET Identity.
/// </summary>
/// <typeparam name="TUser">The application user type that extends ApplicationUser.</typeparam>
public sealed class SignInManagerService<TUser>(
    SignInManager<TUser> signInManager,
    UserManager<TUser> userManager) : ISignInManagerService
    where TUser : ApplicationUser, new()
{
    /// <inheritdoc/>
    public async Task<SignInResult> PasswordSignInAsync(string username, string password, bool rememberMe, bool lockoutOnFailure)
    {
        var result = await signInManager.PasswordSignInAsync(username, password, rememberMe, lockoutOnFailure);
        return MapSignInResult(result);
    }

    /// <inheritdoc/>
    public async Task SignInAsync(IUser user, bool isPersistent)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is not null)
        {
            await signInManager.SignInAsync(identityUser, isPersistent);
        }
    }

    /// <inheritdoc/>
    public async Task SignOutAsync()
    {
        await signInManager.SignOutAsync();
    }

    /// <inheritdoc/>
    public async Task<bool> IsLockedOutAsync(IUser user)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        return identityUser is not null && await userManager.IsLockedOutAsync(identityUser);
    }

    /// <inheritdoc/>
    public async Task<IUser?> GetTwoFactorAuthenticationUserAsync()
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public async Task<SignInResult> TwoFactorSignInAsync(string provider, string code, bool rememberMe, bool rememberBrowser)
    {
        var result = await signInManager.TwoFactorSignInAsync(provider, code, rememberMe, rememberBrowser);
        return MapSignInResult(result);
    }

    /// <inheritdoc/>
    public async Task<bool> IsTwoFactorClientRememberedByNameAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return false;
        }
        return await signInManager.IsTwoFactorClientRememberedAsync(user);
    }

    /// <inheritdoc/>
    public async Task SignInByNameAsync(string username, bool isPersistent)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            await signInManager.SignInAsync(user, isPersistent);
        }
    }

    /// <inheritdoc/>
    public async Task<SignInResult> PasswordSignInByNameAsync(string username, string password, bool isPersistent, bool lockoutOnFailure)
    {
        var result = await signInManager.PasswordSignInAsync(username, password, isPersistent, lockoutOnFailure);
        return MapSignInResult(result);
    }

    /// <inheritdoc/>
    public async Task RememberTwoFactorClientByNameAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            await signInManager.RememberTwoFactorClientAsync(user);
        }
    }

    private static SignInResult MapSignInResult(IdentitySignInResult result)
    {
        return new SignInResult
        {
            Succeeded = result.Succeeded,
            IsLockedOut = result.IsLockedOut,
            IsNotAllowed = result.IsNotAllowed,
            RequiresTwoFactor = result.RequiresTwoFactor
        };
    }

    /// <summary>
    /// Adapter to expose ApplicationUser as IUser.
    /// </summary>
    private sealed class UserAdapter(TUser user) : IUser
    {
        public int UserId => user.Id;
        public Guid UserGuid => Guid.Empty;
        public string UserName => user.UserName ?? string.Empty;
        public string Email => user.Email ?? string.Empty;
        // ApplicationUser base class doesn't have FirstName/LastName - return empty
        public string FirstName => string.Empty;
        public string LastName => string.Empty;
        public bool Enabled => user.Enabled;
        public bool IsExternal => user.IsExternal;
    }
}
