using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.FulfillmentTypeEditPage),
    name: "Edit Fulfillment Type",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Fulfillment Types.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class FulfillmentTypeEditPage : ModelEditPage<FulfillmentTypeViewModel>
{
    private FulfillmentTypeViewModel? model = null;

    public FulfillmentTypeEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override FulfillmentTypeViewModel Model
    {
        get
        {
            if (model == null)
            {
                var fulfillmentTypeInfo = Provider<FulfillmentTypeInfo>.Instance.Get()
                    .WhereEquals(nameof(FulfillmentTypeInfo.FulfillmentTypeID), ObjectId)
                    .FirstOrDefault();

                if (fulfillmentTypeInfo != null)
                {
                    model = new FulfillmentTypeViewModel
                    {
                        FulfillmentTypeID = fulfillmentTypeInfo.FulfillmentTypeID,
                        FulfillmentTypeGUID = fulfillmentTypeInfo.FulfillmentTypeGUID,
                        FulfillmentTypeCodeName = fulfillmentTypeInfo.FulfillmentTypeCodeName,
                        FulfillmentTypeDisplayName = fulfillmentTypeInfo.FulfillmentTypeDisplayName,
                        FulfillmentTypeDescription = fulfillmentTypeInfo.FulfillmentTypeDescription,
                        FulfillmentTypeRequiresShipping = fulfillmentTypeInfo.FulfillmentTypeRequiresShipping,
                        FulfillmentTypeRequiresBillingAddress = fulfillmentTypeInfo.FulfillmentTypeRequiresBillingAddress,
                        FulfillmentTypeSupportsDeliveryOptions = fulfillmentTypeInfo.FulfillmentTypeSupportsDeliveryOptions,
                        FulfillmentTypeIsEnabled = fulfillmentTypeInfo.FulfillmentTypeIsEnabled
                    };
                }
                else
                {
                    model = new FulfillmentTypeViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(FulfillmentTypeViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var fulfillmentTypeInfo = Provider<FulfillmentTypeInfo>.Instance.Get()
                .WhereEquals(nameof(FulfillmentTypeInfo.FulfillmentTypeID), ObjectId)
                .FirstOrDefault();

            if (fulfillmentTypeInfo == null)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Fulfillment type not found.");
                return await Task.FromResult(errorResponse);
            }

            // Map ViewModel back to Info object
            fulfillmentTypeInfo.FulfillmentTypeCodeName = model.FulfillmentTypeCodeName;
            fulfillmentTypeInfo.FulfillmentTypeDisplayName = model.FulfillmentTypeDisplayName;
            fulfillmentTypeInfo.FulfillmentTypeDescription = model.FulfillmentTypeDescription;
            fulfillmentTypeInfo.FulfillmentTypeRequiresShipping = model.FulfillmentTypeRequiresShipping;
            fulfillmentTypeInfo.FulfillmentTypeRequiresBillingAddress = model.FulfillmentTypeRequiresBillingAddress;
            fulfillmentTypeInfo.FulfillmentTypeSupportsDeliveryOptions = model.FulfillmentTypeSupportsDeliveryOptions;
            fulfillmentTypeInfo.FulfillmentTypeIsEnabled = model.FulfillmentTypeIsEnabled;
            fulfillmentTypeInfo.FulfillmentTypeLastModified = DateTime.Now;

            await Provider<FulfillmentTypeInfo>.Instance.SetAsync(fulfillmentTypeInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Fulfillment type updated successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error updating fulfillment type: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
