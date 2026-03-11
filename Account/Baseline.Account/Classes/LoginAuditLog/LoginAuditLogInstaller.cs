using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

namespace Baseline.Account;

/// <summary>
/// Installer for LoginAuditLog database class.
/// </summary>
public static class LoginAuditLogInstaller
{
    public static void Install(ResourceInfo resourceInfo)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(LoginAuditLogInfo.OBJECT_TYPE);
        bool isNew = info == null;

        if (isNew)
        {
            info = DataClassInfo.New(LoginAuditLogInfo.OBJECT_TYPE);
        }

        info!.ClassName = LoginAuditLogInfo.OBJECT_TYPE;
        info.ClassTableName = "Baseline_LoginAuditLog";
        info.ClassDisplayName = "Login Audit Log";
        info.ClassResourceID = resourceInfo.ResourceID;
        info.ClassType = ClassType.OTHER;

        var formInfo = isNew
            ? FormHelper.GetBasicFormDefinition(nameof(LoginAuditLogInfo.LoginAuditLogID))
            : new FormInfo(info!.ClassFormDefinition);

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.LoginAuditLogGuid),
            DataType = FieldDataType.Guid,
            Enabled = true,
            Visible = false,
            AllowEmpty = false,
            DefaultValue = "##NEWGUID##"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.MemberID),
            DataType = FieldDataType.Integer,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.Username),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.ActionType),
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            Visible = true,
            AllowEmpty = false
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.IsSuccess),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.IpAddress),
            DataType = FieldDataType.Text,
            Size = 45, // IPv6 max length
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.UserAgent),
            DataType = FieldDataType.Text,
            Size = 500,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.DeviceType),
            DataType = FieldDataType.Text,
            Size = 50,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.Browser),
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.OperatingSystem),
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.Location),
            DataType = FieldDataType.Text,
            Size = 200,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.DeviceFingerprint),
            DataType = FieldDataType.Text,
            Size = 64, // SHA-256 hex
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.FailureReason),
            DataType = FieldDataType.Text,
            Size = 100,
            Enabled = true,
            Visible = true,
            AllowEmpty = true
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.IsNewDevice),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.AlertSent),
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Visible = true,
            AllowEmpty = false,
            DefaultValue = "false"
        });

        EnsureField(formInfo, new FormFieldInfo
        {
            Name = nameof(LoginAuditLogInfo.AttemptedAt),
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
