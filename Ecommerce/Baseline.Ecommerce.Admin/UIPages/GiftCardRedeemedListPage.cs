using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardApplication),
    slug: "redeemed",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardRedeemedListPage),
    name: "Redeemed Gift Cards",
    templateName: TemplateNames.LISTING,
    order: 200)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Redeemed/Expired/Cancelled Gift Cards.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class GiftCardRedeemedListPage : ListingPage
{
    private readonly IInfoProvider<CurrencyInfo> _currencyProvider;

    public GiftCardRedeemedListPage(IInfoProvider<CurrencyInfo> currencyProvider)
    {
        _currencyProvider = currencyProvider;
    }

    protected override string ObjectType => GiftCardInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.GiftCard") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts ??= [];
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Gift Cards Not Yet Available",
                Content = "The Gift Cards data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });
            return;
        }

        // Filter to show only fully redeemed, expired, and cancelled gift cards
        PageConfiguration.QueryModifiers.AddModifier(query =>
            query.Where(w => w
                .WhereEquals(nameof(GiftCardInfo.GiftCardStatus), GiftCardStatuses.FullyRedeemed)
                .Or()
                .WhereEquals(nameof(GiftCardInfo.GiftCardStatus), GiftCardStatuses.Expired)
                .Or()
                .WhereEquals(nameof(GiftCardInfo.GiftCardStatus), GiftCardStatuses.Cancelled)));

        // Get currencies for display formatting
        var currencies = await _currencyProvider.Get().GetEnumerableTypedResultAsync();
        var currencyDict = currencies.ToDictionary(c => c.CurrencyID, c => c);

        PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
        PageConfiguration.AddEditRowAction<GiftCardEditPage>();

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(GiftCardInfo.GiftCardCode), "Code", searchable: true, maxWidth: 15)
            .AddColumn(nameof(GiftCardInfo.GiftCardInitialAmount), "Initial Amount", formatter: (val, row) =>
            {
                var amount = (decimal)val;
                var currencyId = row[nameof(GiftCardInfo.GiftCardCurrencyID)];
                if (currencyId != null && currencyDict.TryGetValue((int)currencyId, out var currency))
                {
                    return currency.FormatAmount(amount);
                }
                return amount.ToString("N2");
            }, maxWidth: 12)
            .AddColumn(nameof(GiftCardInfo.GiftCardRemainingBalance), "Balance", formatter: (val, row) =>
            {
                var amount = (decimal)val;
                var currencyId = row[nameof(GiftCardInfo.GiftCardCurrencyID)];
                if (currencyId != null && currencyDict.TryGetValue((int)currencyId, out var currency))
                {
                    return currency.FormatAmount(amount);
                }
                return amount.ToString("N2");
            }, maxWidth: 12)
            .AddColumn(nameof(GiftCardInfo.GiftCardStatus), "Status", maxWidth: 12)
            .AddColumn(nameof(GiftCardInfo.GiftCardExpiresAt), "Expires", maxWidth: 12)
            .AddColumn(nameof(GiftCardInfo.GiftCardCreatedWhen), "Created", maxWidth: 12);
    }
}
