using CMS.Membership;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerWalletSectionPage),
    slug: "transactions",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerWalletTransactionsPage),
    name: "Transactions",
    templateName: TemplateNames.LISTING,
    order: 200)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin listing page for wallet transactions within customer context.
/// Shows transaction history for a specific wallet.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class CustomerWalletTransactionsPage : ListingPage
{
    /// <summary>
    /// Customer ID from the parent path.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder), typeof(CustomerEditSection))]
    public int CustomerId { get; set; }

    /// <summary>
    /// Wallet ID from the URL.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder), typeof(CustomerWalletSectionPage))]
    public int WalletId { get; set; }

    protected override string ObjectType => WalletTransactionInfo.OBJECT_TYPE;

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

        // Filter transactions by wallet ID
        PageConfiguration.QueryModifiers.AddModifier((query, _) =>
            query.WhereEquals(nameof(WalletTransactionInfo.TransactionWalletID), WalletId)
                 .OrderByDescending(nameof(WalletTransactionInfo.TransactionCreatedWhen)));

        // Configure columns
        PageConfiguration.ColumnConfigurations
            .AddColumn(nameof(WalletTransactionInfo.TransactionType), "Type", searchable: true, maxWidth: 12)
            .AddColumn(nameof(WalletTransactionInfo.TransactionAmount), "Amount", maxWidth: 12)
            .AddColumn(nameof(WalletTransactionInfo.TransactionBalanceAfter), "Balance After", maxWidth: 12)
            .AddColumn(nameof(WalletTransactionInfo.TransactionStatus), "Status", maxWidth: 10)
            .AddColumn(nameof(WalletTransactionInfo.TransactionDescription), "Description", maxWidth: 25)
            .AddColumn(nameof(WalletTransactionInfo.TransactionCreatedWhen), "Date", maxWidth: 15);
    }
}
