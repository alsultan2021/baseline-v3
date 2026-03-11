using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ProductStockEditPage),
    name: "Edit Product Stock",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing Product Stock records.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ProductStockEditPage : ModelEditPage<ProductStockViewModel>
{
    private ProductStockViewModel? model = null;

    public ProductStockEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override ProductStockViewModel Model
    {
        get
        {
            if (model == null)
            {
                var stockInfo = Provider<ProductStockInfo>.Instance.Get()
                    .WhereEquals(nameof(ProductStockInfo.ProductStockID), ObjectId)
                    .FirstOrDefault();

                if (stockInfo != null)
                {
                    model = new ProductStockViewModel
                    {
                        ProductStockID = stockInfo.ProductStockID,
                        ProductStockGuid = stockInfo.ProductStockGuid,
                        ProductStockProduct = stockInfo.ProductStockProduct,
                        ProductStockAvailableQuantity = stockInfo.ProductStockAvailableQuantity,
                        ProductStockReservedQuantity = stockInfo.ProductStockReservedQuantity,
                        ProductStockMinimumThreshold = stockInfo.ProductStockMinimumThreshold,
                        ProductStockAllowBackorders = stockInfo.ProductStockAllowBackorders,
                        ProductStockTrackingEnabled = stockInfo.ProductStockTrackingEnabled
                    };
                }
                else
                {
                    model = new ProductStockViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(ProductStockViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var stockInfo = Provider<ProductStockInfo>.Instance.Get()
                .WhereEquals(nameof(ProductStockInfo.ProductStockID), ObjectId)
                .FirstOrDefault();

            if (stockInfo == null)
            {
                return GetErrorResponse("Product stock record not found.");
            }

            stockInfo.ProductStockProduct = model.ProductStockProduct;
            stockInfo.ProductStockAvailableQuantity = model.ProductStockAvailableQuantity;
            stockInfo.ProductStockReservedQuantity = model.ProductStockReservedQuantity;
            stockInfo.ProductStockMinimumThreshold = model.ProductStockMinimumThreshold;
            stockInfo.ProductStockAllowBackorders = model.ProductStockAllowBackorders;
            stockInfo.ProductStockTrackingEnabled = model.ProductStockTrackingEnabled;

            // Offload to thread pool — Provider.Set() is sync-only
            await Task.Run(() => Provider<ProductStockInfo>.Instance.Set(stockInfo));

            return GetSuccessResponse("Product stock updated successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error updating product stock: {ex.Message}");
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
