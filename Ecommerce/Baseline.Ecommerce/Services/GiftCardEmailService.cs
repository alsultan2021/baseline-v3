using Baseline.Ecommerce;
using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Service for sending gift card email notifications.
/// </summary>
public interface IGiftCardEmailService
{
    /// <summary>
    /// Sends a gift card email to the recipient.
    /// </summary>
    /// <param name="giftCard">The gift card to send.</param>
    /// <param name="recipientEmail">The recipient's email address.</param>
    /// <param name="recipientName">Optional recipient name for personalization.</param>
    /// <param name="personalMessage">Optional personal message from the purchaser.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email was sent successfully.</returns>
    Task<bool> SendGiftCardEmailAsync(
        GiftCardInfo giftCard,
        string recipientEmail,
        string? recipientName = null,
        string? personalMessage = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a gift card balance notification email.
    /// Used when a gift card is partially used or its balance changes.
    /// </summary>
    Task<bool> SendBalanceUpdateEmailAsync(
        GiftCardInfo giftCard,
        string recipientEmail,
        decimal previousBalance,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for gift card emails.
/// </summary>
public class GiftCardEmailOptions
{
    /// <summary>
    /// The email template code name for gift card delivery emails.
    /// </summary>
    public string GiftCardDeliveryTemplateName { get; set; } = "Baseline.GiftCardDelivery";

    /// <summary>
    /// The email template code name for balance update notifications.
    /// </summary>
    public string BalanceUpdateTemplateName { get; set; } = "Baseline.GiftCardBalanceUpdate";

    /// <summary>
    /// The sender email address for gift card emails.
    /// </summary>
    public string? FromEmail { get; set; } = "noreply@chevalroyal.ca";

    /// <summary>
    /// The sender display name for gift card emails.
    /// </summary>
    public string FromName { get; set; } = "Gift Cards";

    /// <summary>
    /// The URL pattern for the gift card redemption page.
    /// Use {code} as placeholder for the gift card code.
    /// </summary>
    public string RedemptionUrlPattern { get; set; } = "/account/wallet?giftcard={code}";

    /// <summary>
    /// The base URL for the site (used to build absolute URLs).
    /// </summary>
    public string? SiteBaseUrl { get; set; }
}

/// <summary>
/// Default implementation of gift card email service.
/// Uses Kentico Email Library for sending transactional emails.
/// </summary>
public class GiftCardEmailService(
    IEmailService emailService,
    INotificationEmailMessageProvider notificationEmailMessageProvider,
    IInfoProvider<CurrencyInfo> currencyProvider,
    IOptions<GiftCardEmailOptions> options,
    IOptions<BaselineEcommerceOptions> ecommerceOptions,
    ILogger<GiftCardEmailService> logger) : IGiftCardEmailService
{
    private readonly GiftCardEmailOptions _options = options.Value;
    private readonly string _defaultCurrency = ecommerceOptions.Value.Pricing.DefaultCurrency;

    /// <inheritdoc/>
    public async Task<bool> SendGiftCardEmailAsync(
        GiftCardInfo giftCard,
        string recipientEmail,
        string? recipientName = null,
        string? personalMessage = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                logger.LogWarning("Cannot send gift card email - no recipient email provided for {Code}", giftCard.GiftCardCode);
                return false;
            }

            // Get currency info for formatting
            var currency = await currencyProvider.GetAsync(giftCard.GiftCardCurrencyID, cancellationToken);
            var currencySymbol = currency?.CurrencySymbol ?? "$";
            var currencyCode = currency?.CurrencyCode ?? _defaultCurrency;

            // Build the redemption URL
            var redemptionUrl = BuildRedemptionUrl(giftCard.GiftCardCode);

            // Build email content
            var emailContent = BuildGiftCardEmailContent(
                giftCard,
                recipientName,
                personalMessage,
                currencySymbol,
                currencyCode,
                redemptionUrl);

            // Try to use email template first
            var templateSent = await TrySendTemplatedEmailAsync(
                _options.GiftCardDeliveryTemplateName,
                recipientEmail,
                new Dictionary<string, object>
                {
                    ["GiftCardCode"] = giftCard.GiftCardCode,
                    ["Amount"] = giftCard.GiftCardInitialAmount,
                    ["CurrencySymbol"] = currencySymbol,
                    ["CurrencyCode"] = currencyCode,
                    ["RecipientName"] = recipientName ?? "Friend",
                    ["PersonalMessage"] = personalMessage ?? "",
                    ["RedemptionUrl"] = redemptionUrl,
                    ["ExpiresAt"] = giftCard.GiftCardExpiresAt?.ToString("MMMM dd, yyyy") ?? "Never"
                },
                cancellationToken);

            if (templateSent)
            {
                logger.LogDebug("Gift card email sent to {Email} for {Code}", recipientEmail, giftCard.GiftCardCode);
                return true;
            }

            // Fall back to simple email if template not available
            var emailMessage = new EmailMessage
            {
                From = _options.FromEmail ?? "noreply@example.com",
                Recipients = recipientEmail,
                Subject = "You've received a Gift Card!",
                Body = emailContent,
                PlainTextBody = BuildPlainTextContent(giftCard, recipientName, personalMessage, currencySymbol, redemptionUrl)
            };

            await emailService.SendEmail(emailMessage);

            logger.LogDebug("Gift card email sent to {Email} for {Code}", recipientEmail, giftCard.GiftCardCode);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send gift card email to {Email} for {Code}", recipientEmail, giftCard.GiftCardCode);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> SendBalanceUpdateEmailAsync(
        GiftCardInfo giftCard,
        string recipientEmail,
        decimal previousBalance,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                return false;
            }

            var currency = await currencyProvider.GetAsync(giftCard.GiftCardCurrencyID, cancellationToken);
            var currencySymbol = currency?.CurrencySymbol ?? "$";

            var templateSent = await TrySendTemplatedEmailAsync(
                _options.BalanceUpdateTemplateName,
                recipientEmail,
                new Dictionary<string, object>
                {
                    ["GiftCardCode"] = giftCard.GiftCardCode,
                    ["PreviousBalance"] = previousBalance,
                    ["CurrentBalance"] = giftCard.GiftCardRemainingBalance,
                    ["AmountUsed"] = previousBalance - giftCard.GiftCardRemainingBalance,
                    ["CurrencySymbol"] = currencySymbol
                },
                cancellationToken);

            return templateSent;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send balance update email for {Code}", giftCard.GiftCardCode);
            return false;
        }
    }

