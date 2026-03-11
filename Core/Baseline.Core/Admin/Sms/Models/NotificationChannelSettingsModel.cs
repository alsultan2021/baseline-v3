using System.ComponentModel.DataAnnotations;

using CMS.Base;
using CMS.DataEngine;

using Kentico.Xperience.Admin.Base.FormAnnotations;

using Baseline.Core.Admin.Sms.InfoClasses;

namespace Baseline.Core.Admin.Sms.Models;

/// <summary>
/// Model for notification channel settings form.
/// </summary>
public class NotificationChannelSettingsModel
{
    /// <summary>
    /// Internal ID for updates.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The notification email code name.
    /// </summary>
    [DropDownComponent(Label = "Notification Email", DataProviderType = typeof(NotificationEmailOptionsProvider), Order = 1)]
    [Required]
    public string NotificationEmailCodeName { get; set; } = "";

    /// <summary>
    /// Channel type (Email/SMS/Both).
    /// </summary>
    [DropDownComponent(Label = "Channel Type", DataProviderType = typeof(ChannelTypeOptionsProvider), Order = 2)]
    [Required]
    public string ChannelType { get; set; } = NotificationChannelType.Email;

    /// <summary>
    /// SMS template with placeholders.
    /// </summary>
    [TextAreaComponent(Label = "SMS Template", Order = 3, ExplanationText = "SMS message template. Use placeholders like {FirstName}, {OrderNumber}. Max 1600 characters.")]
    public string? SmsTemplate { get; set; }

    /// <summary>
    /// The macro field name for phone number extraction.
    /// </summary>
    [TextInputComponent(Label = "Phone Number Field", Order = 4, ExplanationText = "Enter the exact placeholder property name that contains the phone number (e.g., 'Phone', 'PhoneNumber', 'MobilePhone'). This must match a property in the notification's placeholder class.")]
    public string? PhoneFieldName { get; set; }

    /// <summary>
    /// Whether this notification channel is enabled.
    /// </summary>
    [CheckBoxComponent(Label = "Enabled", Order = 5)]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates an empty model.
    /// </summary>
    public NotificationChannelSettingsModel()
    {
    }

    /// <summary>
    /// Creates a model from an existing settings object.
    /// </summary>
    public NotificationChannelSettingsModel(NotificationChannelSettingsInfo settings)
    {
        Id = settings.NotificationChannelSettingsID;
        NotificationEmailCodeName = settings.NotificationEmailCodeName;
        ChannelType = settings.ChannelType;
        SmsTemplate = settings.SmsTemplate;
        PhoneFieldName = settings.PhoneFieldName;
        IsEnabled = settings.IsEnabled;
    }
}

/// <summary>
/// Provides channel type options for dropdown.
/// </summary>
public class ChannelTypeOptionsProvider : IDropDownOptionsProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new DropDownOptionItem { Value = NotificationChannelType.Email, Text = "Email Only" },
            new DropDownOptionItem { Value = NotificationChannelType.SMS, Text = "SMS Only" },
            new DropDownOptionItem { Value = NotificationChannelType.Both, Text = "Both Email and SMS" }
        ]);
}

/// <summary>
/// Provides notification email options from the database.
/// </summary>
public class NotificationEmailOptionsProvider : IDropDownOptionsProvider
{
    /// <inheritdoc />
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>();

        try
        {
            // Query CMS_NotificationEmail table directly using ObjectQuery
            var result = new ObjectQuery("CMS.NotificationEmail")
                .Column("NotificationEmailCodeName")
                .Column("NotificationEmailDisplayName")
                .OrderBy("NotificationEmailDisplayName")
                .GetEnumerableTypedResult();

            foreach (var row in result)
            {
                var codeName = row.GetStringValue("NotificationEmailCodeName", "");
                var displayName = row.GetStringValue("NotificationEmailDisplayName", codeName);

                if (!string.IsNullOrEmpty(codeName))
                {
                    items.Add(new DropDownOptionItem { Value = codeName, Text = displayName });
                }
            }
        }
        catch (Exception)
        {
            // Return empty if table doesn't exist yet
        }

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
