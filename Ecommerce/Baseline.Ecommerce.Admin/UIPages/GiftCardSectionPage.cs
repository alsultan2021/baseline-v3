using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardApplication),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardSectionPage),
    name: "Gift Card",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for individual Gift Card editing.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class GiftCardSectionPage : EditSectionPage<GiftCardInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is GiftCardInfo giftCard)
        {
            return await Task.FromResult(giftCard.GiftCardCode ?? "Gift Card");
        }

        return await Task.FromResult("Gift Card");
    }
}
