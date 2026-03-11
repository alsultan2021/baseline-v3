using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CommerceConfigurationApplication),
    slug: "currencies",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyListPage),
    name: "Currencies",
    templateName: TemplateNames.LISTING,
    order: 110,
    Icon = Icons.DollarSign)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Currencies.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class CurrencyListPage : ListingPage
{
    protected override string ObjectType => CurrencyInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.Currency") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Currencies Not Yet Available",
                Content = "The Currencies data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<CurrencyCreatePage>("Add currency");
        PageConfiguration.AddEditRowAction<CurrencyEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(CurrencyInfo.CurrencyCode), "Code", searchable: true, maxWidth: 10)
            .AddColumn(nameof(CurrencyInfo.CurrencyDisplayName), "Name", searchable: true, maxWidth: 25)
            .AddColumn(nameof(CurrencyInfo.CurrencySymbol), "Symbol", maxWidth: 10)
            .AddColumn(nameof(CurrencyInfo.CurrencyDecimalPlaces), "Decimals", maxWidth: 10)
            .AddColumn(nameof(CurrencyInfo.CurrencyEnabled), "Enabled", maxWidth: 10)
            .AddColumn(nameof(CurrencyInfo.CurrencyOrder), "Order", maxWidth: 10);
    }
}
