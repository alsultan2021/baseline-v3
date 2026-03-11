using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CommerceConfigurationApplication),
    slug: "product-stock",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockListPage),
    name: "Product Stock",
    templateName: TemplateNames.LISTING,
    order: 300,
    Icon = Icons.Box)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Product Stock.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ProductStockListPage : ListingPage
{
    protected override string ObjectType => ProductStockInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.ProductStock") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Product Stock Not Yet Available",
                Content = "The Product Stock data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<ProductStockCreatePage>("Add stock record");
        PageConfiguration.AddEditRowAction<ProductStockEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(ProductStockInfo.ProductStockProduct), "Product", maxWidth: 20)
            .AddColumn(nameof(ProductStockInfo.ProductStockAvailableQuantity), "Available", maxWidth: 12)
            .AddColumn(nameof(ProductStockInfo.ProductStockReservedQuantity), "Reserved", maxWidth: 12)
            .AddColumn(nameof(ProductStockInfo.ProductStockMinimumThreshold), "Min Threshold", maxWidth: 12)
            .AddColumn(nameof(ProductStockInfo.ProductStockAllowBackorders), "Backorders", maxWidth: 10)
            .AddColumn(nameof(ProductStockInfo.ProductStockTrackingEnabled), "Tracking", maxWidth: 10)
            .AddColumn(nameof(ProductStockInfo.ProductStockLastModified), "Last Modified", maxWidth: 15);
    }
}
