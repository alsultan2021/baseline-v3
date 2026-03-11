using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CommerceConfigurationApplication),
    slug: "tax-classes",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassListPage),
    name: "Tax Classes",
    templateName: TemplateNames.LISTING,
    order: 100,
    Icon = Icons.CogwheelSquare)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Tax Classes.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class TaxClassListPage : ListingPage
{
    protected override string ObjectType => TaxClassInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.TaxClass") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Tax Classes Not Yet Available",
                Content = "The Tax Classes data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<TaxClassCreatePage>("Create tax class");
        PageConfiguration.AddEditRowAction<TaxClassEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(TaxClassInfo.TaxClassDisplayName), "Display Name", searchable: true, maxWidth: 25)
            .AddColumn(nameof(TaxClassInfo.TaxClassName), "Code Name", searchable: true, maxWidth: 15)
            .AddColumn(nameof(TaxClassInfo.TaxClassDefaultRate), "Rate (%)", maxWidth: 10)
            .AddColumn(nameof(TaxClassInfo.TaxClassIsDefault), "Default", maxWidth: 10)
            .AddColumn(nameof(TaxClassInfo.TaxClassIsExempt), "Exempt", maxWidth: 10)
            .AddColumn(nameof(TaxClassInfo.TaxClassEnabled), "Enabled", maxWidth: 10)
            .AddColumn(nameof(TaxClassInfo.TaxClassOrder), "Order", maxWidth: 10);
    }
}