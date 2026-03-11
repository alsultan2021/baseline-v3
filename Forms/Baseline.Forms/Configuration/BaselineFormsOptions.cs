namespace Baseline.Forms.Configuration;

/// <summary>
/// Configuration options for Baseline Forms module.
/// </summary>
public class BaselineFormsOptions
{
    /// <summary>
    /// Gets or sets whether to enable form caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether to log form submissions.
    /// </summary>
    public bool LogSubmissions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to send autoresponder emails.
    /// </summary>
    public bool EnableAutoresponders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to send notification emails.
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets the notification recipient email (admin/site owner).
    /// </summary>
    public string? NotificationRecipientEmail { get; set; }

    /// <summary>
    /// Gets or sets the default from email for form notifications.
    /// </summary>
    public string? DefaultFromEmail { get; set; }

    /// <summary>
    /// Gets or sets the default from name for form notifications.
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Gets or sets whether to validate form data on the server.
    /// </summary>
    public bool EnableServerSideValidation { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum file upload size in bytes.
    /// </summary>
    public long MaxFileUploadSize { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Gets or sets allowed file extensions for uploads.
    /// </summary>
    public string[] AllowedFileExtensions { get; set; } = 
    [
        ".pdf", ".doc", ".docx", ".xls", ".xlsx",
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    ];

    /// <summary>
    /// Gets or sets whether to associate submissions with contacts.
    /// </summary>
    public bool AssociateWithContacts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to check consent before submission.
    /// </summary>
    public bool RequireConsentForSubmission { get; set; } = false;

    /// <summary>
    /// Gets or sets the consent code name required for submission.
    /// </summary>
    public string? RequiredConsentCodeName { get; set; }

    /// <summary>
    /// Gets or sets whether to enable honeypot spam protection.
    /// When enabled, forms include a hidden field that bots typically fill.
    /// </summary>
    public bool EnableHoneypot { get; set; } = true;

    /// <summary>
    /// Gets or sets the honeypot field name (should look like a real field).
    /// </summary>
    public string HoneypotFieldName { get; set; } = "website";

    /// <summary>
    /// Gets or sets the honeypot CSS class for hiding the field.
    /// </summary>
    public string HoneypotCssClass { get; set; } = "visually-hidden";
}
