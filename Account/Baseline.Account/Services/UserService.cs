using CMS.EmailEngine;
using CMS.Membership;
using CMS.Notifications;
using Kentico.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// Placeholders for MFA verification code notification email.
/// Register this in the Notifications app with code name "MFAVerificationCode".
/// </summary>
public class MfaVerificationCodePlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "MFAVerificationCode";

    [PlaceholderRequired]
    [PlaceholderDescription("The user's display name or username")]
    public string UserName { get; set; } = string.Empty;

    [PlaceholderRequired]
    [PlaceholderDescription("The 6-digit verification code")]
    public string VerificationCode { get; set; } = string.Empty;

    [PlaceholderDescription("Number of minutes until the code expires")]
    public string ExpiryMinutes { get; set; } = "5";
}

/// <summary>
/// v3 implementation of IUserService using Xperience by Kentico member APIs.
/// </summary>
/// <typeparam name="TUser">The application user type that extends ApplicationUser.</typeparam>
public sealed class UserService<TUser>(
    UserManager<TUser> userManager,
    IEmailService emailService,
    INotificationEmailMessageProvider notificationEmailMessageProvider,
    ILogger<UserService<TUser>> logger,
    IOptions<BaselineAccountOptions> options) : IUserService
    where TUser : ApplicationUser, new()
{
    /// <inheritdoc/>
    public async Task<IUser?> GetUserAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public async Task<IUser?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public async Task<IUser?> GetUserByUsernameAsync(string username)
    {
        var user = await userManager.FindByNameAsync(username);
        return user is null ? null : new UserAdapter(user);
    }

    /// <inheritdoc/>
    public Task<IUser?> CreateUserAsync(CreateUserRequest request)
    {
        // Implement async user creation
        var user = new TUser
        {
            UserName = request.Username,
            Email = request.Email
            // Note: FirstName/LastName are not in base ApplicationUser class
        };

        return Task.FromResult<IUser?>(new UserAdapter(user));
    }

    /// <inheritdoc/>
    public async Task<bool> ValidatePasswordAsync(string password)
    {
        // Use UserManager's password validators
        foreach (var validator in userManager.PasswordValidators)
        {
            var result = await validator.ValidateAsync(userManager, null!, password);
            if (!result.Succeeded)
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public async Task<CSharpFunctionalExtensions.Result<IUser>> CreateUser(IUser user, string password, bool enabled = true)
    {
        var newUser = new TUser
        {
            UserName = user.UserName,
            Email = user.Email,
            // Note: FirstName/LastName are not in base ApplicationUser class
            Enabled = enabled
        };

        var result = await userManager.CreateAsync(newUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return CSharpFunctionalExtensions.Result.Failure<IUser>($"Failed to create user: {errors}");
        }

        return CSharpFunctionalExtensions.Result.Success<IUser>(new UserAdapter(newUser));
    }

    /// <inheritdoc/>
    public async Task<bool> HasPasswordAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is not null && await userManager.HasPasswordAsync(user);
    }

    /// <inheritdoc/>
    public async Task<CSharpFunctionalExtensions.Result<IUser>> UpgradeGuestUserAsync(string email, string userName, string password)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            return CSharpFunctionalExtensions.Result.Failure<IUser>("User not found");
        }

        if (await userManager.HasPasswordAsync(existingUser))
        {
            return CSharpFunctionalExtensions.Result.Failure<IUser>("Account already has a password. Please log in or use forgot password.");
        }

        // Update username if different
        if (!string.Equals(existingUser.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            existingUser.UserName = userName;
        }

        // Disable until email confirmation
        existingUser.Enabled = false;

        var updateResult = await userManager.UpdateAsync(existingUser);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return CSharpFunctionalExtensions.Result.Failure<IUser>($"Failed to update user: {errors}");
        }

        // Add password
        var passwordResult = await userManager.AddPasswordAsync(existingUser, password);
        if (!passwordResult.Succeeded)
        {
            var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
            return CSharpFunctionalExtensions.Result.Failure<IUser>($"Failed to set password: {errors}");
        }

        return CSharpFunctionalExtensions.Result.Success<IUser>(new UserAdapter(existingUser));
    }

    /// <inheritdoc/>
    public async Task SendRegistrationConfirmationEmailAsync(IUser user, string confirmationUrl)
    {
        // Generate email confirmation token
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        var token = identityUser is not null
            ? await userManager.GenerateEmailConfirmationTokenAsync(identityUser)
            : string.Empty;

        // Append userId and token to the confirmation URL
        var separator = confirmationUrl.Contains('?') ? '&' : '?';
        var fullUrl = $"{confirmationUrl}{separator}userId={user.UserId}&token={Uri.EscapeDataString(token)}";

        var message = new EmailMessage
        {
            From = options.Value.SystemEmailFrom ?? "no-reply@localhost",
            Recipients = user.Email,
            Subject = "Confirm your registration",
            Body = $"Please confirm your registration by clicking <a href=\"{fullUrl}\">here</a>."
        };
        await emailService.SendEmail(message);
    }

    /// <inheritdoc/>
    public async Task SendVerificationCodeEmailAsync(IUser user, string token)
    {
        var placeholders = new MfaVerificationCodePlaceholders
        {
            UserName = user.UserName,
            VerificationCode = token,
            ExpiryMinutes = "5"
        };

        // Note: NotificationEmailMessageProvider.CreateEmailMessage requires a CMS User ID
        // (CMS_User table), but Members (front-end users) only have Member IDs.
        // Use GetCmsUserIdForNotification to find a valid CMS User ID or fallback to system user.
        int cmsUserId = GetCmsUserIdForNotification(user.Email);

        var emailMessage = await notificationEmailMessageProvider.CreateEmailMessage(
            placeholders.NotificationEmailName,
            cmsUserId,
            placeholders
        );

        if (emailMessage is null)
        {
            logger.LogWarning("MFA notification template '{Template}' not found, using fallback email",
                placeholders.NotificationEmailName);

            // Fallback to direct email if notification template not configured
            var fallbackMessage = new EmailMessage
            {
                From = options.Value.SystemEmailFrom ?? "no-reply@localhost",
                Recipients = user.Email,
                Subject = "Your verification code",
                Body = $"""
                    <p>Hello {user.UserName},</p>
                    <p>Your verification code is: <strong>{token}</strong></p>
                    <p>This code will expire in 5 minutes.</p>
                    """
            };
            await emailService.SendEmail(fallbackMessage);
            return;
        }

        // Override recipient to ensure it goes to the member's email
        emailMessage.Recipients = user.Email;
        await emailService.SendEmail(emailMessage);
    }

    /// <summary>
    /// Gets a valid CMS User ID for notification emails.
    /// Members don't have CMS User IDs, so we try to find a matching user or fallback to system user.
    /// </summary>
    private static int GetCmsUserIdForNotification(string email)
    {
        // Try to find a CMS User by email
        var user = UserInfo.Provider.Get()
            .Columns(nameof(UserInfo.UserID))
            .WhereEquals(nameof(UserInfo.Email), email)
            .TopN(1)
            .FirstOrDefault();

        if (user != null)
        {
            return user.UserID;
        }

        // Try to find any enabled admin user as fallback
        var adminUser = UserInfo.Provider.Get()
            .Columns(nameof(UserInfo.UserID))
            .WhereTrue(nameof(UserInfo.UserEnabled))
            .TopN(1)
            .FirstOrDefault();

        if (adminUser != null)
        {
            return adminUser.UserID;
        }

        // Fallback to system user (0)
        return 0;
    }

    /// <inheritdoc/>
    public async Task SendPasswordResetEmailAsync(IUser user, string resetUrl)
    {
        var message = new EmailMessage
        {
            From = options.Value.SystemEmailFrom ?? "no-reply@localhost",
            Recipients = user.Email,
            Subject = "Reset your password",
            Body = $"Reset your password by clicking <a href=\"{resetUrl}\">here</a>."
        };
        await emailService.SendEmail(message);
    }

    /// <inheritdoc/>
    public async Task<bool> ResetPasswordAsync(IUser user, string newPassword, string currentPassword)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return false;
        }

        var result = await userManager.ChangePasswordAsync(identityUser, currentPassword, newPassword);
        return result.Succeeded;
    }

    /// <inheritdoc/>
    public async Task<IdentityResult> ResetPasswordFromTokenAsync(IUser user, string token, string newPassword)
    {
        var identityUser = await userManager.FindByIdAsync(user.UserId.ToString());
        if (identityUser is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        var aspNetResult = await userManager.ResetPasswordAsync(identityUser, token, newPassword);
        return aspNetResult.Succeeded
            ? IdentityResult.Success
            : IdentityResult.Failed(aspNetResult.Errors
                .Select(e => new IdentityError { Code = e.Code, Description = e.Description })
                .ToArray());
    }

    /// <summary>
    /// Adapter to expose ApplicationUser as IUser.
    /// </summary>
    private sealed class UserAdapter(TUser user) : IUser
    {
        public int UserId => user.Id;
        public Guid UserGuid => Guid.Empty; // Populated via MemberInfo if needed
        public string UserName => user.UserName ?? string.Empty;
        public string Email => user.Email ?? string.Empty;
        // ApplicationUser base class doesn't have FirstName/LastName - return empty
        public string FirstName => string.Empty;
        public string LastName => string.Empty;
        public bool Enabled => user.Enabled;
        public bool IsExternal => user.IsExternal;
    }
}

