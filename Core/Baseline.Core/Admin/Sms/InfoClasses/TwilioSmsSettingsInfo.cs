using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;

using Baseline.Core.Admin.Sms.InfoClasses;

[assembly: RegisterObjectType(typeof(TwilioSmsSettingsInfo), TwilioSmsSettingsInfo.OBJECT_TYPE)]

namespace Baseline.Core.Admin.Sms.InfoClasses;

/// <summary>
/// Data container class for Twilio SMS settings.
/// </summary>
public class TwilioSmsSettingsInfo : AbstractInfo<TwilioSmsSettingsInfo, IInfoProvider<TwilioSmsSettingsInfo>>, IInfoWithId
{
    /// <summary>
    /// Object type.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.twiliosmssettings";

    /// <summary>
    /// Type information.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<TwilioSmsSettingsInfo>),
        OBJECT_TYPE,
        "Baseline.TwilioSmsSettings",
        nameof(TwilioSmsSettingsID),
        nameof(TwilioSmsSettingsLastModified),
        nameof(TwilioSmsSettingsGuid),
        nameof(TwilioSmsSettingsCodeName),
        nameof(TwilioSmsSettingsDisplayName),
        null,
        null,
        null)
    {
        TouchCacheDependencies = true,
        ContinuousIntegrationSettings = { Enabled = true }
    };

    /// <summary>
    /// Settings ID.
    /// </summary>
    [DatabaseField]
    public virtual int TwilioSmsSettingsID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TwilioSmsSettingsID)), 0);
        set => SetValue(nameof(TwilioSmsSettingsID), value);
    }

    /// <summary>
    /// Settings GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid TwilioSmsSettingsGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(TwilioSmsSettingsGuid)), Guid.Empty);
        set => SetValue(nameof(TwilioSmsSettingsGuid), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime TwilioSmsSettingsLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(TwilioSmsSettingsLastModified)), DateTime.MinValue);
        set => SetValue(nameof(TwilioSmsSettingsLastModified), value);
    }

    /// <summary>
    /// Code name for the settings entry.
    /// </summary>
    [DatabaseField]
    public virtual string TwilioSmsSettingsCodeName
    {
        get => ValidationHelper.GetString(GetValue(nameof(TwilioSmsSettingsCodeName)), string.Empty);
        set => SetValue(nameof(TwilioSmsSettingsCodeName), value);
    }

    /// <summary>
    /// Display name for the settings entry.
    /// </summary>
    [DatabaseField]
    public virtual string TwilioSmsSettingsDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(TwilioSmsSettingsDisplayName)), string.Empty);
        set => SetValue(nameof(TwilioSmsSettingsDisplayName), value);
    }

    /// <summary>
    /// Twilio Account SID.
    /// </summary>
    [DatabaseField]
    public virtual string AccountSid
    {
        get => ValidationHelper.GetString(GetValue(nameof(AccountSid)), string.Empty);
        set => SetValue(nameof(AccountSid), value);
    }

    /// <summary>
    /// Twilio Auth Token (stored encrypted).
    /// </summary>
    [DatabaseField]
    public virtual string AuthToken
    {
        get => ValidationHelper.GetString(GetValue(nameof(AuthToken)), string.Empty);
        set => SetValue(nameof(AuthToken), value);
    }

    /// <summary>
    /// Sender phone number in E.164 format (e.g., +15551234567).
    /// </summary>
    [DatabaseField]
    public virtual string FromPhoneNumber
    {
        get => ValidationHelper.GetString(GetValue(nameof(FromPhoneNumber)), string.Empty);
        set => SetValue(nameof(FromPhoneNumber), value);
    }

    /// <summary>
    /// Optional Twilio Messaging Service SID.
    /// </summary>
    [DatabaseField]
    public virtual string? MessagingServiceSid
    {
        get => ValidationHelper.GetString(GetValue(nameof(MessagingServiceSid)), null);
        set => SetValue(nameof(MessagingServiceSid), value);
    }

    /// <summary>
    /// Default country code for phone number formatting (e.g., +1).
    /// </summary>
    [DatabaseField]
    public virtual string DefaultCountryCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(DefaultCountryCode)), "+1");
        set => SetValue(nameof(DefaultCountryCode), value);
    }

    /// <summary>
    /// Environment: Test or Production.
    /// </summary>
    [DatabaseField]
    public virtual string Environment
    {
        get => ValidationHelper.GetString(GetValue(nameof(Environment)), "Test");
        set => SetValue(nameof(Environment), value);
    }

    /// <summary>
    /// Whether this configuration is enabled.
    /// </summary>
    [DatabaseField]
    public virtual bool IsEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsEnabled)), true);
        set => SetValue(nameof(IsEnabled), value);
    }

    /// <summary>
    /// Whether to use Twilio Verify service for OTP codes.
    /// </summary>
    [DatabaseField]
    public virtual bool UseVerifyService
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(UseVerifyService)), false);
        set => SetValue(nameof(UseVerifyService), value);
    }

    /// <summary>
    /// Twilio Verify Service SID (required if UseVerifyService is true).
    /// </summary>
    [DatabaseField]
    public virtual string? VerifyServiceSid
    {
        get => ValidationHelper.GetString(GetValue(nameof(VerifyServiceSid)), null);
        set => SetValue(nameof(VerifyServiceSid), value);
    }

    /// <inheritdoc />
    public int Id
    {
        get => TwilioSmsSettingsID;
        set => TwilioSmsSettingsID = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TwilioSmsSettingsInfo"/> class.
    /// </summary>
    public TwilioSmsSettingsInfo() : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Initializes a new instance from a DataRow.
    /// </summary>
    public TwilioSmsSettingsInfo(DataRow dr) : base(TYPEINFO, dr)
    {
    }

    /// <inheritdoc />
    protected override void DeleteObject() => Provider.Delete(this);

    /// <inheritdoc />
    protected override void SetObject() => Provider.Set(this);
}
