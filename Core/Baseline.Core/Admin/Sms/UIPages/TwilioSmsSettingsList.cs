using Kentico.Xperience.Admin.Base;

using Baseline.Core.Admin.Sms.InfoClasses;
using Baseline.Core.Admin.Sms.UIPages;

[assembly: UIPage(
    parentType: typeof(TwilioSmsApplication),
    slug: "settings",
    uiPageType: typeof(TwilioSmsSettingsList),
    name: "SMS Configurations",
    templateName: TemplateNames.LISTING,
    order: 1)]

namespace Baseline.Core.Admin.Sms.UIPages;

/// <summary>
/// Lists all Twilio SMS configurations.
/// </summary>
public class TwilioSmsSettingsList : ListingPage
{
    /// <inheritdoc />
    protected override string ObjectType => TwilioSmsSettingsInfo.OBJECT_TYPE;

    /// <inheritdoc />
    public override Task ConfigurePage()
    {
        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsDisplayName), "Name")
            .AddColumn(nameof(TwilioSmsSettingsInfo.Environment), "Environment")
            .AddColumn(nameof(TwilioSmsSettingsInfo.FromPhoneNumber), "From Number")
            .AddColumn(nameof(TwilioSmsSettingsInfo.IsEnabled), "Enabled")
            .AddColumn(nameof(TwilioSmsSettingsInfo.TwilioSmsSettingsLastModified), "Last Modified");

        PageConfiguration.HeaderActions.AddLink<TwilioSmsSettingsCreate>("Add Configuration");
        PageConfiguration.AddEditRowAction<TwilioSmsSettingsEdit>();
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));

        return base.ConfigurePage();
    }

    /// <summary>
    /// Deletes a Twilio SMS settings entry.
    /// </summary>
    [PageCommand]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);
}
