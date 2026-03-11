using CMS;
using CMS.DataEngine;

using Baseline.Core.Admin.Sms.InfoClasses;

[assembly: RegisterObjectType(typeof(NotificationChannelSettingsInfo), NotificationChannelSettingsInfo.OBJECT_TYPE)]

namespace Baseline.Core.Admin.Sms.InfoClasses;

/// <summary>
/// Stores channel settings (Email/SMS/Both) for each notification email template.
/// Links to CMS_NotificationEmail by code name.
/// </summary>
[Serializable]
public class NotificationChannelSettingsInfo : AbstractInfo<NotificationChannelSettingsInfo, IInfoProvider<NotificationChannelSettingsInfo>>
{
    /// <summary>
    /// Object type identifier.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.notificationchannelsettings";

    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<NotificationChannelSettingsInfo>),
        OBJECT_TYPE,
        "Baseline.NotificationChannelSettings",
        nameof(NotificationChannelSettingsID),
        nameof(NotificationChannelSettingsLastModified),
        nameof(NotificationChannelSettingsGuid),
        nameof(NotificationEmailCodeName),
        nameof(NotificationEmailCodeName),
        null,
        null,
        null)
    {
        TouchCacheDependencies = true,
        LogEvents = true
    };

    /// <summary>
    /// Creates a new instance of the class.
    /// </summary>
    public NotificationChannelSettingsInfo() : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Primary key ID.
    /// </summary>
    [DatabaseField]
    public virtual int NotificationChannelSettingsID
    {
        get => GetIntegerValue(nameof(NotificationChannelSettingsID), 0);
        set => SetValue(nameof(NotificationChannelSettingsID), value);
    }

    /// <summary>
    /// GUID for the settings.
    /// </summary>
    [DatabaseField]
    public virtual Guid NotificationChannelSettingsGuid
    {
        get => GetGuidValue(nameof(NotificationChannelSettingsGuid), Guid.Empty);
        set => SetValue(nameof(NotificationChannelSettingsGuid), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime NotificationChannelSettingsLastModified
    {
        get => GetDateTimeValue(nameof(NotificationChannelSettingsLastModified), DateTime.Now);
        set => SetValue(nameof(NotificationChannelSettingsLastModified), value);
    }

    /// <summary>
    /// The code name of the notification email (links to CMS_NotificationEmail.NotificationEmailCodeName).
    /// </summary>
    [DatabaseField]
    public virtual string NotificationEmailCodeName
    {
        get => GetStringValue(nameof(NotificationEmailCodeName), string.Empty);
        set => SetValue(nameof(NotificationEmailCodeName), value);
    }

    /// <summary>
    /// Channel type: Email, SMS, or Both.
    /// </summary>
    [DatabaseField]
    public virtual string ChannelType
    {
        get => GetStringValue(nameof(ChannelType), NotificationChannelType.Email);
        set => SetValue(nameof(ChannelType), value);
    }

    /// <summary>
    /// SMS message template (used when ChannelType includes SMS).
    /// Supports placeholders like {FirstName}, {OrderNumber}, etc.
    /// </summary>
    [DatabaseField]
    public virtual string? SmsTemplate
    {
        get => GetStringValue(nameof(SmsTemplate), null);
        set => SetValue(nameof(SmsTemplate), value);
    }

    /// <summary>
    /// The macro field name for the recipient phone number (e.g., "PhoneNumber" or "MobilePhone").
    /// Used to extract phone number from notification context.
    /// </summary>
    [DatabaseField]
    public virtual string? PhoneFieldName
    {
        get => GetStringValue(nameof(PhoneFieldName), null);
        set => SetValue(nameof(PhoneFieldName), value);
    }

    /// <summary>
    /// Whether this notification is enabled.
    /// </summary>
    [DatabaseField]
    public virtual bool IsEnabled
    {
        get => GetBooleanValue(nameof(IsEnabled), true);
        set => SetValue(nameof(IsEnabled), value);
    }
}

/// <summary>
/// Constants for notification channel types.
/// </summary>
public static class NotificationChannelType
{
    /// <summary>Email only (default Kentico behavior).</summary>
    public const string Email = "Email";

    /// <summary>SMS only.</summary>
    public const string SMS = "SMS";

    /// <summary>Both Email and SMS.</summary>
    public const string Both = "Both";
}
