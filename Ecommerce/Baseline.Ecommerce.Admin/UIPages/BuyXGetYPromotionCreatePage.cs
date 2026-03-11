using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.BuyXGetYPromotionCreatePage),
    name: "Create Buy X Get Y Promotion",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Buy X Get Y Promotions.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class BuyXGetYPromotionCreatePage : ModelEditPage<BuyXGetYPromotionViewModel>
{
    private readonly IInfoProvider<PromotionInfo> promotionProvider;
    private BuyXGetYPromotionViewModel? model = null;

    public BuyXGetYPromotionCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<PromotionInfo> promotionProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        this.promotionProvider = promotionProvider;
    }

    protected override BuyXGetYPromotionViewModel Model => model ??= new BuyXGetYPromotionViewModel
    {
        PromotionGuid = Guid.NewGuid(),
        PromotionActiveFrom = DateTime.UtcNow,
        PromotionEnabled = true,
        BuyQuantity = 1,
        GetQuantity = 1,
        GetDiscountPercentage = 100m
    };

    protected override async Task<ICommandResponse> ProcessFormData(BuyXGetYPromotionViewModel model, ICollection<IFormItem> formItems)
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
                PromotionType = (int)PromotionTypeEnum.BuyXGetY,
                PromotionBuyQuantity = model.BuyQuantity,
                PromotionGetQuantity = model.GetQuantity,
                PromotionGetDiscountPercentage = model.GetDiscountPercentage,
                PromotionTargetCategories = model.TargetCategories,
                PromotionTargetProducts = model.TargetProducts,
                PromotionActiveFrom = model.PromotionActiveFrom,
                PromotionActiveTo = model.PromotionActiveTo,
                PromotionEnabled = model.PromotionEnabled,
                PromotionOrder = model.PromotionOrder,
                PromotionCreated = DateTime.UtcNow,
                PromotionRedemptionCount = 0
            };

            await promotionProvider.SetAsync(promotionInfo);

            return GetSuccessResponse("Buy X Get Y promotion created successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error creating Buy X Get Y promotion: {ex.Message}");
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
