using System;
using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

#nullable enable

[assembly: RegisterObjectType(typeof(Baseline.Account.PasskeyCredentialInfo), Baseline.Account.PasskeyCredentialInfo.OBJECT_TYPE)]

namespace Baseline.Account;

/// <summary>
/// WebAuthn/Passkey credential storage for biometric (fingerprint/Face ID) login.
/// </summary>
public partial class PasskeyCredentialInfo : AbstractInfo<PasskeyCredentialInfo, IInfoProvider<PasskeyCredentialInfo>>, IInfoWithId
{
    public const string OBJECT_TYPE = "baseline.passkeycredential";

    public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(
        typeof(IInfoProvider<PasskeyCredentialInfo>),
        OBJECT_TYPE,
        "Baseline.PasskeyCredential",
        nameof(PasskeyCredentialID),
        nameof(LastModified),
        nameof(PasskeyCredentialGuid),
        null,
        nameof(DeviceName),
        null,
        null,
        null)
    {
        TouchCacheDependencies = true,
        LogEvents = false
    };

    [DatabaseField]
    public virtual int PasskeyCredentialID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PasskeyCredentialID)), 0);
        set => SetValue(nameof(PasskeyCredentialID), value);
    }

    [DatabaseField]
    public virtual Guid PasskeyCredentialGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(PasskeyCredentialGuid)), Guid.Empty);
        set => SetValue(nameof(PasskeyCredentialGuid), value, Guid.Empty);
    }

    /// <summary>Member ID that owns this passkey.</summary>
    [DatabaseField]
    public virtual int MemberID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(MemberID)), 0);
        set => SetValue(nameof(MemberID), value);
    }

    /// <summary>WebAuthn credential ID (base64-encoded).</summary>
    [DatabaseField]
    public virtual string CredentialId
    {
        get => ValidationHelper.GetString(GetValue(nameof(CredentialId)), string.Empty);
        set => SetValue(nameof(CredentialId), value);
    }

    /// <summary>WebAuthn public key in COSE format (base64-encoded).</summary>
    [DatabaseField]
    public virtual string PublicKey
    {
        get => ValidationHelper.GetString(GetValue(nameof(PublicKey)), string.Empty);
        set => SetValue(nameof(PublicKey), value);
    }

    /// <summary>User handle (base64-encoded) for WebAuthn.</summary>
    [DatabaseField]
    public virtual string UserHandle
    {
        get => ValidationHelper.GetString(GetValue(nameof(UserHandle)), string.Empty);
        set => SetValue(nameof(UserHandle), value);
    }

    /// <summary>Signature counter for replay attack protection.</summary>
    [DatabaseField]
    public virtual int SignatureCounter
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(SignatureCounter)), 0);
        set => SetValue(nameof(SignatureCounter), value);
    }

    /// <summary>Authenticator AAGUID (device type identifier).</summary>
    [DatabaseField]
    public virtual string? Aaguid
    {
        get => ValidationHelper.GetString(GetValue(nameof(Aaguid)), null);
        set => SetValue(nameof(Aaguid), value);
    }

    /// <summary>Credential type (e.g., "public-key").</summary>
    [DatabaseField]
    public virtual string CredentialType
    {
        get => ValidationHelper.GetString(GetValue(nameof(CredentialType)), "public-key");
        set => SetValue(nameof(CredentialType), value);
    }

    /// <summary>User-provided name for this device/passkey.</summary>
    [DatabaseField]
    public virtual string DeviceName
    {
        get => ValidationHelper.GetString(GetValue(nameof(DeviceName)), string.Empty);
        set => SetValue(nameof(DeviceName), value);
    }

    /// <summary>Authenticator attachment type (platform, cross-platform).</summary>
    [DatabaseField]
    public virtual string? AttachmentType
    {
        get => ValidationHelper.GetString(GetValue(nameof(AttachmentType)), null);
        set => SetValue(nameof(AttachmentType), value);
    }

    /// <summary>Transports supported by this credential (internal, usb, ble, nfc).</summary>
    [DatabaseField]
    public virtual string? Transports
    {
        get => ValidationHelper.GetString(GetValue(nameof(Transports)), null);
        set => SetValue(nameof(Transports), value);
    }

    /// <summary>Whether the credential is backed up (synced across devices).</summary>
    [DatabaseField]
    public virtual bool IsBackedUp
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsBackedUp)), false);
        set => SetValue(nameof(IsBackedUp), value);
    }

    /// <summary>Whether the credential is currently active.</summary>
    [DatabaseField]
    public virtual bool IsActive
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(IsActive)), true);
        set => SetValue(nameof(IsActive), value);
    }

    [DatabaseField]
    public virtual DateTime CreatedAt
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(CreatedAt)), DateTime.UtcNow);
        set => SetValue(nameof(CreatedAt), value);
    }

    [DatabaseField]
    public virtual DateTime LastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(LastModified)), DateTime.UtcNow);
        set => SetValue(nameof(LastModified), value);
    }

    [DatabaseField]
    public virtual DateTime? LastUsedAt
    {
        get
        {
            var v = GetValue(nameof(LastUsedAt));
            return v == null ? null : ValidationHelper.GetDateTime(v, DateTime.MinValue);
        }
        set => SetValue(nameof(LastUsedAt), value);
    }

    protected override void DeleteObject() => Provider.Delete(this);
    protected override void SetObject() => Provider.Set(this);

    public PasskeyCredentialInfo() : base(TYPEINFO) { }
    public PasskeyCredentialInfo(DataRow dr) : base(TYPEINFO, dr) { }
}
