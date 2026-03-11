using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for Exchange Rate editing navigation.
/// </summary>
public class CurrencyExchangeRateSectionPage : EditSectionPage<CurrencyExchangeRateInfo>
{
    private readonly IInfoProvider<CurrencyInfo> currencyProvider;

    public CurrencyExchangeRateSectionPage(IInfoProvider<CurrencyInfo> currencyProvider)
    {
        this.currencyProvider = currencyProvider;
    }

    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is CurrencyExchangeRateInfo rate)
        {
            var fromCurrency = currencyProvider.Get(rate.ExchangeRateFromCurrencyID);
            var toCurrency = currencyProvider.Get(rate.ExchangeRateToCurrencyID);
            var fromCode = fromCurrency?.CurrencyCode ?? "?";
            var toCode = toCurrency?.CurrencyCode ?? "?";
            return await Task.FromResult($"{fromCode} → {toCode}");
        }

        return await Task.FromResult("Exchange Rate");
    }
}
