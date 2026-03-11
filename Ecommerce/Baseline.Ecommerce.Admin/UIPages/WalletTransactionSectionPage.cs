using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionSectionPage),
    name: "Transaction",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for WalletTransaction editing navigation.
/// </summary>
public class WalletTransactionSectionPage : EditSectionPage<WalletTransactionInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is WalletTransactionInfo transaction)
        {
            return await Task.FromResult($"Transaction {transaction.TransactionID} - {transaction.TransactionType}");
        }

        return await Task.FromResult("Transaction");
    }
}
