using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(PromotionsApplication),
    slug: "buyxgety-promotions",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionListPage),
    name: "Buy X Get Y Promotions",
    templateName: TemplateNames.LISTING,
    order: 220,
    Icon = Icons.Gift)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Buy X Get Y Promotions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class BuyXGetYPromotionListPage : ListingPage
{
    protected override string ObjectType => PromotionInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.Callouts ??= [];

        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.Promotion") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Buy X Get Y Promotions Not Yet Available",
                Content = "The Promotions data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.QueryModifiers.AddModifier((query, _) =>
            query.WhereEquals(nameof(PromotionInfo.PromotionType), (int)PromotionTypeEnum.BuyXGetY));

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<BuyXGetYPromotionCreatePage>("Create Buy X Get Y promotion");
        PageConfiguration.AddEditRowAction<BuyXGetYPromotionEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(PromotionInfo.PromotionDisplayName), "Name", searchable: true, maxWidth: 20)
            .AddColumn(nameof(PromotionInfo.PromotionBuyQuantity), "Buy Qty", maxWidth: 8)
            .AddColumn(nameof(PromotionInfo.PromotionGetQuantity), "Get Qty", maxWidth: 8)
            .AddColumn(nameof(PromotionInfo.PromotionGetDiscountPercentage), "Discount %", formatter: (val, _) =>
                (decimal)val >= 100m ? "Free" : $"{val}%", maxWidth: 10)
            .AddColumn(nameof(PromotionInfo.PromotionActiveFrom), "Active From", maxWidth: 12)
            .AddColumn(nameof(PromotionInfo.PromotionActiveTo), "Active To", maxWidth: 12)
            .AddColumn(nameof(PromotionInfo.PromotionEnabled), "Enabled", maxWidth: 8);
    }
}
