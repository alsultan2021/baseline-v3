using Kentico.Xperience.Admin.Base;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.UIPages;

// Notification channel settings are managed programmatically or via code
// No separate admin UI - settings are stored per notification email code name

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Lists notification channel settings (Email/SMS/Both per notification).
/// </summary>
public class NotificationChannelSettingsList : ListingPage
{
    /// <inheritdoc />
    protected override string ObjectType => NotificationChannelSettingsInfo.OBJECT_TYPE;

    /// <inheritdoc />
    public override Task ConfigurePage()
    {
        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(NotificationChannelSettingsInfo.NotificationEmailCodeName), "Notification")
            .AddColumn(nameof(NotificationChannelSettingsInfo.ChannelType), "Channel")
            .AddColumn(nameof(NotificationChannelSettingsInfo.PhoneFieldName), "Phone Field")
            .AddColumn(nameof(NotificationChannelSettingsInfo.IsEnabled), "Enabled")
            .AddColumn(nameof(NotificationChannelSettingsInfo.NotificationChannelSettingsLastModified), "Last Modified");

        PageConfiguration.HeaderActions.AddLink<NotificationChannelSettingsCreate>("Add Notification Channel");
        PageConfiguration.AddEditRowAction<NotificationChannelSettingsEdit>();
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));

        return base.ConfigurePage();
    }

    /// <summary>
    /// Deletes a notification channel settings entry.
    /// </summary>
    [PageCommand]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);
}
