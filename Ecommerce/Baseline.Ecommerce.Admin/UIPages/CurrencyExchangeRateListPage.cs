using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(CommerceConfigurationApplication),
    slug: "exchange-rates",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateListPage),
    name: "Exchange Rates",
    templateName: TemplateNames.LISTING,
    order: 120,
    Icon = Icons.Graph)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Currency Exchange Rates.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class CurrencyExchangeRateListPage : ListingPage
{
    private readonly IInfoProvider<CurrencyInfo> currencyProvider;

    public CurrencyExchangeRateListPage(IInfoProvider<CurrencyInfo> currencyProvider)
    {
        this.currencyProvider = currencyProvider;
    }

    protected override string ObjectType => CurrencyExchangeRateInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.CurrencyExchangeRate") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Exchange Rates Not Yet Available",
                Content = "The Exchange Rates data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.HeaderActions.AddLink<CurrencyExchangeRateCreatePage>("Add exchange rate");
        PageConfiguration.AddEditRowAction<CurrencyExchangeRateEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(CurrencyExchangeRateInfo.ExchangeRateFromCurrencyID), "From Currency", searchable: true, maxWidth: 20,
                formatter: (value, _) => GetCurrencyCode(value))
            .AddColumn(nameof(CurrencyExchangeRateInfo.ExchangeRateToCurrencyID), "To Currency", searchable: true, maxWidth: 20,
                formatter: (value, _) => GetCurrencyCode(value))
            .AddColumn(nameof(CurrencyExchangeRateInfo.ExchangeRateValue), "Rate", maxWidth: 15)
            .AddColumn(nameof(CurrencyExchangeRateInfo.ExchangeRateValidFrom), "Valid From", maxWidth: 15)
            .AddColumn(nameof(CurrencyExchangeRateInfo.ExchangeRateValidTo), "Valid To", maxWidth: 15)
            .AddColumn(nameof(CurrencyExchangeRateInfo.ExchangeRateEnabled), "Enabled", maxWidth: 10);
    }

    private string GetCurrencyCode(object? value)
    {
        if (value is int currencyId && currencyId > 0)
        {
            var currency = currencyProvider.Get(currencyId);
            return currency?.CurrencyCode ?? currencyId.ToString();
        }
        return value?.ToString() ?? "-";
    }
}
