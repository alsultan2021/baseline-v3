namespace Baseline.Core.Services.Sms;

/// <summary>
/// Configuration options for Twilio SMS service.
/// </summary>
public class TwilioSmsOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Twilio";

    /// <summary>
    /// Gets or sets the Twilio Account SID.
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Twilio Auth Token.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Twilio phone number to send SMS from (E.164 format).
    /// </summary>
    public string FromPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Twilio Messaging Service SID (optional, alternative to FromPhoneNumber).
    /// </summary>
    public string? MessagingServiceSid { get; set; }

    /// <summary>
    /// Gets or sets whether SMS sending is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default country code for phone numbers without country code.
    /// </summary>
    public string DefaultCountryCode { get; set; } = "+1";

    /// <summary>
    /// Gets or sets whether to use Twilio Verify for verification codes (more secure).
    /// </summary>
    public bool UseVerifyService { get; set; } = false;

    /// <summary>
    /// Gets or sets the Twilio Verify Service SID (required if UseVerifyService is true).
    /// </summary>
    public string? VerifyServiceSid { get; set; }

    /// <summary>
    /// Gets or sets the maximum SMS message length before truncation (Twilio limit is 1600).
    /// </summary>
    public int MaxMessageLength { get; set; } = 1600;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(AccountSid) || string.IsNullOrWhiteSpace(AuthToken))
        {
            return false;
        }

        // Either FromPhoneNumber or MessagingServiceSid must be provided
        if (string.IsNullOrWhiteSpace(FromPhoneNumber) && string.IsNullOrWhiteSpace(MessagingServiceSid))
        {
            return false;
        }

        // If using Verify service, VerifyServiceSid must be provided
        if (UseVerifyService && string.IsNullOrWhiteSpace(VerifyServiceSid))
        {
            return false;
        }

        return true;
    }
}
