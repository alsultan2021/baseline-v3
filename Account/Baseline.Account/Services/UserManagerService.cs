using Kentico.Membership;
using Microsoft.AspNetCore.Identity;

namespace Baseline.Account;

/// <summary>
/// v3 implementation of IUserManagerService using ASP.NET Identity.
/// </summary>
/// <typeparam name="TUser">The application user type that extends ApplicationUser.</typeparam>
public sealed class UserManagerService<TUser>(
    UserManager<TUser> userManager) : IUserManagerService
    where TUser : ApplicationUser, new()
{
    /// <inheritdoc/>
    public async Task<IUser?> FindByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public async Task<IUser?> FindByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public async Task<IUser?> FindByNameAsync(string userName)
    {
        var user = await userManager.FindByNameAsync(userName);
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> CreateAsync(IUser user, string password)
    {
        var newUser = new TUser
        {
            UserName = user.UserName,
            Email = user.Email,
            // Note: FirstName/LastName are not in base ApplicationUser class
            Enabled = user.Enabled
        };

        var result = await userManager.CreateAsync(newUser, password);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> AddToRoleAsync(IUser user, string role)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var result = await userManager.AddToRoleAsync(identityUser, role);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> RemoveFromRoleAsync(IUser user, string role)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var result = await userManager.RemoveFromRoleAsync(identityUser, role);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<bool> IsInRoleAsync(IUser user, string role)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        return identityUser is not null && await userManager.IsInRoleAsync(identityUser, role);
    }

    /// <inheritdoc/>
    public async Task<IList<string>> GetRolesAsync(IUser user)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return [];
        }
        return await userManager.GetRolesAsync(identityUser);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateEmailConfirmationTokenAsync(IUser user)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return string.Empty;
        }
        return await userManager.GenerateEmailConfirmationTokenAsync(identityUser);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ConfirmEmailAsync(IUser user, string token)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var result = await userManager.ConfirmEmailAsync(identityUser, token);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<string> GeneratePasswordResetTokenAsync(IUser user)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return string.Empty;
        }
        return await userManager.GeneratePasswordResetTokenAsync(identityUser);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ResetPasswordAsync(IUser user, string token, string newPassword)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var result = await userManager.ResetPasswordAsync(identityUser, token, newPassword);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ChangePasswordAsync(IUser user, string currentPassword, string newPassword)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var result = await userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> UpdateAsync(IUser user)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        identityUser.Email = user.Email;
        // Note: FirstName/LastName are not in base ApplicationUser class
        identityUser.Enabled = user.Enabled;

        var result = await userManager.UpdateAsync(identityUser);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> EnableUserByIdAsync(int userId)
    {
        var identityUser = await userManager.FindByIdAsync(userId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        identityUser.Enabled = true;
        var result = await userManager.UpdateAsync(identityUser);
        return MapIdentityResult(result);
    }

    /// <inheritdoc/>
    public async Task<bool> CheckPasswordByNameAsync(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return false;
        }
        return await userManager.CheckPasswordAsync(user, password);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateTwoFactorTokenByNameAsync(string username, string provider)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return string.Empty;
        }
        return await userManager.GenerateTwoFactorTokenAsync(user, provider);
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyTwoFactorTokenByNameAsync(string username, string provider, string token)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return false;
        }
        return await userManager.VerifyTwoFactorTokenAsync(user, provider, token);
    }

    /// <inheritdoc/>
    public async Task<string> GetSecurityStampAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null)
        {
            return string.Empty;
        }
        return await userManager.GetSecurityStampAsync(user);
    }

    private static IdentityResult MapIdentityResult(Microsoft.AspNetCore.Identity.IdentityResult result)
    {
        return result.Succeeded
            ? IdentityResult.Success
            : IdentityResult.Failed(result.Errors
                .Select(e => new IdentityError { Code = e.Code, Description = e.Description })
                .ToArray());
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
