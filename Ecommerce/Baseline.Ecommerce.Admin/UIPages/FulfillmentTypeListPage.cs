using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CommerceConfigurationApplication),
    slug: "fulfillment-types",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeListPage),
    name: "Fulfillment Types",
    templateName: TemplateNames.LISTING,
    order: 120,
    Icon = Icons.Box)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Fulfillment Types.
/// Allows administrators to manage fulfillment types that define checkout behavior.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class FulfillmentTypeListPage : ListingPage
{
    protected override string ObjectType => FulfillmentTypeInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists in the database
        var dataClassInfo = DataClassInfoProvider.GetDataClassInfo("Baseline.FulfillmentType");
        if (dataClassInfo is null)
        {
            // Data class not installed yet - show informative message
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Setup Required",
                Content = "The Fulfillment Type data class has not been installed. Please run the Baseline.Ecommerce module installer or CI restore to create the required database tables.",
                Type = CalloutType.QuickTip,
                ContentAsHtml = false
            });
            return;
        }

        // Configure page actions and columns
        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<FulfillmentTypeCreatePage>("Add fulfillment type");
        PageConfiguration.AddEditRowAction<FulfillmentTypeEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(FulfillmentTypeInfo.FulfillmentTypeDisplayName), "Display Name", searchable: true, maxWidth: 25)
            .AddColumn(nameof(FulfillmentTypeInfo.FulfillmentTypeCodeName), "Code Name", searchable: true, maxWidth: 15)
            .AddColumn(nameof(FulfillmentTypeInfo.FulfillmentTypeRequiresShipping), "Requires Shipping", maxWidth: 15)
            .AddColumn(nameof(FulfillmentTypeInfo.FulfillmentTypeSupportsDeliveryOptions), "Delivery Options", maxWidth: 15)
            .AddColumn(nameof(FulfillmentTypeInfo.FulfillmentTypeIsEnabled), "Enabled", maxWidth: 10);
    }
}
