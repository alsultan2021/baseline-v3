using CMS.EmailEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Account;

/// <summary>
/// Service for sending security alerts when login from new/unrecognized device.
/// </summary>
public interface INewDeviceAlertService
{
    /// <summary>
    /// Send an email alert for login from a new device.
    /// </summary>
    Task SendNewDeviceAlertAsync(NewDeviceAlertContext context);
}

/// <summary>
/// Context for new device alert email.
/// </summary>
public record NewDeviceAlertContext
{
    public required string Email { get; init; }
    public required string Username { get; init; }
    public string? IpAddress { get; init; }
    public string? Location { get; init; }
    public string? DeviceType { get; init; }
    public string? Browser { get; init; }
    public string? OperatingSystem { get; init; }
    public DateTime LoginTime { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Implementation of new device alert service using Kentico email system.
/// </summary>
public class NewDeviceAlertService(
    IEmailService emailService,
    IOptions<LoginAuditOptions> options,
    ILogger<NewDeviceAlertService> logger) : INewDeviceAlertService
{
    public async Task SendNewDeviceAlertAsync(NewDeviceAlertContext context)
    {
        if (!options.Value.EnableNewDeviceAlerts)
        {
            logger.LogDebug("New device alerts are disabled");
            return;
        }

        try
        {
            await SendAlertEmailAsync(context);

            logger.LogInformation(
                "New device alert sent to {Email} for login from {IpAddress}",
                context.Email, context.IpAddress ?? "unknown IP");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send new device alert to {Email}", context.Email);
        }
    }

    private async Task SendAlertEmailAsync(NewDeviceAlertContext context)
    {
        var body = $"""
Hello {context.Username},

We noticed a new sign-in to your account.

Details:
- Time: {context.LoginTime:f} (UTC)
- Device: {context.DeviceType ?? "Unknown"}
- Browser: {context.Browser ?? "Unknown"}
- Operating System: {context.OperatingSystem ?? "Unknown"}
- IP Address: {context.IpAddress ?? "Unknown"}
- Location: {context.Location ?? "Unknown"}

If this was you, you can ignore this message. If you did not sign in, please secure your account immediately by:
1. Changing your password
2. Enabling two-factor authentication if not already enabled
3. Reviewing your recent account activity

Best regards,
Your Security Team
""";

        var email = new EmailMessage
        {
            Recipients = context.Email,
            Subject = "New Sign-In to Your Account",
            Body = body
        };

        await emailService.SendEmail(email);
    }
}
