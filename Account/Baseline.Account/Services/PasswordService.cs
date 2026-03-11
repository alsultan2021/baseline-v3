using CMS.EmailEngine;
using CMS.Membership;
using Microsoft.AspNetCore.Identity;
using Kentico.Membership;
using Microsoft.Extensions.Options;
using System.Web;

namespace Baseline.Account;

/// <summary>
/// Implementation of password service using Xperience by Kentico Identity.
/// </summary>
/// <typeparam name="TUser">The application user type.</typeparam>
public class PasswordService<TUser>(
    UserManager<TUser> userManager,
    IAuthenticationService authService,
    IEmailService emailService,
    IOptions<BaselineAccountOptions> options) : IPasswordService
    where TUser : ApplicationUser, new()
{
    public async Task<PasswordRecoveryResult> InitiateRecoveryAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);

        // Always return success to prevent email enumeration
        if (user is null)
        {
            return PasswordRecoveryResult.Succeeded();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        // Send password reset email
        var resetUrl = options.Value.PasswordResetUrl;
        if (!string.IsNullOrEmpty(resetUrl))
        {
            var fullResetUrl = $"{resetUrl}?userId={user.Id}&token={HttpUtility.UrlEncode(token)}";
            var emailMessage = new EmailMessage
            {
                From = options.Value.SystemEmailFrom ?? "no-reply@localhost",
                Recipients = email,
                Subject = "Password Reset Request",
                Body = $"A password reset request has been generated for your account. " +
                       $"If you made this request, you may reset your password by clicking " +
                       $"<a href=\"{fullResetUrl}\">here</a>. " +
                       $"If you did not make this request, please ignore this email."
            };
            await emailService.SendEmail(emailMessage);
        }

        return PasswordRecoveryResult.Succeeded();
    }

    public async Task<PasswordResetResult> ResetPasswordAsync(int userId, string token, string newPassword)
    {
        // First validate the password
        var validation = ValidatePassword(newPassword);
        if (!validation.IsValid)
        {
            return PasswordResetResult.Failed(validation.Errors);
        }

        // Find the user by ID
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return PasswordResetResult.Failed(["Invalid or expired token."]);
        }

        // Reset the password using the token
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            return PasswordResetResult.Failed(result.Errors.Select(e => e.Description));
        }

        return PasswordResetResult.Succeeded();
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(
        string currentPassword,
        string newPassword)
    {
        var currentUser = await authService.GetCurrentUserAsync();
        if (currentUser is null)
        {
            return PasswordChangeResult.Failed(["User not authenticated."]);
        }

        var validation = ValidatePassword(newPassword);
        if (!validation.IsValid)
        {
            return PasswordChangeResult.Failed(validation.Errors);
        }

        var user = await userManager.FindByIdAsync(currentUser.Id.ToString());
        if (user is null)
        {
            return PasswordChangeResult.Failed(["User not found."]);
        }

        var result = await userManager.ChangePasswordAsync(
            user,
            currentPassword,
            newPassword);

        if (!result.Succeeded)
        {
            return PasswordChangeResult.Failed(result.Errors.Select(e => e.Description));
        }

        return PasswordChangeResult.Succeeded();
    }

    public PasswordValidationResult ValidatePassword(string password)
    {
        var policy = options.Value.PasswordPolicy;
        var errors = new List<string>();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("Password is required.");
            return PasswordValidationResult.Invalid(errors);
        }

        if (password.Length < policy.MinimumLength)
        {
            errors.Add($"Password must be at least {policy.MinimumLength} characters long.");
        }

        if (policy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (policy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (policy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (policy.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            errors.Add("Password must contain at least one special character.");
        }

        if (password.Distinct().Count() < policy.RequiredUniqueChars)
        {
            errors.Add($"Password must contain at least {policy.RequiredUniqueChars} unique characters.");
        }

        return errors.Count == 0
            ? PasswordValidationResult.Valid()
            : PasswordValidationResult.Invalid(errors);
    }
}
