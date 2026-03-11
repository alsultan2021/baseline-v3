using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using CMS.Modules;

using Baseline.Core.Admin.Sms.InfoClasses;

namespace Baseline.Core.Admin.Sms.Modules;

/// <summary>
/// Extends CMS_NotificationEmail class with SMS fields visible in notification properties UI
/// </summary>
public class TwilioNotificationExtensionInstaller(IEventLogService eventLogService)
{
    public void Install()
    {
        // Try multiple possible class names
        string[] possibleNames = [
            "CMS.NotificationEmail",
            "CMS.Notification",
            "CMS.Notifications.NotificationEmail",
            "Kentico.Notifications.NotificationEmail",
            "NotificationEmail"
        ];

        DataClassInfo? notificationClass = null;
        string? foundClassName = null;

        foreach (var name in possibleNames)
        {
            notificationClass = DataClassInfoProvider.GetDataClassInfo(name);
            if (notificationClass != null)
            {
                foundClassName = name;
                break;
            }
        }

        if (notificationClass == null)
        {
            eventLogService.LogWarning(
                source: nameof(TwilioNotificationExtensionInstaller),
                eventCode: "SMS_CLASS_NOT_FOUND",
                eventDescription: $"Could not find notification class. Tried: {string.Join(", ", possibleNames)}");
            return;
        }

        var formInfo = new FormInfo(notificationClass.ClassFormDefinition);

        // Channel Type field (Properties tab - for delivery method selection)
        if (formInfo.GetFormField("NotificationChannelType") == null)
        {
            formInfo.AddFormItem(new FormFieldInfo
            {
                Name = "NotificationChannelType",
                AllowEmpty = false,
                DataType = FieldDataType.Text,
                Size = 50,
                DefaultValue = "Email",
                Caption = "Channel type",
                Visible = true,
                Enabled = true
            });
        }

        // SMS Configuration ID field (Properties tab - references TwilioSmsSettings)
        if (formInfo.GetFormField("NotificationSmsConfigurationID") == null)
        {
            formInfo.AddFormItem(new FormFieldInfo
            {
                Name = "NotificationSmsConfigurationID",
                AllowEmpty = true,
                DataType = FieldDataType.Integer,
                Caption = "SMS configuration",
                Visible = true,
                Enabled = true,
                ReferenceToObjectType = TwilioSmsSettingsInfo.OBJECT_TYPE,
                ReferenceType = ObjectDependencyEnum.NotRequired
            });
        }

        // Phone Field Name (Properties tab - which placeholder property has phone)
        if (formInfo.GetFormField("NotificationPhoneField") == null)
        {
            formInfo.AddFormItem(new FormFieldInfo
            {
                Name = "NotificationPhoneField",
                AllowEmpty = true,
                DataType = FieldDataType.Text,
                Size = 200,
                Caption = "Phone placeholder field",
                Visible = true,
                Enabled = true
            });
        }

        // SMS Content field (Content tab - the actual SMS message)
        if (formInfo.GetFormField("NotificationSmsContent") == null)
        {
            formInfo.AddFormItem(new FormFieldInfo
            {
                Name = "NotificationSmsContent",
                AllowEmpty = true,
                DataType = FieldDataType.LongText,
                Caption = "SMS content",
                Visible = true,
                Enabled = true
            });
        }

        // Update class definitions
        notificationClass.ClassFormDefinition = formInfo.GetXmlDefinition();
        notificationClass.ClassXmlSchema = formInfo.GetXmlDefinition();
        DataClassInfoProvider.SetDataClassInfo(notificationClass);
    }
}
