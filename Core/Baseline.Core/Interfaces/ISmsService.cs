namespace Baseline.Core.Interfaces;

/// <summary>
/// Service for sending SMS messages.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends an SMS message to the specified phone number.
    /// </summary>
    /// <param name="phoneNumber">The recipient's phone number (E.164 format preferred).</param>
    /// <param name="message">The message content.</param>
    /// <returns>True if the message was sent successfully; otherwise, false.</returns>
    Task<bool> SendSmsAsync(string phoneNumber, string message);

    /// <summary>
    /// Sends a verification code SMS to the specified phone number.
    /// </summary>
    /// <param name="phoneNumber">The recipient's phone number.</param>
    /// <param name="firstName">The recipient's first name for personalization.</param>
    /// <param name="verificationCode">The verification code to send.</param>
    /// <returns>True if the message was sent successfully; otherwise, false.</returns>
    Task<bool> SendVerificationCodeAsync(string phoneNumber, string firstName, string verificationCode);

    /// <summary>
    /// Sends a notification SMS using a template.
    /// </summary>
    /// <param name="phoneNumber">The recipient's phone number.</param>
    /// <param name="templateName">The name of the SMS template (maps to notification email templates).</param>
    /// <param name="placeholders">Dictionary of placeholder values.</param>
    /// <returns>True if the message was sent successfully; otherwise, false.</returns>
    Task<bool> SendTemplatedSmsAsync(string phoneNumber, string templateName, IDictionary<string, string> placeholders);

    /// <summary>
    /// Checks if the SMS service is configured and available.
    /// </summary>
    /// <returns>True if SMS service is available; otherwise, false.</returns>
    bool IsAvailable();
}
