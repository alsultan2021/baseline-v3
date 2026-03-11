using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

using Baseline.Core.Admin.Sms.InfoClasses;

namespace Baseline.Core.Admin.Sms;

/// <summary>
/// Installs the Twilio SMS settings database schema.
/// </summary>
public class TwilioSmsModuleInstaller(IInfoProvider<ResourceInfo> resourceProvider)
{
    private const string RESOURCE_NAME = "Baseline.TwilioSms";

    /// <summary>
    /// Installs the Twilio SMS settings database schema and resource.
    /// </summary>
    public void Install()
    {
        var resource = resourceProvider.Get(RESOURCE_NAME) ?? new ResourceInfo();
        InitializeResource(resource);
        InstallTwilioSmsSettingsInfo(resource);
        InstallNotificationChannelSettingsInfo(resource);
    }

    private void InitializeResource(ResourceInfo resource)
    {
        resource.ResourceDisplayName = "Twilio SMS Notifications";
        resource.ResourceName = RESOURCE_NAME;
        resource.ResourceDescription = "Twilio SMS notification settings for Xperience by Kentico";
        resource.ResourceIsInDevelopment = false;

        if (resource.HasChanged)
        {
            resourceProvider.Set(resource);
        }
    }

    private void InstallTwilioSmsSettingsInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(TwilioSmsSettingsInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(TwilioSmsSettingsInfo.OBJECT_TYPE);

        info.ClassName = TwilioSmsSettingsInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = TwilioSmsSettingsInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Twilio SMS Settings";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsID));

        // GUID field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsGuid),
            AllowEmpty = false,
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true
        });

        // LastModified field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsLastModified),
            AllowEmpty = false,
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true
        });

        // CodeName field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsCodeName),
            AllowEmpty = false,
            Visible = true,
            Size = 100,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Code name"
        });

        // DisplayName field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsDisplayName),
            AllowEmpty = false,
            Visible = true,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Display name"
        });

        // AccountSid field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.AccountSid),
            AllowEmpty = false,
            Visible = true,
            Size = 100,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Account SID"
        });

        // AuthToken field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.AuthToken),
            AllowEmpty = false,
            Visible = true,
            Size = 100,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Auth Token"
        });

        // FromPhoneNumber field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.FromPhoneNumber),
            AllowEmpty = true,
            Visible = true,
            Size = 20,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "From Phone Number"
        });

        // MessagingServiceSid field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.MessagingServiceSid),
            AllowEmpty = true,
            Visible = true,
            Size = 100,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Messaging Service SID"
        });

        // DefaultCountryCode field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.DefaultCountryCode),
            AllowEmpty = false,
            Visible = true,
            Size = 10,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Default Country Code",
            DefaultValue = "+1"
        });

        // Environment field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.Environment),
            AllowEmpty = false,
            Visible = true,
            Size = 20,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Environment",
            DefaultValue = "Test"
        });

        // IsEnabled field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.IsEnabled),
            AllowEmpty = false,
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Caption = "Enabled",
            DefaultValue = "true"
        });

        // UseVerifyService field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.UseVerifyService),
            AllowEmpty = false,
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Caption = "Use Verify Service",
            DefaultValue = "false"
        });

        // VerifyServiceSid field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(TwilioSmsSettingsInfo.VerifyServiceSid),
            AllowEmpty = true,
            Visible = true,
            Size = 100,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Verify Service SID"
        });

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }

    private static void SetFormDefinition(DataClassInfo info, FormInfo formInfo)
    {
        if (info.ClassID > 0)
        {
            var existingForm = new FormInfo(info.ClassFormDefinition);
            existingForm.CombineWithForm(formInfo, new CombineWithFormSettings
            {
                OverwriteExisting = false
            });
            info.ClassFormDefinition = existingForm.GetXmlDefinition();
        }
        else
        {
            info.ClassFormDefinition = formInfo.GetXmlDefinition();
        }
    }

    private void InstallNotificationChannelSettingsInfo(ResourceInfo resource)
    {
        var info = DataClassInfoProvider.GetDataClassInfo(NotificationChannelSettingsInfo.OBJECT_TYPE)
            ?? DataClassInfo.New(NotificationChannelSettingsInfo.OBJECT_TYPE);

        info.ClassName = NotificationChannelSettingsInfo.TYPEINFO.ObjectClassName;
        info.ClassTableName = NotificationChannelSettingsInfo.TYPEINFO.ObjectClassName.Replace(".", "_");
        info.ClassDisplayName = "Notification Channel Settings";
        info.ClassType = ClassType.OTHER;
        info.ClassResourceID = resource.ResourceID;

        var formInfo = FormHelper.GetBasicFormDefinition(nameof(NotificationChannelSettingsInfo.NotificationChannelSettingsID));

        // GUID field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.NotificationChannelSettingsGuid),
            AllowEmpty = false,
            Visible = false,
            DataType = FieldDataType.Guid,
            Enabled = true
        });

        // LastModified field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.NotificationChannelSettingsLastModified),
            AllowEmpty = false,
            Visible = false,
            DataType = FieldDataType.DateTime,
            Enabled = true
        });

        // NotificationEmailCodeName field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.NotificationEmailCodeName),
            AllowEmpty = false,
            Visible = true,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Notification Email Code Name"
        });

        // ChannelType field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.ChannelType),
            AllowEmpty = false,
            Visible = true,
            Size = 20,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Channel Type",
            DefaultValue = NotificationChannelType.Email
        });

        // SmsTemplate field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.SmsTemplate),
            AllowEmpty = true,
            Visible = true,
            Size = 1600,
            DataType = FieldDataType.LongText,
            Enabled = true,
            Caption = "SMS Template"
        });

        // PhoneFieldName field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.PhoneFieldName),
            AllowEmpty = true,
            Visible = true,
            Size = 200,
            DataType = FieldDataType.Text,
            Enabled = true,
            Caption = "Phone Number Field"
        });

        // IsEnabled field
        formInfo.AddFormItem(new FormFieldInfo
        {
            Name = nameof(NotificationChannelSettingsInfo.IsEnabled),
            AllowEmpty = false,
            Visible = true,
            DataType = FieldDataType.Boolean,
            Enabled = true,
            Caption = "Enabled",
            DefaultValue = "true"
        });

        SetFormDefinition(info, formInfo);

        if (info.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(info);
        }
    }
}
