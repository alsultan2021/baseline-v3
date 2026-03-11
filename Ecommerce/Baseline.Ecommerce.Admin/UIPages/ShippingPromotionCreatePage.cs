using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.ShippingPromotionListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.ShippingPromotionCreatePage),
    name: "Create Shipping Promotion",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Shipping Promotions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class ShippingPromotionCreatePage : ModelEditPage<ShippingPromotionViewModel>
{
    private readonly IInfoProvider<PromotionInfo> promotionProvider;
    private ShippingPromotionViewModel? model = null;

    public ShippingPromotionCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<PromotionInfo> promotionProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        this.promotionProvider = promotionProvider;
    }

    protected override ShippingPromotionViewModel Model => model ??= new ShippingPromotionViewModel
    {
        PromotionGuid = Guid.NewGuid(),
        PromotionActiveFrom = DateTime.UtcNow,
        PromotionEnabled = true
    };

    protected override async Task<ICommandResponse> ProcessFormData(ShippingPromotionViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var existing = await promotionProvider
                .Get()
                .WhereEquals(nameof(PromotionInfo.PromotionName), model.PromotionName)
                .GetEnumerableTypedResultAsync();

            if (existing.Any())
            {
                return GetErrorResponse("A promotion with this code name already exists.");
            }

            var promotionInfo = new PromotionInfo
            {
                PromotionGuid = model.PromotionGuid,
                PromotionName = model.PromotionName,
                PromotionDisplayName = model.PromotionDisplayName,
                PromotionDescription = model.PromotionDescription,
                PromotionType = (int)PromotionTypeEnum.Shipping,
                PromotionShippingDiscountType = int.TryParse(model.ShippingDiscountType, out var sdt) ? sdt : 0,
                PromotionDiscountValue = model.PromotionDiscountValue,
                PromotionMaxShippingDiscount = model.MaxShippingDiscount,
                PromotionMinimumRequirementType = int.TryParse(model.MinimumRequirementType, out var mrt) ? mrt : 0,
                PromotionMinimumRequirementValue = model.MinimumRequirementValue,
                PromotionTargetShippingZones = model.TargetShippingZones,
                PromotionTargetCategories = model.TargetCategories,
                PromotionActiveFrom = model.PromotionActiveFrom,
                PromotionActiveTo = model.PromotionActiveTo,
                PromotionEnabled = model.PromotionEnabled,
                PromotionOrder = model.PromotionOrder,
                PromotionCreated = DateTime.UtcNow,
                PromotionRedemptionCount = 0
            };

            await promotionProvider.SetAsync(promotionInfo);

            return GetSuccessResponse("Shipping promotion created successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error creating shipping promotion: {ex.Message}");
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
