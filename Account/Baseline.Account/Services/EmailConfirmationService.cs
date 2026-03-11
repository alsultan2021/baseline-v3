using CMS.EmailEngine;
using Kentico.Membership;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Web;

namespace Baseline.Account;

/// <summary>
/// Implementation of email confirmation service using Xperience by Kentico Identity.
/// Aligns with Kentico's SignIn.RequireConfirmedAccount option.
/// </summary>
/// <typeparam name="TUser">The application user type.</typeparam>
public class EmailConfirmationService<TUser>(
    UserManager<TUser> userManager,
    IEmailService emailService,
    IOptions<BaselineAccountOptions> options) : IEmailConfirmationService
    where TUser : ApplicationUser, new()
{
    public async Task<SendConfirmationResult> SendConfirmationEmailAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new SendConfirmationResult
            {
                Success = false,
                ErrorMessage = "User not found."
            };
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            return new SendConfirmationResult
            {
                Success = false,
                ErrorMessage = "User does not have an email address."
            };
        }

        return await SendConfirmationEmailInternalAsync(user);
    }

    public async Task<EmailConfirmationResult> ConfirmEmailAsync(int userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new EmailConfirmationResult
            {
                Success = false,
                ErrorMessage = "Invalid confirmation link."
            };
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return new EmailConfirmationResult
            {
                Success = false,
                ErrorMessage = $"Email confirmation failed: {errors}"
            };
        }

        return new EmailConfirmationResult { Success = true };
    }

    public async Task<bool> IsEmailConfirmedAsync(int userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        return await userManager.IsEmailConfirmedAsync(user);
    }

    public async Task<SendConfirmationResult> ResendConfirmationEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Return success to prevent email enumeration
            return new SendConfirmationResult { Success = true };
        }

        // Check if email is already confirmed
        if (await userManager.IsEmailConfirmedAsync(user))
        {
            return new SendConfirmationResult
            {
                Success = false,
                ErrorMessage = "Email is already confirmed."
            };
        }

        return await SendConfirmationEmailInternalAsync(user);
    }

    private async Task<SendConfirmationResult> SendConfirmationEmailInternalAsync(TUser user)
    {
        try
        {
            // Generate email confirmation token
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

            // Build confirmation URL
            var confirmationUrl = options.Value.Urls?.ConfirmationUrl ?? "/Account/Confirmation";
            var encodedToken = HttpUtility.UrlEncode(token);
            var fullUrl = $"{confirmationUrl}?userId={user.Id}&token={encodedToken}";

            // Send confirmation email
            var message = new EmailMessage
            {
                From = options.Value.SystemEmailFrom ?? "no-reply@localhost",
                Recipients = user.Email,
                Subject = "Confirm your email address",
                Body = $@"
                    <h2>Confirm Your Email Address</h2>
                    <p>Thank you for registering. Please confirm your email address by clicking the link below:</p>
                    <p><a href=""{fullUrl}"">Confirm Email</a></p>
                    <p>If you did not create an account, you can safely ignore this email.</p>
                "
            };

            await emailService.SendEmail(message);

            return new SendConfirmationResult { Success = true };
        }
        catch (Exception ex)
        {
            return new SendConfirmationResult
            {
                Success = false,
                ErrorMessage = $"Failed to send confirmation email: {ex.Message}"
            };
        }
    }
}
