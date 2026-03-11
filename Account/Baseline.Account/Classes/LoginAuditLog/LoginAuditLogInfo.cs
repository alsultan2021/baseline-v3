using System;
using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

#nullable enable

[assembly: RegisterObjectType(typeof(Baseline.Account.LoginAuditLogInfo), Baseline.Account.LoginAuditLogInfo.OBJECT_TYPE)]

namespace Baseline.Account;

/// <summary>
/// Audit trail for login attempts: successful logins, failed attempts, lockouts, 2FA events.
/// </summary>
public partial class LoginAuditLogInfo : AbstractInfo<LoginAuditLogInfo, IInfoProvider<LoginAuditLogInfo>>, IInfoWithId
{
    public const string OBJECT_TYPE = "baseline.loginauditlog";

    public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(
        typeof(IInfoProvider<LoginAuditLogInfo>),
        OBJECT_TYPE,
        "Baseline.LoginAuditLog",
        nameof(LoginAuditLogID),
        nameof(AttemptedAt),
        nameof(LoginAuditLogGuid),
        null,
        null,
        null,
        null,
        null)
    {
        TouchCacheDependencies = true,
        LogEvents = false // Don't log these to event log (would cause recursion)
    };

    [DatabaseField]
    public virtual int LoginAuditLogID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(LoginAuditLogID)), 0);
        set => SetValue(nameof(LoginAuditLogID), value);
    }

    [DatabaseField]
    public virtual Guid LoginAuditLogGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(LoginAuditLogGuid)), Guid.Empty);
        set => SetValue(nameof(LoginAuditLogGuid), value, Guid.Empty);
    }

    /// <summary>Member ID if known (may be null for failed attempts with unknown username).</summary>
    [DatabaseField]
    public virtual int? MemberID
    {
        get
        {
            int v = ValidationHelper.GetInteger(GetValue(nameof(MemberID)), 0);
            return v == 0 ? null : v;
        }
        set => SetValue(nameof(MemberID), value, 0);
    }

    /// <summary>Username or email used in the login attempt.</summary>
    [DatabaseField]
    public virtual string Username
    {
        get => ValidationHelper.GetString(GetValue(nameof(Username)), string.Empty);
        set => SetValue(nameof(Username), value);
    }

    /// <summary>Login action type — see <see cref="LoginAuditActionType"/>.</summary>
    [DatabaseField]
    public virtual string ActionType
    {
        get => ValidationHelper.GetString(GetValue(nameof(ActionType)), string.Empty);
        set => SetValue(nameof(ActionType), value);
    }

    /// <summary>Whether the login attempt was successful.</summary>
    [DatabaseField]
    public virtual bool IsSuccess
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsSuccess)), false);
        set => SetValue(nameof(IsSuccess), value);
    }

    /// <summary>IP address of the client.</summary>
    [DatabaseField]
    public virtual string? IpAddress
    {
        get => ValidationHelper.GetString(GetValue(nameof(IpAddress)), null);
        set => SetValue(nameof(IpAddress), value);
    }

    /// <summary>User agent string from the request.</summary>
    [DatabaseField]
    public virtual string? UserAgent
    {
        get => ValidationHelper.GetString(GetValue(nameof(UserAgent)), null);
        set => SetValue(nameof(UserAgent), value);
    }

    /// <summary>Parsed device type (Desktop, Mobile, Tablet, Unknown).</summary>
    [DatabaseField]
    public virtual string? DeviceType
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceType)), null);
        set => SetValue(nameof(DeviceType), value);
    }

    /// <summary>Parsed browser name.</summary>
    [DatabaseField]
    public virtual string? Browser
    {
        get => ValidationHelper.GetString(GetValue(nameof(Browser)), null);
        set => SetValue(nameof(Browser), value);
    }

    /// <summary>Parsed operating system.</summary>
    [DatabaseField]
    public virtual string? OperatingSystem
    {
        get => ValidationHelper.GetString(GetValue(nameof(OperatingSystem)), null);
        set => SetValue(nameof(OperatingSystem), value);
    }

    /// <summary>Geographic location (city, country) if resolved from IP.</summary>
    [DatabaseField]
    public virtual string? Location
    {
        get => ValidationHelper.GetString(GetValue(nameof(Location)), null);
        set => SetValue(nameof(Location), value);
    }

    /// <summary>Device fingerprint hash for device recognition.</summary>
    [DatabaseField]
    public virtual string? DeviceFingerprint
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceFingerprint)), null);
        set => SetValue(nameof(DeviceFingerprint), value);
    }

    /// <summary>Failure reason code if login failed.</summary>
    [DatabaseField]
    public virtual string? FailureReason
    {
        get => ValidationHelper.GetString(GetValue(nameof(FailureReason)), null);
        set => SetValue(nameof(FailureReason), value);
    }

    /// <summary>Whether this login was from a new/unrecognized device.</summary>
    [DatabaseField]
    public virtual bool IsNewDevice
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsNewDevice)), false);
        set => SetValue(nameof(IsNewDevice), value);
    }

    /// <summary>Whether a new device alert email was sent.</summary>
    [DatabaseField]
    public virtual bool AlertSent
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(AlertSent)), false);
        set => SetValue(nameof(AlertSent), value);
    }

    [DatabaseField]
    public virtual DateTime AttemptedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(AttemptedAt)), DateTime.UtcNow);
        set => SetValue(nameof(AttemptedAt), value);
    }

    protected override void DeleteObject() => Provider.Delete(this);
    protected override void SetObject() => Provider.Set(this);

    public LoginAuditLogInfo() : base(TYPEINFO) { }
    public LoginAuditLogInfo(DataRow dr) : base(TYPEINFO, dr) { }
}

/// <summary>
/// Login audit action types.
/// </summary>
public static class LoginAuditActionType
{
    public const string LoginSuccess = "LoginSuccess";
    public const string LoginFailed = "LoginFailed";
    public const string Logout = "Logout";
    public const string AccountLocked = "AccountLocked";
    public const string TwoFactorRequired = "TwoFactorRequired";
    public const string TwoFactorSuccess = "TwoFactorSuccess";
    public const string TwoFactorFailed = "TwoFactorFailed";
    public const string PasswordReset = "PasswordReset";
    public const string PasswordChanged = "PasswordChanged";
    public const string AccountCreated = "AccountCreated";
    public const string ExternalLogin = "ExternalLogin";
    public const string SessionExpired = "SessionExpired";
    public const string SessionTerminated = "SessionTerminated";
}
