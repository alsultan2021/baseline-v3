using CMS.DataEngine;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.Models;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Base class for notification channel settings edit pages.
/// </summary>
public abstract class NotificationChannelSettingsBaseEdit(
    IFormItemCollectionProvider formItemCollectionProvider,
    IFormDataBinder formDataBinder,
    IInfoProvider<NotificationChannelSettingsInfo> settingsProvider)
    : ModelEditPage<NotificationChannelSettingsModel>(formItemCollectionProvider, formDataBinder)
{
    /// <summary>
    /// The notification channel settings provider.
    /// </summary>
    protected readonly IInfoProvider<NotificationChannelSettingsInfo> SettingsProvider = settingsProvider;

    /// <summary>
    /// Validates model and processes the save operation.
    /// </summary>
    protected ModificationResult ValidateAndProcess(NotificationChannelSettingsModel model, bool updateExisting = false)
    {
        if (string.IsNullOrWhiteSpace(model.NotificationEmailCodeName))
        {
            return new ModificationResult(ModificationResultState.Failure, "Notification email is required.");
        }

        if (string.IsNullOrWhiteSpace(model.ChannelType))
        {
            return new ModificationResult(ModificationResultState.Failure, "Channel type is required.");
        }

        // If SMS is selected, require SMS template and phone field
        if (model.ChannelType is NotificationChannelType.SMS or NotificationChannelType.Both)
        {
            if (string.IsNullOrWhiteSpace(model.SmsTemplate))
            {
                return new ModificationResult(ModificationResultState.Failure, "SMS template is required when SMS channel is selected.");
            }
            
            if (string.IsNullOrWhiteSpace(model.PhoneFieldName))
            {
                return new ModificationResult(ModificationResultState.Failure, "Phone number field is required when SMS channel is selected.");
            }
        }

        // Check for duplicate notification code name (for new entries only)
        if (!updateExisting)
        {
            var existing = SettingsProvider.Get()
                .WhereEquals(nameof(NotificationChannelSettingsInfo.NotificationEmailCodeName), model.NotificationEmailCodeName)
                .FirstOrDefault();

            if (existing != null)
            {
                return new ModificationResult(ModificationResultState.Failure,
                    $"Channel settings already exist for notification '{model.NotificationEmailCodeName}'.");
            }
        }

        var info = updateExisting
            ? SettingsProvider.Get()
                .WhereEquals(nameof(NotificationChannelSettingsInfo.NotificationChannelSettingsID), model.Id)
                .FirstOrDefault()
                ?? new NotificationChannelSettingsInfo()
            : new NotificationChannelSettingsInfo();

        info.NotificationEmailCodeName = model.NotificationEmailCodeName;
        info.ChannelType = model.ChannelType;
        info.SmsTemplate = model.SmsTemplate;
        info.PhoneFieldName = model.PhoneFieldName;
        info.IsEnabled = model.IsEnabled;
        info.NotificationChannelSettingsLastModified = DateTime.Now;

        if (!updateExisting)
        {
            info.NotificationChannelSettingsGuid = Guid.NewGuid();
        }

        SettingsProvider.Set(info);

        return new ModificationResult(ModificationResultState.Success);
    }
}
