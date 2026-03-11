using System.ComponentModel.DataAnnotations;

using Kentico.Xperience.Admin.Base.FormAnnotations;

using Baseline.Core.Admin.Sms.InfoClasses;

namespace Baseline.Core.Admin.Sms.Models;

/// <summary>
/// Model for Twilio SMS settings form.
/// </summary>
public class TwilioSmsSettingsModel
{
    /// <summary>
    /// Internal ID for updates.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Display name for the configuration.
    /// </summary>
    [TextInputComponent(Label = "Display Name", Order = 1)]
    [Required]
    public string DisplayName { get; set; } = "";

    /// <summary>
    /// Environment (Test/Live).
    /// </summary>
    [DropDownComponent(Label = "Environment", DataProviderType = typeof(EnvironmentOptionsProvider), Order = 2)]
    [Required]
    public string Environment { get; set; } = "Test";

    /// <summary>
    /// Twilio Account SID.
    /// </summary>
    [TextInputComponent(Label = "Account SID", Order = 3, ExplanationText = "Your Twilio Account SID from the console")]
    [Required]
    public string AccountSid { get; set; } = "";

    /// <summary>
    /// Twilio Auth Token.
    /// </summary>
    [TextInputComponent(Label = "Auth Token", Order = 4, ExplanationText = "Your Twilio Auth Token")]
    [Required]
    public string AuthToken { get; set; } = "";

    /// <summary>
    /// From phone number.
    /// </summary>
    [TextInputComponent(Label = "From Phone Number", Order = 5, ExplanationText = "E.164 format: +1234567890")]
    [Required]
    public string FromPhoneNumber { get; set; } = "";

    /// <summary>
    /// Messaging Service SID (optional).
    /// </summary>
    [TextInputComponent(Label = "Messaging Service SID", Order = 6, ExplanationText = "Optional: Use a Messaging Service instead of a phone number")]
    public string? MessagingServiceSid { get; set; }

    /// <summary>
    /// Default country code.
    /// </summary>
    [TextInputComponent(Label = "Default Country Code", Order = 7, ExplanationText = "e.g., +1 for US")]
    public string DefaultCountryCode { get; set; } = "+1";

    /// <summary>
    /// Whether this configuration is enabled.
    /// </summary>
    [CheckBoxComponent(Label = "Enabled", Order = 8)]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Whether to use Twilio Verify service.
    /// </summary>
    [CheckBoxComponent(Label = "Use Verify Service", Order = 9, ExplanationText = "Use Twilio Verify for OTP codes instead of standard SMS")]
    public bool UseVerifyService { get; set; }

    /// <summary>
    /// Verify Service SID.
    /// </summary>
    [TextInputComponent(Label = "Verify Service SID", Order = 10, ExplanationText = "Required if using Verify Service")]
    public string? VerifyServiceSid { get; set; }

    /// <summary>
    /// Creates an empty model.
    /// </summary>
    public TwilioSmsSettingsModel()
    {
    }

    /// <summary>
    /// Creates a model from an existing settings object.
    /// </summary>
    public TwilioSmsSettingsModel(TwilioSmsSettingsInfo settings)
    {
        Id = settings.TwilioSmsSettingsID;
        DisplayName = settings.TwilioSmsSettingsDisplayName;
        Environment = settings.Environment;
        AccountSid = settings.AccountSid;
        AuthToken = settings.AuthToken;
        FromPhoneNumber = settings.FromPhoneNumber;
        MessagingServiceSid = settings.MessagingServiceSid;
        DefaultCountryCode = settings.DefaultCountryCode;
        IsEnabled = settings.IsEnabled;
        UseVerifyService = settings.UseVerifyService;
        VerifyServiceSid = settings.VerifyServiceSid;
    }
}

/// <summary>
/// Provides environment options for dropdown.
/// </summary>
public class EnvironmentOptionsProvider : IDropDownOptionsProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new DropDownOptionItem { Value = "Test", Text = "Test" },
            new DropDownOptionItem { Value = "Live", Text = "Live" }
        ]);
}
