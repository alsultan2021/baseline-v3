using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

#nullable enable

[assembly: RegisterObjectType(typeof(Baseline.Account.DeviceAuthorizationInfo), Baseline.Account.DeviceAuthorizationInfo.OBJECT_TYPE)]

namespace Baseline.Account;

/// <summary>
/// Pending device authorization request: a short-lived code that an authenticated user
/// can approve to sign-in an unauthenticated device (Netflix / Google TV style).
/// </summary>
public partial class DeviceAuthorizationInfo : AbstractInfo<DeviceAuthorizationInfo, IInfoProvider<DeviceAuthorizationInfo>>, IInfoWithId
{
    public const string OBJECT_TYPE = "baseline.deviceauthorization";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<DeviceAuthorizationInfo>),
        OBJECT_TYPE,
        "Baseline.DeviceAuthorization",
        nameof(DeviceAuthorizationID),
        nameof(CreatedAt),
        nameof(DeviceAuthorizationGuid),
        null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        LogEvents = false
    };

    /// <summary>Primary key.</summary>
    [DatabaseField]
    public virtual int DeviceAuthorizationID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(DeviceAuthorizationID)), 0);
        set => SetValue(nameof(DeviceAuthorizationID), value);
    }

    [DatabaseField]
    public virtual Guid DeviceAuthorizationGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(DeviceAuthorizationGuid)), Guid.Empty);
        set => SetValue(nameof(DeviceAuthorizationGuid), value, Guid.Empty);
    }

    /// <summary>Short user-facing code, e.g. "ABCD-1234".</summary>
    [DatabaseField]
    public virtual string UserCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(UserCode)), string.Empty);
        set => SetValue(nameof(UserCode), value);
    }

    /// <summary>Secret token the polling device uses to check status.</summary>
    [DatabaseField]
    public virtual string DeviceCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceCode)), string.Empty);
        set => SetValue(nameof(DeviceCode), value);
    }

    /// <summary>MemberID of the user who approved the request (null until approved).</summary>
    [DatabaseField]
    public virtual int? ApprovedByMemberID
    {
        get
        {
            int v = ValidationHelper.GetInteger(GetValue(nameof(ApprovedByMemberID)), 0);
            return v == 0 ? null : v;
        }
        set => SetValue(nameof(ApprovedByMemberID), value, 0);
    }

    /// <summary>Pending | Approved | Denied | Expired.</summary>
    [DatabaseField]
    public virtual string Status
    {
        get => ValidationHelper.GetString(GetValue(nameof(Status)), DeviceAuthorizationStatus.Pending);
        set => SetValue(nameof(Status), value);
    }

    /// <summary>IP address of the requesting device.</summary>
    [DatabaseField]
    public virtual string? RequestingIpAddress
    {
        get => ValidationHelper.GetString(GetValue(nameof(RequestingIpAddress)), null);
        set => SetValue(nameof(RequestingIpAddress), value);
    }

    /// <summary>User-agent of the requesting device.</summary>
    [DatabaseField]
    public virtual string? RequestingUserAgent
    {
        get => ValidationHelper.GetString(GetValue(nameof(RequestingUserAgent)), null);
        set => SetValue(nameof(RequestingUserAgent), value);
    }

    /// <summary>Device fingerprint of the requesting device.</summary>
    [DatabaseField]
    public virtual string? RequestingDeviceFingerprint
    {
        get => ValidationHelper.GetString(GetValue(nameof(RequestingDeviceFingerprint)), null);
        set => SetValue(nameof(RequestingDeviceFingerprint), value);
    }

    /// <summary>Friendly name parsed from user-agent (e.g. "Chrome on Windows 10").</summary>
    [DatabaseField]
    public virtual string? RequestingDeviceName
    {
        get => ValidationHelper.GetString(GetValue(nameof(RequestingDeviceName)), null);
        set => SetValue(nameof(RequestingDeviceName), value);
    }

    /// <summary>When the code expires.</summary>
    [DatabaseField]
    public virtual DateTime ExpiresAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ExpiresAt)), DateTime.UtcNow);
        set => SetValue(nameof(ExpiresAt), value);
    }

    [DatabaseField]
    public virtual DateTime CreatedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(CreatedAt)), DateTime.UtcNow);
        set => SetValue(nameof(CreatedAt), value);
    }

    protected override void DeleteObject() => Provider.Delete(this);
    protected override void SetObject() => Provider.Set(this);

    public DeviceAuthorizationInfo() : base(TYPEINFO) { }
    public DeviceAuthorizationInfo(DataRow dr) : base(TYPEINFO, dr) { }
}

/// <summary>
/// Status values for device authorization requests.
/// </summary>
public static class DeviceAuthorizationStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Denied = "Denied";
    public const string Expired = "Expired";
}
