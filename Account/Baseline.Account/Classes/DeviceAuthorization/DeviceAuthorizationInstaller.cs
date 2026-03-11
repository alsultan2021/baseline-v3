using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Account;

/// <summary>
/// Installer for the Baseline_DeviceAuthorization table.
/// </summary>
public static class DeviceAuthorizationInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(DeviceAuthorizationInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(DeviceAuthorizationInfo.OBJECT_TYPE);
        }

        info!.ClassName = DeviceAuthorizationInfo.OBJECT_TYPE;
        info.ClassTableName = "Baseline_DeviceAuthorization";
        info.ClassDisplayName = "Device Authorization";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(DeviceAuthorizationInfo.DeviceAuthorizationID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.DeviceAuthorizationGuid),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.UserCode),
            DataType = FieldDataType.Text,
            Size = 20,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.DeviceCode),
            DataType = FieldDataType.Text,
            Size = 64,
            Enabled = true,
            Visible = false,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.ApprovedByMemberID),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.Status),
            DataType = FieldDataType.Text,
            Size = 20,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = DeviceAuthorizationStatus.Pending
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.RequestingIpAddress),
            DataType = FieldDataType.Text,
            Size = 45,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.RequestingUserAgent),
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.RequestingDeviceFingerprint),
            DataType = FieldDataType.Text,
            Size = 64,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.RequestingDeviceName),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.ExpiresAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(DeviceAuthorizationInfo.CreatedAt),
            DataType = FieldDataType.DateTime,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
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
