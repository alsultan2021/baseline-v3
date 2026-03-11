using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.ShippingPromotionListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ShippingPromotionSectionPage),
    name: "Shipping Promotion",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for individual Shipping Promotion editing.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ShippingPromotionSectionPage : EditSectionPage<PromotionInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is PromotionInfo promotion)
        {
            return await Task.FromResult(promotion.PromotionDisplayName ?? "Shipping Promotion");
        }

        return await Task.FromResult("Shipping Promotion");
    }
}
