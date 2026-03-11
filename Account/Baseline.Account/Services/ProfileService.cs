using CMS.Membership;
using Microsoft.AspNetCore.Identity;
using Kentico.Membership;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// Implementation of profile service using Xperience by Kentico Identity.
/// </summary>
/// <typeparam name="TUser">The application user type.</typeparam>
/// <remarks>
/// When TUser is or inherits from <see cref="ApplicationUserBaseline"/>, extended properties
/// (FirstName, LastName) are automatically mapped. Otherwise they remain null.
/// </remarks>
public class ProfileService<TUser>(
    UserManager<TUser> userManager,
    IAuthenticationService authService) : IProfileService
    where TUser : ApplicationUser, new()
{
    public async Task<UserProfile?> GetProfileAsync()
    {
        var currentUser = await authService.GetCurrentUserAsync();
        return currentUser is null ? null : await GetProfileByIdAsync(currentUser.Id);
    }

    public async Task<UserProfile?> GetProfileByIdAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var logins = await userManager.GetLoginsAsync(user);
        var twoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user);

        return new UserProfile
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            // Map name fields from ApplicationUserBaseline when available
            FirstName = (user as ApplicationUserBaseline)?.MemberFirstName,
            LastName = (user as ApplicationUserBaseline)?.MemberLastName,
            TwoFactorEnabled = twoFactorEnabled,
            ExternalLogins = logins.Select(l => new ExternalLoginInfo(
                l.LoginProvider,
                l.ProviderKey,
                null,
                l.ProviderDisplayName))
        };
    }

    public async Task<ProfileUpdateResult> UpdateProfileAsync(ProfileUpdateRequest request)
    {
        var currentUser = await authService.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return ProfileUpdateResult.Failed("User not authenticated.");
        }

        var user = await userManager.FindByIdAsync(currentUser.Id.ToString());
        if (user is null)
        {
            return ProfileUpdateResult.Failed("User not found.");
        }

        // Update name fields when the user type supports it
        if (user is ApplicationUserBaseline baselineUser)
        {
            if (request.FirstName is not null)
            {
                baselineUser.MemberFirstName = request.FirstName;
            }
            if (request.LastName is not null)
            {
                baselineUser.MemberLastName = request.LastName;
            }
        }

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ProfileUpdateResult.Failed(errors);
        }

        var updatedProfile = await GetProfileByIdAsync(currentUser.Id);
        return ProfileUpdateResult.Succeeded(updatedProfile!);
    }

    public Task<ProfilePictureResult> UploadProfilePictureAsync(
        Stream imageStream,
        string fileName)
    {
        // This would typically:
        // 1. Validate the image (size, format)
        // 2. Resize/optimize the image
        // 3. Upload to media library or blob storage
        // 4. Update user's profile picture URL

        // For now, return a placeholder result
        return Task.FromResult(ProfilePictureResult.Failed(
            "Profile picture upload not yet implemented."));
    }

    public async Task<AccountDeletionResult> DeleteAccountAsync(string confirmPassword)
    {
        var currentUser = await authService.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return AccountDeletionResult.Failed("User not authenticated.");
        }

        var user = await userManager.FindByIdAsync(currentUser.Id.ToString());
        if (user is null)
        {
            return AccountDeletionResult.Failed("User not found.");
        }

        // Verify password
        var passwordValid = await userManager.CheckPasswordAsync(user, confirmPassword);
        if (!passwordValid)
        {
            return AccountDeletionResult.Failed("Invalid password.");
        }

        // Delete the user
        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return AccountDeletionResult.Failed(errors);
        }

        // Sign out the user
        await authService.SignOutAsync();

        return AccountDeletionResult.Succeeded();
    }
}
