using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for Tax Class editing navigation.
/// </summary>
public class TaxClassSectionPage : EditSectionPage<TaxClassInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is TaxClassInfo taxClass)
        {
            return await Task.FromResult(taxClass.TaxClassDisplayName ?? "Tax Class");
        }

        return await Task.FromResult("Tax Class");
    }
}
