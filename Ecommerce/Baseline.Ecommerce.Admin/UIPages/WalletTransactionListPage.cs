using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletSectionPage),
    slug: "transactions",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionListPage),
    name: "Transactions",
    templateName: TemplateNames.LISTING,
    order: 10)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for Wallet Transactions.
/// Shows transaction history for a specific wallet.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class WalletTransactionListPage : ListingPage
{
    protected override string ObjectType => WalletTransactionInfo.OBJECT_TYPE;

    [PageParameter(typeof(IntPageModelBinder), typeof(WalletSectionPage))]
    public int ParentObjectId { get; set; }

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        PageConfiguration.Callouts ??= [];

        // Check if the data class exists
        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.WalletTransaction") != null;

        if (!dataClassExists)
        {
            PageConfiguration.Callouts.Add(new CalloutConfiguration
            {
                Headline = "Transactions Not Yet Available",
                Content = "The WalletTransaction data class has not been installed yet. Please restart the application to complete the installation.",
                Type = CalloutType.QuickTip,
                Placement = CalloutPlacement.OnDesk
            });

            return;
        }

        // Filter transactions to only show those belonging to the current wallet
        PageConfiguration.QueryModifiers.AddModifier(query =>
            query.WhereEquals(nameof(WalletTransactionInfo.TransactionWalletID), ParentObjectId));

        PageConfiguration.HeaderActions.AddLink<WalletTransactionCreatePage>(
            "Add transaction",
            parameters: new PageParameterValues
            {
                { typeof(WalletSectionPage), ParentObjectId }
            });
        PageConfiguration.AddEditRowAction<WalletTransactionEditPage>(
            parameters: new PageParameterValues
            {
                { typeof(WalletSectionPage), ParentObjectId }
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(WalletTransactionInfo.TransactionType), "Type", searchable: true, maxWidth: 12)
            .AddColumn(nameof(WalletTransactionInfo.TransactionAmount), "Amount", maxWidth: 12)
            .AddColumn(nameof(WalletTransactionInfo.TransactionBalanceAfter), "Balance After", maxWidth: 12)
            .AddColumn(nameof(WalletTransactionInfo.TransactionStatus), "Status", maxWidth: 10)
            .AddColumn(nameof(WalletTransactionInfo.TransactionReference), "Reference", searchable: true, maxWidth: 15)
            .AddColumn(nameof(WalletTransactionInfo.TransactionDescription), "Description", maxWidth: 20)
            .AddColumn(nameof(WalletTransactionInfo.TransactionCreatedWhen), "Created", maxWidth: 15);
    }
}
