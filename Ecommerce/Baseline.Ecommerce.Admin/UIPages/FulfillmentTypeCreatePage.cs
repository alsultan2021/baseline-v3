using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeCreatePage),
    name: "Create Fulfillment Type",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Fulfillment Types.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class FulfillmentTypeCreatePage : ModelEditPage<FulfillmentTypeViewModel>
{
    private FulfillmentTypeViewModel? model = null;

    public FulfillmentTypeCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    protected override FulfillmentTypeViewModel Model
    {
        get
        {
            model ??= new FulfillmentTypeViewModel
            {
                FulfillmentTypeGUID = Guid.NewGuid(),
                FulfillmentTypeIsEnabled = true,
                FulfillmentTypeRequiresBillingAddress = true
            };
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(FulfillmentTypeViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var fulfillmentTypeInfo = new FulfillmentTypeInfo
            {
                FulfillmentTypeGUID = model.FulfillmentTypeGUID,
                FulfillmentTypeCodeName = model.FulfillmentTypeCodeName,
                FulfillmentTypeDisplayName = model.FulfillmentTypeDisplayName,
                FulfillmentTypeDescription = model.FulfillmentTypeDescription,
                FulfillmentTypeRequiresShipping = model.FulfillmentTypeRequiresShipping,
                FulfillmentTypeRequiresBillingAddress = model.FulfillmentTypeRequiresBillingAddress,
                FulfillmentTypeSupportsDeliveryOptions = model.FulfillmentTypeSupportsDeliveryOptions,
                FulfillmentTypeIsEnabled = model.FulfillmentTypeIsEnabled,
                FulfillmentTypeLastModified = DateTime.Now
            };

            await Provider<FulfillmentTypeInfo>.Instance.SetAsync(fulfillmentTypeInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Fulfillment type created successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error creating fulfillment type: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
