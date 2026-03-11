using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletSectionPage),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for Wallet editing navigation.
/// Provides tabs for wallet details and transaction history.
/// </summary>
public class WalletSectionPage : EditSectionPage<WalletInfo>
{
    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is WalletInfo wallet)
        {
            return await Task.FromResult($"Wallet {wallet.WalletID} - {wallet.WalletType}");
        }

        return await Task.FromResult("Wallet");
    }
}
