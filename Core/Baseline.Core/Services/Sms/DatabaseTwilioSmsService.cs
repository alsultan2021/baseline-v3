using System.Text.RegularExpressions;

using CMS.DataEngine;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Interfaces;

using Microsoft.Extensions.Logging;

using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Baseline.Core.Services.Sms;

/// <summary>
/// Database-backed Twilio SMS service that reads settings from the admin UI.
/// </summary>
public partial class DatabaseTwilioSmsService : ISmsService
{
    private readonly IInfoProvider<TwilioSmsSettingsInfo> settingsProvider;
    private readonly ILogger<DatabaseTwilioSmsService> logger;

    private TwilioSmsSettingsInfo? cachedSettings;
    private DateTime cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public DatabaseTwilioSmsService(
        IInfoProvider<TwilioSmsSettingsInfo> settingsProvider,
        ILogger<DatabaseTwilioSmsService> logger)
    {
        this.settingsProvider = settingsProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Gets the active (enabled) SMS settings from the database.
    /// </summary>
    private TwilioSmsSettingsInfo? GetActiveSettings()
    {
        if (cachedSettings != null && DateTime.UtcNow < cacheExpiry)
        {
            return cachedSettings;
        }

        try
        {
            // Get the first enabled settings entry
            cachedSettings = settingsProvider
                .Get()
                .WhereEquals(nameof(TwilioSmsSettingsInfo.IsEnabled), true)
                .TopN(1)
                .FirstOrDefault();

            cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

            if (cachedSettings != null)
            {
                // Initialize Twilio client with the settings
                TwilioClient.Init(cachedSettings.AccountSid, cachedSettings.AuthToken);
                logger.LogDebug("Twilio SMS initialized from database settings: {DisplayName}",
                    cachedSettings.TwilioSmsSettingsDisplayName);
            }

            return cachedSettings;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load Twilio SMS settings from database");
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsAvailable()
    {
        var settings = GetActiveSettings();
        return settings != null && settings.IsEnabled && IsValidSettings(settings);
    }

    private static bool IsValidSettings(TwilioSmsSettingsInfo settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AccountSid) || string.IsNullOrWhiteSpace(settings.AuthToken))
        {
            return false;
        }

        // Either FromPhoneNumber or MessagingServiceSid must be provided
        if (string.IsNullOrWhiteSpace(settings.FromPhoneNumber) && string.IsNullOrWhiteSpace(settings.MessagingServiceSid))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        var settings = GetActiveSettings();
        if (settings == null || !IsValidSettings(settings))
        {
            logger.LogWarning("SMS service not available, skipping send to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }

        try
        {
            string formattedPhone = FormatPhoneNumber(phoneNumber, settings.DefaultCountryCode);
            string truncatedMessage = TruncateMessage(message);

            var messageOptions = new CreateMessageOptions(new PhoneNumber(formattedPhone))
            {
                Body = truncatedMessage
            };

            // Use MessagingServiceSid if available, otherwise use FromPhoneNumber
            if (!string.IsNullOrWhiteSpace(settings.MessagingServiceSid))
            {
                messageOptions.MessagingServiceSid = settings.MessagingServiceSid;
            }
            else
            {
                messageOptions.From = new PhoneNumber(settings.FromPhoneNumber);
            }

            var result = await MessageResource.CreateAsync(messageOptions);

            logger.LogInformation(
                "SMS sent to {PhoneNumber}, SID: {MessageSid}, Status: {Status}",
                MaskPhoneNumber(phoneNumber),
                result.Sid,
                result.Status);

            return result.Status != MessageResource.StatusEnum.Failed &&
                   result.Status != MessageResource.StatusEnum.Undelivered;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendVerificationCodeAsync(string phoneNumber, string firstName, string verificationCode)
    {
        if (!IsAvailable())
        {
            logger.LogWarning("SMS service not available for verification code to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }

        string message = $"Hi {firstName}, your verification code is: {verificationCode}. This code expires in 10 minutes. Do not share this code with anyone.";

        return await SendSmsAsync(phoneNumber, message);
    }

    /// <inheritdoc />
    public async Task<bool> SendTemplatedSmsAsync(string phoneNumber, string templateName, IDictionary<string, string> placeholders)
    {
        if (!IsAvailable())
        {
            logger.LogWarning("SMS service not available for template {TemplateName} to {PhoneNumber}",
                templateName, MaskPhoneNumber(phoneNumber));
            return false;
        }

        string message = BuildMessageFromPlaceholders(templateName, placeholders);
        return await SendSmsAsync(phoneNumber, message);
    }

    /// <summary>
    /// Builds an SMS message from template placeholders.
    /// </summary>
    private static string BuildMessageFromPlaceholders(string templateName, IDictionary<string, string> placeholders)
    {
        string template = templateName switch
        {
            "BookingVerificationCode" => "Hi {FirstName}, your verification code is: {VerificationCode}. Expires in {ExpiryMinutes} minutes.",
            "BookingConfirmation" => "Hi {FirstName}! Your reservation #{ReservationNumber} at {RestaurantName} is confirmed for {ReservationDate} at {ReservationTime}. Party of {NumberOfSeats}.",
            "BookingReminder" => "Reminder: Your reservation at {RestaurantName} is tomorrow at {ReservationTime}. Confirmation: {ReservationNumber}",
            "BookingCancellation" => "Your reservation #{ReservationNumber} at {RestaurantName} has been cancelled.",
            _ => string.Join(", ", placeholders.Select(p => $"{p.Key}: {p.Value}"))
        };

        foreach (var placeholder in placeholders)
        {
            template = template.Replace($"{{{placeholder.Key}}}", placeholder.Value, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    /// <summary>
    /// Formats a phone number to E.164 format.
    /// </summary>
    private static string FormatPhoneNumber(string phoneNumber, string defaultCountryCode)
    {
        string cleaned = PhoneCleanupRegex().Replace(phoneNumber, "");

        if (phoneNumber.TrimStart().StartsWith('+'))
        {
            return $"+{cleaned}";
        }

        if (cleaned.StartsWith('0'))
        {
            cleaned = cleaned[1..];
        }

        return $"{defaultCountryCode}{cleaned}";
    }

    /// <summary>
    /// Truncates message to SMS limit (160 chars for single, 1600 for concatenated).
    /// </summary>
    private static string TruncateMessage(string message, int maxLength = 1600)
    {
        if (string.IsNullOrEmpty(message))
        {
            return string.Empty;
        }

        return message.Length <= maxLength ? message : message[..maxLength];
    }

    /// <summary>
    /// Masks a phone number for logging (privacy protection).
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length < 4)
        {
            return "***";
        }

        return $"***{phoneNumber[^4..]}";
    }

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PhoneCleanupRegex();

    /// <summary>
    /// Clears the settings cache (useful after admin UI changes).
    /// </summary>
    public void ClearCache()
    {
        cachedSettings = null;
        cacheExpiry = DateTime.MinValue;
    }
}
