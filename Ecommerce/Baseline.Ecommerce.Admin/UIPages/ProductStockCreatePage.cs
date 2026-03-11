using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockCreatePage),
    name: "Add Stock Record",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Product Stock records.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class ProductStockCreatePage : ModelEditPage<ProductStockViewModel>
{
    private readonly IInfoProvider<ProductStockInfo> stockProvider;
    private ProductStockViewModel? model = null;

    public ProductStockCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<ProductStockInfo> stockProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        this.stockProvider = stockProvider;
    }

    protected override ProductStockViewModel Model => model ??= new ProductStockViewModel
    {
        ProductStockGuid = Guid.NewGuid(),
        ProductStockTrackingEnabled = true
    };

    protected override async Task<ICommandResponse> ProcessFormData(ProductStockViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            // Get the product GUID from the selection
            var productGuid = model.ProductStockProduct.FirstOrDefault()?.Identifier;
            if (productGuid == null || productGuid == Guid.Empty)
            {
                return GetErrorResponse("A product must be selected.");
            }

            // Check for duplicate product selection by comparing JSON content
            var allStocks = await stockProvider
                .Get()
                .GetEnumerableTypedResultAsync();

            var existingForProduct = allStocks.FirstOrDefault(s => s.GetProductGuid() == productGuid);
            if (existingForProduct != null)
            {
                return GetErrorResponse("A stock record for this product already exists.");
            }

            var stockInfo = new ProductStockInfo
            {
                ProductStockGuid = model.ProductStockGuid,
                ProductStockProduct = model.ProductStockProduct,
                ProductStockAvailableQuantity = model.ProductStockAvailableQuantity,
                ProductStockReservedQuantity = model.ProductStockReservedQuantity,
                ProductStockMinimumThreshold = model.ProductStockMinimumThreshold,
                ProductStockAllowBackorders = model.ProductStockAllowBackorders,
                ProductStockTrackingEnabled = model.ProductStockTrackingEnabled
            };

            await stockProvider.SetAsync(stockInfo);

            return GetSuccessResponse("Product stock record created successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error creating product stock record: {ex.Message}");
        }
    }

    private ICommandResponse GetSuccessResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
        response.AddSuccessMessage(message);
        return response;
    }

    private ICommandResponse GetErrorResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
        response.AddErrorMessage(message);
        return response;
    }
}
