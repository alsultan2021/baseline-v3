using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Account;

/// <summary>
/// Installer for PasskeyCredential database class for WebAuthn/Passkey biometric authentication.
/// </summary>
public static class PasskeyCredentialInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(PasskeyCredentialInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(PasskeyCredentialInfo.OBJECT_TYPE);
        }

        info!.ClassName = PasskeyCredentialInfo.OBJECT_TYPE;
        info.ClassTableName = "Baseline_PasskeyCredential";
        info.ClassDisplayName = "Passkey Credential";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(PasskeyCredentialInfo.PasskeyCredentialID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.PasskeyCredentialGuid),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.MemberID),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.CredentialId),
            DataType = FieldDataType.Text,
            Size = 2000, // Base64-encoded credential ID can be large
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.PublicKey),
            DataType = FieldDataType.LongText,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.UserHandle),
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.SignatureCounter),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "0"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.Aaguid),
            DataType = FieldDataType.Text,
            Size = 36, // UUID format
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.CredentialType),
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "public-key"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.DeviceName),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.AttachmentType),
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.Transports),
            DataType = FieldDataType.Text,
            Size = 200, // JSON array of transports
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.IsBackedUp),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.IsActive),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "true"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.CreatedAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.LastModified),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(PasskeyCredentialInfo.LastUsedAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        info!.ClassFormDefinition = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(info);
    }

    private static void EnsureField(FormInfo formInfo, FormFieldInfo field)
    {
        var existing = formInfo.GetFormField(field.Name);
        if (existing == null)
        {
            formInfo.AddFormItem(field);
        }
        else
        {
            formInfo.UpdateFormField(field.Name, field);
        }
    }
}
