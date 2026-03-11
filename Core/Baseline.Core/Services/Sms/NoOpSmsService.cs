using Baseline.Core.Interfaces;

using Microsoft.Extensions.Logging;

namespace Baseline.Core.Services.Sms;

/// <summary>
/// No-op implementation of SMS service for when SMS is not configured.
/// Logs all SMS attempts without sending.
/// </summary>
public class NoOpSmsService : ISmsService
{
    private readonly ILogger<NoOpSmsService> logger;

    public NoOpSmsService(ILogger<NoOpSmsService> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public bool IsAvailable() => false;

    /// <inheritdoc />
    public Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        logger.LogDebug("NoOp SMS: Would send to {PhoneNumber}: {Message}", MaskPhone(phoneNumber), message);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> SendVerificationCodeAsync(string phoneNumber, string firstName, string verificationCode)
    {
        logger.LogDebug(
            "NoOp SMS: Would send verification code to {PhoneNumber} for {FirstName}",
            MaskPhone(phoneNumber),
            firstName);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> SendTemplatedSmsAsync(string phoneNumber, string templateName, IDictionary<string, string> placeholders)
    {
        logger.LogDebug(
            "NoOp SMS: Would send template {TemplateName} to {PhoneNumber}",
            templateName,
            MaskPhone(phoneNumber));
        return Task.FromResult(false);
    }

    private static string MaskPhone(string phone) =>
        string.IsNullOrWhiteSpace(phone) || phone.Length < 4 ? "***" : $"***{phone[^4..]}";
}