    /// <summary>
    /// Attempts to send an email using a Kentico notification template.
    /// Falls back to simple email if template is not configured.
    /// </summary>
    private async Task<bool> TrySendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> macros,
        CancellationToken cancellationToken)
    {
        try
        {
            var placeholders = new GiftCardEmailPlaceholders(templateName, macros);
            var emailMessage = await notificationEmailMessageProvider.CreateEmailMessage(
                templateName,
                0, // system user — recipient is overridden below
                placeholders);

            if (emailMessage is null)
            {
                logger.LogDebug("Notification template '{Template}' not found, falling back to inline email", templateName);
                return false;
            }

            emailMessage.Recipients = recipientEmail;
            await emailService.SendEmail(emailMessage);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Templated email failed for '{Template}', falling back to inline", templateName);
            return false;
        }
    }

    /// <summary>
    /// Builds the redemption URL for a gift card.
    /// </summary>
    private string BuildRedemptionUrl(string code)
    {
        var relativePath = _options.RedemptionUrlPattern.Replace("{code}", Uri.EscapeDataString(code));

        if (!string.IsNullOrEmpty(_options.SiteBaseUrl))
        {
            return _options.SiteBaseUrl.TrimEnd('/') + relativePath;
        }

        return relativePath;
    }

    /// <summary>
    /// Builds the HTML email content for a gift card.
    /// </summary>
    private static string BuildGiftCardEmailContent(
        GiftCardInfo giftCard,
        string? recipientName,
        string? personalMessage,
        string currencySymbol,
        string currencyCode,
        string redemptionUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName)
            ? "Hello!"
            : $"Hello {recipientName}!";

        var messageSection = string.IsNullOrWhiteSpace(personalMessage)
            ? ""
            : $@"
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; font-style: italic;'>
                <p style='margin: 0; color: #666;'>""{personalMessage}""</p>
            </div>";

        var expirationNote = giftCard.GiftCardExpiresAt.HasValue
            ? $"<p style='color: #888; font-size: 12px;'>This gift card expires on {giftCard.GiftCardExpiresAt.Value:MMMM dd, yyyy}.</p>"
            : "<p style='color: #888; font-size: 12px;'>This gift card does not expire.</p>";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, ""Helvetica Neue"", Arial, sans-serif; margin: 0; padding: 20px; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 28px;'>🎁 You've Received a Gift Card!</h1>
        </div>
        
        <div style='padding: 40px;'>
            <p style='font-size: 18px; color: #333;'>{greeting}</p>
            
            <p style='color: #666;'>Someone special has sent you a gift card!</p>
            
            {messageSection}
            
            <div style='background-color: #f0f4ff; border: 2px dashed #667eea; border-radius: 12px; padding: 30px; text-align: center; margin: 30px 0;'>
                <p style='margin: 0 0 10px; color: #666; font-size: 14px;'>YOUR GIFT CARD CODE</p>
                <p style='margin: 0 0 20px; font-size: 32px; font-weight: bold; color: #667eea; letter-spacing: 2px;'>{giftCard.GiftCardCode}</p>
                <p style='margin: 0; font-size: 14px; color: #666;'>Value</p>
                <p style='margin: 5px 0 0; font-size: 36px; font-weight: bold; color: #333;'>{currencySymbol}{giftCard.GiftCardInitialAmount:N2} {currencyCode}</p>
            </div>
            
            <div style='text-align: center;'>
                <a href='{redemptionUrl}' style='display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: #ffffff; padding: 15px 40px; border-radius: 30px; text-decoration: none; font-weight: bold; font-size: 16px;'>Redeem Now</a>
            </div>
            
            <div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee;'>
                <p style='color: #666; font-size: 14px;'><strong>How to use your gift card:</strong></p>
                <ol style='color: #666; font-size: 14px;'>
                    <li>Click the ""Redeem Now"" button above or visit your account wallet</li>
                    <li>Enter the gift card code when prompted</li>
                    <li>The balance will be added to your account wallet</li>
                    <li>Use your wallet balance at checkout!</li>
                </ol>
                {expirationNote}
            </div>
        </div>
        
        <div style='background-color: #f8f9fa; padding: 20px; text-align: center;'>
            <p style='margin: 0; color: #888; font-size: 12px;'>If you have any questions, please contact our support team.</p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Builds plain text email content for fallback.
    /// </summary>
    private static string BuildPlainTextContent(
        GiftCardInfo giftCard,
        string? recipientName,
        string? personalMessage,
        string currencySymbol,
        string redemptionUrl)
    {
        var greeting = string.IsNullOrWhiteSpace(recipientName)
            ? "Hello!"
            : $"Hello {recipientName}!";

        var message = string.IsNullOrWhiteSpace(personalMessage)
            ? ""
            : $"\n\nMessage from sender:\n\"{personalMessage}\"\n";

        return $@"{greeting}

You've received a gift card!
{message}
YOUR GIFT CARD CODE: {giftCard.GiftCardCode}
VALUE: {currencySymbol}{giftCard.GiftCardInitialAmount:N2}

To redeem your gift card, visit:
{redemptionUrl}

How to use your gift card:
1. Click the link above or visit your account wallet
2. Enter the gift card code when prompted
3. The balance will be added to your account wallet
4. Use your wallet balance at checkout!

If you have any questions, please contact our support team.
";
    }
}

/// <summary>
/// Notification email placeholder bridge for gift card emails.
/// Implements <see cref="INotificationEmailPlaceholdersByCodeName"/> so the
/// XbK notification system can resolve placeholder values.
/// </summary>
public class GiftCardEmailPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    private readonly Dictionary<string, object> _macros;

    public GiftCardEmailPlaceholders(string templateCodeName, Dictionary<string, object> macros)
    {
        NotificationEmailName = templateCodeName;
        _macros = macros;
    }

    public string NotificationEmailName { get; }

    public IDictionary<string, Func<string>> GetPlaceholders() =>
        _macros.ToDictionary(
            kvp => kvp.Key,
            kvp => new Func<string>(() => kvp.Value?.ToString() ?? ""));
}
