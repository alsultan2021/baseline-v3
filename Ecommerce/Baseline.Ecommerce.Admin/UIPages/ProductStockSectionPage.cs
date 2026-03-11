using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.Services;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockSectionPage),
    name: "Product Stock",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Section page for individual Product Stock editing.
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ProductStockSectionPage : EditSectionPage<ProductStockInfo>
{
    private readonly IProductMetadataRetriever productMetadataRetriever;

    public ProductStockSectionPage(IProductMetadataRetriever productMetadataRetriever)
    {
        this.productMetadataRetriever = productMetadataRetriever;
    }

    protected override async Task<string> GetObjectDisplayName(BaseInfo infoObject)
    {
        if (infoObject is ProductStockInfo stock)
        {
            var displayName = await productMetadataRetriever.GetProductDisplayNameAsync(stock);
            if (!string.IsNullOrEmpty(displayName))
            {
                return $"Stock for {displayName}";
            }

            var productGuid = stock.GetProductGuid();
            return $"Stock for Product {productGuid?.ToString("N")[..8] ?? "(none)"}";
        }

        return "Product Stock";
    }
}
