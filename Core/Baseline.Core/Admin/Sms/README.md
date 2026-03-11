# SMS Notification System

This module extends Xperience by Kentico's notification system to support SMS delivery via Twilio, in addition to email notifications.

## Features

- **Dual Channel Support**: Configure notifications to send via Email, SMS, or Both
- **Integrated UI**: SMS custom fields are automatically added to notification email class
- **Phone Field Selection**: Specify which placeholder property contains the recipient's phone number
- **SMS Templates**: Specify SMS template code names per notification
- **Multiple Twilio Accounts**: Configure multiple Twilio accounts via SMS Configurations

## Quick Start

### 1. Set Up Twilio SMS Settings

Navigate to **SMS Notifications > SMS Configurations** in the admin interface.

Create a new SMS configuration with:

- Display name (e.g., "Production SMS")
- Twilio Account SID
- Twilio Auth Token
- From Phone Number (your Twilio number)
- Environment (optional: Development, Staging, Production)

### 2. Add SMS Fields to Notification UI Forms (Manual Setup)

After application startup, the installer (`TwilioNotificationExtensionInstaller`) automatically adds four custom fields to the `CMS_NotificationEmail` database class:

1. **NotificationChannelType** (Text, 50) - Channel selection: "Email", "SMS", or "Both"
2. **NotificationSmsConfigurationID** (Integer) - References SMS configuration (Twilio account to use)
3. **NotificationPhoneField** (Text, 200) - Phone placeholder property name
4. **NotificationSmsContent** (LongText) - The SMS message content with placeholders

**To make these fields visible in the admin UI:**

1. Open the **Modules** application in admin
2. Select the **Notifications** module
3. Go to the **Classes** tab
4. Select **Notification email** class
5. Switch to **UI forms** tab
6. Edit the **Properties** UI form:
   - Add `NotificationChannelType` field
   - Add `NotificationSmsConfigurationID` field (use Object selector component)
   - Add `NotificationPhoneField` field
7. Edit the **Content** UI form:
   - Add `NotificationSmsContent` field (use Text area component)
8. **Save** both UI forms

After this one-time setup, SMS settings will appear:

- **Properties tab**: `/admin/notifications/list/{notification-id}/properties` - Channel type, SMS config, phone field
- **Content tab**: `/admin/notifications/list/{notification-id}/content` - SMS message content

**Example Configuration:**

**Properties Tab:**

- Channel type: `Both`
- SMS configuration: `Production SMS` (select from dropdown)
- Phone field: `Phone`

**Content Tab:**

- Email content: [Your email HTML]
- SMS content: `Hello {FirstName}, your verification code is {VerificationCode}. Expires in {ExpiryMinutes} mins.`

The system will then send both email and SMS when this notification is triggered, using the selected Twilio configuration.

## Phone Field Selection Guide

The "Phone field" must match a property name in your notification's placeholder class.

**Example Placeholder Class:**

```csharp
using Baseline.Core.Admin.Sms.InfoClasses;

// Configure SMS for booking verification code notification
var bookingVerificationSms = new NotificationChannelSettingsInfo
{
    NotificationEmailCodeName = "BookingVerificationCode",
    ChannelType = NotificationChannelType.Both,  // Email + SMS
    PhoneFieldName = "Phone",  // Must match placeholder property name
    SmsTemplate = "Hello {FirstName}! Your Cheval Royal booking verification code is {VerificationCode}. It expires in {ExpiryMinutes} minutes.",
    IsEnabled = true
};

// Save the configuration
var settingsProvider = Service.Resolve<IInfoProvider<NotificationChannelSettingsInfo>>();
settingsProvider.Set(bookingVerificationSms);
```

## Phone Number Field Selection

The **Phone Number Field** setting is crucial for SMS delivery. This field must match a property name in the notification's placeholder class.

### How It Works

Each notification email in Kentico has a corresponding placeholder class that defines available macro fields. For example:

```csharp
public class BookingConfirmationPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "BookingConfirmation";

    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }  // This is the phone field!
    // ... other properties
}
```

In this case, you would enter **"Phone"** (without quotes) as the Phone Number Field value in the admin UI.

### Common Phone Field Names

- `Phone`
- `PhoneNumber`
- `MobilePhone`
- `ContactPhone`

### Finding the Correct Field Name

1. Check the notification placeholder class in your codebase
2. Look for properties registered in `NotificationEmailPlaceholderConfigurationStore`
3. The property must contain a valid phone number in international format (e.g., +14155551234)

## SMS Template Syntax

SMS templates support the same placeholder syntax as email notifications:

```
Hello {FirstName}, your verification code is {VerificationCode}. It expires in {ExpiryMinutes} minutes.
```

**Important**: SMS messages have a 1600 character limit. Keep your templates concise.

## Example: Booking Verification SMS

**Notification**: BookingVerificationCode  
**Phone Number Field**: `Phone` (or whatever property contains the phone)  
**SMS Template**:

```
Hello {FirstName}! Your Cheval Royal booking verification code is {VerificationCode}.
It expires in {ExpiryMinutes} minutes.
```

## Developer Guide

### Registering Notification Placeholders with Phone Fields

When creating custom notifications, ensure your placeholder class includes a phone field:

```csharp
public class MyCustomPlaceholders : INotificationEmailPlaceholdersByCodeName
{
    public string NotificationEmailName => "MyCustomNotification";

    [PlaceholderRequired]
    public string Phone { get; set; } = string.Empty;  // Add this!

    // ... other properties
}
```

Register it in your module:

```csharp
protected override void OnInit(ModuleInitParameters parameters)
{
    base.OnInit(parameters);
    NotificationEmailPlaceholderConfigurationStore.Instance.TryAdd(new MyCustomPlaceholders());
}
```

### Sending Notifications with SMS

The SMS system automatically hooks into Kentico's notification system. When you send a notification with both email and SMS configured:

1. The system retrieves the notification channel settings
2. Checks if SMS is enabled for that notification
3. Extracts the phone number from the specified field in the placeholders
4. Sends the SMS using the configured Twilio account
5. Replaces placeholder tokens in the SMS template

No code changes are needed in your notification sending logic!

## Troubleshooting

**SMS not sending:**

- Verify Phone Number Field matches the exact property name in the placeholder class
- Check that the phone number is in international format (+1...)
- Ensure the Twilio configuration is enabled
- Check application logs for errors

**Phone field not found:**

- Double-check the placeholder class property name (case-sensitive!)
- Ensure the placeholder class is registered in `NotificationEmailPlaceholderConfigurationStore`

## Database Schema

The module creates two tables:

- `Baseline_TwilioSmsSettings`: Stores Twilio account configurations
- `Baseline_NotificationChannelSettings`: Maps notifications to SMS channels and phone fields
