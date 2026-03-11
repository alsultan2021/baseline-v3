using System.Text.RegularExpressions;

using Baseline.Core.Interfaces;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Baseline.Core.Services.Sms;

/// <summary>
/// Twilio implementation of SMS service for sending notifications.
/// </summary>
public partial class TwilioSmsService : ISmsService
{
    private readonly TwilioSmsOptions options;
    private readonly ILogger<TwilioSmsService> logger;
    private readonly bool isInitialized;

    public TwilioSmsService(
        IOptions<TwilioSmsOptions> options,
        ILogger<TwilioSmsService> logger)
    {
        this.options = options.Value;
        this.logger = logger;
        isInitialized = InitializeTwilio();
    }

    /// <summary>
    /// Initializes the Twilio client with account credentials.
    /// </summary>
    private bool InitializeTwilio()
    {
        if (!options.Enabled || !options.IsValid())
        {
            logger.LogWarning("Twilio SMS service is disabled or not properly configured");
            return false;
        }

        try
        {
            TwilioClient.Init(options.AccountSid, options.AuthToken);
            logger.LogInformation("Twilio SMS service initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Twilio SMS service");
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsAvailable() => isInitialized && options.Enabled;

    /// <inheritdoc />
    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        if (!IsAvailable())
        {
            logger.LogWarning("SMS service not available, skipping send to {PhoneNumber}", MaskPhoneNumber(phoneNumber));
            return false;
        }

        try
        {
            string formattedPhone = FormatPhoneNumber(phoneNumber);
            string truncatedMessage = TruncateMessage(message);

            var messageOptions = new CreateMessageOptions(new PhoneNumber(formattedPhone))
            {
                Body = truncatedMessage
            };

            // Use MessagingServiceSid if available, otherwise use FromPhoneNumber
            if (!string.IsNullOrWhiteSpace(options.MessagingServiceSid))
            {
                messageOptions.MessagingServiceSid = options.MessagingServiceSid;
            }
            else
            {
                messageOptions.From = new PhoneNumber(options.FromPhoneNumber);
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

        // Build message from template - in production, could fetch templates from Kentico
        string message = BuildMessageFromPlaceholders(templateName, placeholders);

        return await SendSmsAsync(phoneNumber, message);
    }

    /// <summary>
    /// Builds an SMS message from template placeholders.
    /// </summary>
    private static string BuildMessageFromPlaceholders(string templateName, IDictionary<string, string> placeholders)
    {
        // Default templates - could be extended to fetch from CMS
        string template = templateName switch
        {
            "BookingVerificationCode" => "Hi {FirstName}, your verification code is: {VerificationCode}. Expires in {ExpiryMinutes} minutes.",
            "BookingConfirmation" => "Hi {FirstName}! Your reservation #{ReservationNumber} at {RestaurantName} is confirmed for {ReservationDate} at {ReservationTime}. Party of {NumberOfSeats}.",
            "BookingReminder" => "Reminder: Your reservation at {RestaurantName} is tomorrow at {ReservationTime}. Confirmation: {ReservationNumber}",
            "BookingCancellation" => "Your reservation #{ReservationNumber} at {RestaurantName} has been cancelled.",
            _ => string.Join(", ", placeholders.Select(p => $"{p.Key}: {p.Value}"))
        };

        // Replace placeholders
        foreach (var placeholder in placeholders)
        {
            template = template.Replace($"{{{placeholder.Key}}}", placeholder.Value, StringComparison.OrdinalIgnoreCase);
        }

        return template;
    }

    /// <summary>
    /// Formats a phone number to E.164 format.
    /// </summary>
    private string FormatPhoneNumber(string phoneNumber)
    {
        // Remove all non-digit characters except leading +
        string cleaned = PhoneCleanupRegex().Replace(phoneNumber, "");

        // If starts with +, keep it; otherwise add default country code
        if (phoneNumber.TrimStart().StartsWith('+'))
        {
            return $"+{cleaned}";
        }

        // Remove leading 0 if present (common in local formats)
        if (cleaned.StartsWith('0'))
        {
            cleaned = cleaned[1..];
        }

        return $"{options.DefaultCountryCode}{cleaned}";
    }

    /// <summary>
    /// Truncates message to maximum allowed length.
    /// </summary>
    private string TruncateMessage(string message)
    {
        if (message.Length <= options.MaxMessageLength)
        {
            return message;
        }

        logger.LogWarning("SMS message truncated from {OriginalLength} to {MaxLength} characters",
            message.Length, options.MaxMessageLength);

        return message[..(options.MaxMessageLength - 3)] + "...";
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
}
