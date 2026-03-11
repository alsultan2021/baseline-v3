using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionEditPage),
    name: "Edit Buy X Get Y Promotion",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Buy X Get Y Promotions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class BuyXGetYPromotionEditPage : ModelEditPage<BuyXGetYPromotionViewModel>
{
    private BuyXGetYPromotionViewModel? model = null;

    public BuyXGetYPromotionEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override BuyXGetYPromotionViewModel Model
    {
        get
        {
            if (model == null)
            {
                var promotionInfo = Provider<PromotionInfo>.Instance.Get()
                    .WhereEquals(nameof(PromotionInfo.PromotionID), ObjectId)
                    .FirstOrDefault();

                if (promotionInfo != null)
                {
                    model = new BuyXGetYPromotionViewModel
                    {
                        PromotionID = promotionInfo.PromotionID,
                        PromotionGuid = promotionInfo.PromotionGuid,
                        PromotionName = promotionInfo.PromotionName,
                        PromotionDisplayName = promotionInfo.PromotionDisplayName,
                        PromotionDescription = promotionInfo.PromotionDescription,
                        BuyQuantity = promotionInfo.PromotionBuyQuantity,
                        GetQuantity = promotionInfo.PromotionGetQuantity,
                        GetDiscountPercentage = promotionInfo.PromotionGetDiscountPercentage,
                        TargetCategories = promotionInfo.PromotionTargetCategories,
                        TargetProducts = promotionInfo.PromotionTargetProducts,
                        PromotionActiveFrom = promotionInfo.PromotionActiveFrom,
                        PromotionActiveTo = promotionInfo.PromotionActiveTo,
                        PromotionEnabled = promotionInfo.PromotionEnabled,
                        PromotionOrder = promotionInfo.PromotionOrder
                    };
                }
                else
                {
                    model = new BuyXGetYPromotionViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(BuyXGetYPromotionViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var promotionInfo = Provider<PromotionInfo>.Instance.Get()
                .WhereEquals(nameof(PromotionInfo.PromotionID), ObjectId)
                .FirstOrDefault();

            if (promotionInfo == null)
            {
                return GetErrorResponse("Buy X Get Y promotion not found.");
            }

            promotionInfo.PromotionName = model.PromotionName;
            promotionInfo.PromotionDisplayName = model.PromotionDisplayName;
            promotionInfo.PromotionDescription = model.PromotionDescription;
            promotionInfo.PromotionType = (int)PromotionTypeEnum.BuyXGetY;
            promotionInfo.PromotionBuyQuantity = model.BuyQuantity;
            promotionInfo.PromotionGetQuantity = model.GetQuantity;
            promotionInfo.PromotionGetDiscountPercentage = model.GetDiscountPercentage;
            promotionInfo.PromotionTargetCategories = model.TargetCategories;
            promotionInfo.PromotionTargetProducts = model.TargetProducts;
            promotionInfo.PromotionActiveFrom = model.PromotionActiveFrom;
            promotionInfo.PromotionActiveTo = model.PromotionActiveTo;
            promotionInfo.PromotionEnabled = model.PromotionEnabled;
            promotionInfo.PromotionOrder = model.PromotionOrder;

            await Task.Run(() => Provider<PromotionInfo>.Instance.Set(promotionInfo));

            return GetSuccessResponse("Buy X Get Y promotion updated successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error updating Buy X Get Y promotion: {ex.Message}");
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
