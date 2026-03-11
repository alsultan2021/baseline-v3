using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Account;

/// <summary>
/// Installer for the Baseline_TrustedDevice table.
/// </summary>
public static class TrustedDeviceInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(TrustedDeviceInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(TrustedDeviceInfo.OBJECT_TYPE);
        }

        info!.ClassName = TrustedDeviceInfo.OBJECT_TYPE;
        info.ClassTableName = "Baseline_TrustedDevice";
        info.ClassDisplayName = "Trusted Device";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(TrustedDeviceInfo.TrustedDeviceID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.TrustedDeviceGuid),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.MemberID),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.DeviceToken),
            DataType = FieldDataType.Text,
            Size = 64,
            Enabled = true,
            Visible = false,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.DeviceFingerprint),
            DataType = FieldDataType.Text,
            Size = 64,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.DeviceName),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.DeviceType),
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.Browser),
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.OperatingSystem),
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.IpAddress),
            DataType = FieldDataType.Text,
            Size = 45,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.TrustedAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.LastUsedAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.ExpiresAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(TrustedDeviceInfo.IsActive),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "true"
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
