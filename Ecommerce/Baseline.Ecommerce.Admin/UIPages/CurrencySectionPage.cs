using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencySectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for Currency editing navigation.
/// </summary>
public class CurrencySectionPage : EditSectionPage<CurrencyInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is CurrencyInfo currency)
        {
            return await Task.FromResult($"{currency.CurrencyCode} - {currency.CurrencyDisplayName}");
        }

        return await Task.FromResult("Currency");
    }
}
