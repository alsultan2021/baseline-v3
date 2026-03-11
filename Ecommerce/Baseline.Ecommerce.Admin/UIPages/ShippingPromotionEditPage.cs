using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.ShippingPromotionSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ShippingPromotionEditPage),
    name: "Edit Shipping Promotion",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Shipping Promotions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ShippingPromotionEditPage : ModelEditPage<ShippingPromotionViewModel>
{
    private ShippingPromotionViewModel? model = null;

    public ShippingPromotionEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override ShippingPromotionViewModel Model
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
                    model = new ShippingPromotionViewModel
                    {
                        PromotionID = promotionInfo.PromotionID,
                        PromotionGuid = promotionInfo.PromotionGuid,
                        PromotionName = promotionInfo.PromotionName,
                        PromotionDisplayName = promotionInfo.PromotionDisplayName,
                        PromotionDescription = promotionInfo.PromotionDescription,
                        ShippingDiscountType = promotionInfo.PromotionShippingDiscountType.ToString(),
                        PromotionDiscountValue = promotionInfo.PromotionDiscountValue,
                        MaxShippingDiscount = promotionInfo.PromotionMaxShippingDiscount,
                        MinimumRequirementType = promotionInfo.PromotionMinimumRequirementType.ToString(),
                        MinimumRequirementValue = promotionInfo.PromotionMinimumRequirementValue,
                        TargetShippingZones = promotionInfo.PromotionTargetShippingZones,
                        TargetCategories = promotionInfo.PromotionTargetCategories,
                        PromotionActiveFrom = promotionInfo.PromotionActiveFrom,
                        PromotionActiveTo = promotionInfo.PromotionActiveTo,
                        PromotionEnabled = promotionInfo.PromotionEnabled,
                        PromotionOrder = promotionInfo.PromotionOrder
                    };
                }
                else
                {
                    model = new ShippingPromotionViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(ShippingPromotionViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var promotionInfo = Provider<PromotionInfo>.Instance.Get()
                .WhereEquals(nameof(PromotionInfo.PromotionID), ObjectId)
                .FirstOrDefault();

            if (promotionInfo == null)
            {
                return GetErrorResponse("Shipping promotion not found.");
            }

            promotionInfo.PromotionName = model.PromotionName;
            promotionInfo.PromotionDisplayName = model.PromotionDisplayName;
            promotionInfo.PromotionDescription = model.PromotionDescription;
            promotionInfo.PromotionType = (int)PromotionTypeEnum.Shipping;
            promotionInfo.PromotionShippingDiscountType = int.TryParse(model.ShippingDiscountType, out var sdt) ? sdt : 0;
            promotionInfo.PromotionDiscountValue = model.PromotionDiscountValue;
            promotionInfo.PromotionMaxShippingDiscount = model.MaxShippingDiscount;
            promotionInfo.PromotionMinimumRequirementType = int.TryParse(model.MinimumRequirementType, out var mrt) ? mrt : 0;
            promotionInfo.PromotionMinimumRequirementValue = model.MinimumRequirementValue;
            promotionInfo.PromotionTargetShippingZones = model.TargetShippingZones;
            promotionInfo.PromotionTargetCategories = model.TargetCategories;
            promotionInfo.PromotionActiveFrom = model.PromotionActiveFrom;
            promotionInfo.PromotionActiveTo = model.PromotionActiveTo;
            promotionInfo.PromotionEnabled = model.PromotionEnabled;
            promotionInfo.PromotionOrder = model.PromotionOrder;

            await Task.Run(() => Provider<PromotionInfo>.Instance.Set(promotionInfo));

            return GetSuccessResponse("Shipping promotion updated successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error updating shipping promotion: {ex.Message}");
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
