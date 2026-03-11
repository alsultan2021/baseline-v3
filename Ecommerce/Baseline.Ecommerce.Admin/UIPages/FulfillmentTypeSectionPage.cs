using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for Fulfillment Type editing navigation.
/// </summary>
public class FulfillmentTypeSectionPage : EditSectionPage<FulfillmentTypeInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is FulfillmentTypeInfo fulfillmentType)
        {
            return await Task.FromResult(fulfillmentType.FulfillmentTypeDisplayName);
        }

        return await Task.FromResult("Fulfillment Type");
    }
}
