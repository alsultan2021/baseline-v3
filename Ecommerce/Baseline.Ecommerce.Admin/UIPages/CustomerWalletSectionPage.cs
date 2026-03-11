using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerWalletListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CustomerWalletSectionPage),
    name: "Wallet Details",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: 0)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for customer wallet details navigation.
/// Provides tabs for wallet details and transaction history within customer context.
/// </summary>
public class CustomerWalletSectionPage : EditSectionPage<WalletInfo>
{
    /// <summary>
    /// Customer ID from the parent path.
    /// </summary>
    [PageParameter(typeof(IntPageModelBinder), typeof(CustomerEditSection))]
    public int CustomerId { get; set; }

    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is WalletInfo wallet)
        {
            return await Task.FromResult($"Wallet {wallet.WalletID} - {wallet.WalletType}");
        }

        return await Task.FromResult("Wallet");
    }
}
