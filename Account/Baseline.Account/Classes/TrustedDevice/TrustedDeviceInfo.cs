using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

#nullable enable

[assembly: RegisterObjectType(typeof(Baseline.Account.TrustedDeviceInfo), Baseline.Account.TrustedDeviceInfo.OBJECT_TYPE)]

namespace Baseline.Account;

/// <summary>
/// A persistent trusted device record. When a user checks "Remember this device"
/// or approves a device via QR/code flow, a token is stored here and set as a
/// long-lived cookie. Subsequent logins from this device bypass 2FA.
/// Users can view and revoke trusted devices from their account page.
/// </summary>
public partial class TrustedDeviceInfo : AbstractInfo<TrustedDeviceInfo, IInfoProvider<TrustedDeviceInfo>>, IInfoWithId
{
    public const string OBJECT_TYPE = "baseline.trusteddevice";

    public static readonly ObjectTypeInfo TYPEINFO = new(
        typeof(IInfoProvider<TrustedDeviceInfo>),
        OBJECT_TYPE,
        "Baseline.TrustedDevice",
        nameof(TrustedDeviceID),
        nameof(LastUsedAt),
        nameof(TrustedDeviceGuid),
        null, null, null, null, null)
    {
        TouchCacheDependencies = true,
        LogEvents = false
    };

    /// <summary>Primary key.</summary>
    [DatabaseField]
    public virtual int TrustedDeviceID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TrustedDeviceID)), 0);
        set => SetValue(nameof(TrustedDeviceID), value);
    }

    [DatabaseField]
    public virtual Guid TrustedDeviceGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(TrustedDeviceGuid)), Guid.Empty);
        set => SetValue(nameof(TrustedDeviceGuid), value, Guid.Empty);
    }

    /// <summary>The member who owns this trusted device.</summary>
    [DatabaseField]
    public virtual int MemberID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(MemberID)), 0);
        set => SetValue(nameof(MemberID), value);
    }

    /// <summary>Secure random token stored in cookie for device recognition.</summary>
    [DatabaseField]
    public virtual string DeviceToken
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceToken)), string.Empty);
        set => SetValue(nameof(DeviceToken), value);
    }

    /// <summary>SHA-256 fingerprint of (UserAgent|IP prefix).</summary>
    [DatabaseField]
    public virtual string? DeviceFingerprint
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceFingerprint)), null);
        set => SetValue(nameof(DeviceFingerprint), value);
    }

    /// <summary>Friendly device name, e.g. "Chrome on Windows 10".</summary>
    [DatabaseField]
    public virtual string DeviceName
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceName)), string.Empty);
        set => SetValue(nameof(DeviceName), value);
    }

    /// <summary>Device type: Desktop, Mobile, Tablet.</summary>
    [DatabaseField]
    public virtual string? DeviceType
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceType)), null);
        set => SetValue(nameof(DeviceType), value);
    }

    /// <summary>Browser name.</summary>
    [DatabaseField]
    public virtual string? Browser
    {
        get => ValidationHelper.GetString(GetValue(nameof(Browser)), null);
        set => SetValue(nameof(Browser), value);
    }

    /// <summary>Operating system.</summary>
    [DatabaseField]
    public virtual string? OperatingSystem
    {
        get => ValidationHelper.GetString(GetValue(nameof(OperatingSystem)), null);
        set => SetValue(nameof(OperatingSystem), value);
    }

    /// <summary>IP address when device was first trusted.</summary>
    [DatabaseField]
    public virtual string? IpAddress
    {
        get => ValidationHelper.GetString(GetValue(nameof(IpAddress)), null);
        set => SetValue(nameof(IpAddress), value);
    }

    /// <summary>When the device was first trusted.</summary>
    [DatabaseField]
    public virtual DateTime TrustedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(TrustedAt)), DateTime.UtcNow);
        set => SetValue(nameof(TrustedAt), value);
    }

    /// <summary>When the device was last used for authentication.</summary>
    [DatabaseField]
    public virtual DateTime LastUsedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(LastUsedAt)), DateTime.UtcNow);
        set => SetValue(nameof(LastUsedAt), value);
    }

    /// <summary>When the trust expires (null = never, controlled by cookie lifetime).</summary>
    [DatabaseField]
    public virtual DateTime? ExpiresAt
    {
        get
        {
            var val = GetValue(nameof(ExpiresAt));
            return val == null ? null : ValidationHelper.GetDateTime(val, DateTime.MinValue);
        }
        set => SetValue(nameof(ExpiresAt), value);
    }

    /// <summary>Whether this device is still trusted (false = revoked by user).</summary>
    [DatabaseField]
    public virtual bool IsActive
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsActive)), true);
        set => SetValue(nameof(IsActive), value);
    }

    protected override void DeleteObject() => Provider.Delete(this);
    protected override void SetObject() => Provider.Set(this);

    public TrustedDeviceInfo() : base(TYPEINFO) { }
    public TrustedDeviceInfo(DataRow dr) : base(TYPEINFO, dr) { }
}
