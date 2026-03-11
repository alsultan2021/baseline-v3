using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionSectionPage),
    name: "Buy X Get Y Promotion",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for individual Buy X Get Y Promotion editing.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class BuyXGetYPromotionSectionPage : EditSectionPage<PromotionInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is PromotionInfo promotion)
        {
            return await Task.FromResult(promotion.PromotionDisplayName ?? "Buy X Get Y Promotion");
        }

        return await Task.FromResult("Buy X Get Y Promotion");
    }
}
