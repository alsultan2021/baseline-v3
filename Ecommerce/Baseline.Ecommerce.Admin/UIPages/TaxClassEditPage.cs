using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassEditPage),
    name: "Edit Tax Class",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Tax Classes.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class TaxClassEditPage : ModelEditPage<TaxClassViewModel>
{
    private TaxClassViewModel? model = null;

    public TaxClassEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override TaxClassViewModel Model
    {
        get
        {
            if (model == null)
            {
                var taxClassInfo = Provider<TaxClassInfo>.Instance.Get()
                    .WhereEquals(nameof(TaxClassInfo.TaxClassID), ObjectId)
                    .FirstOrDefault();

                if (taxClassInfo != null)
                {
                    model = new TaxClassViewModel
                    {
                        TaxClassID = taxClassInfo.TaxClassID,
                        TaxClassGuid = taxClassInfo.TaxClassGuid,
                        TaxClassName = taxClassInfo.TaxClassName,
                        TaxClassDisplayName = taxClassInfo.TaxClassDisplayName,
                        TaxClassDescription = taxClassInfo.TaxClassDescription,
                        TaxClassDefaultRate = taxClassInfo.TaxClassDefaultRate,
                        TaxClassIsDefault = taxClassInfo.TaxClassIsDefault,
                        TaxClassIsExempt = taxClassInfo.TaxClassIsExempt,
                        TaxClassOrder = taxClassInfo.TaxClassOrder,
                        TaxClassEnabled = taxClassInfo.TaxClassEnabled
                    };
                }
                else
                {
                    model = new TaxClassViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(TaxClassViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var taxClassInfo = Provider<TaxClassInfo>.Instance.Get()
                .WhereEquals(nameof(TaxClassInfo.TaxClassID), ObjectId)
                .FirstOrDefault();

            if (taxClassInfo == null)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Tax class not found.");
                return await Task.FromResult(errorResponse);
            }

            // If this is marked as default and wasn't before, unset other defaults
            if (model.TaxClassIsDefault && !taxClassInfo.TaxClassIsDefault)
            {
                await ClearOtherDefaults(taxClassInfo.TaxClassID);
            }

            // Map ViewModel back to Info object
            taxClassInfo.TaxClassName = model.TaxClassName;
            taxClassInfo.TaxClassDisplayName = model.TaxClassDisplayName;
            taxClassInfo.TaxClassDescription = model.TaxClassDescription;
            taxClassInfo.TaxClassDefaultRate = model.TaxClassDefaultRate;
            taxClassInfo.TaxClassIsDefault = model.TaxClassIsDefault;
            taxClassInfo.TaxClassIsExempt = model.TaxClassIsExempt;
            taxClassInfo.TaxClassOrder = model.TaxClassOrder;
            taxClassInfo.TaxClassEnabled = model.TaxClassEnabled;
            taxClassInfo.TaxClassLastModified = DateTime.Now;

            await Provider<TaxClassInfo>.Instance.SetAsync(taxClassInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Tax class updated successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error updating tax class: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }

    private static async Task ClearOtherDefaults(int excludeId)
    {
        var defaults = await Provider<TaxClassInfo>.Instance.Get()
            .WhereEquals(nameof(TaxClassInfo.TaxClassIsDefault), true)
            .WhereNotEquals(nameof(TaxClassInfo.TaxClassID), excludeId)
            .GetEnumerableTypedResultAsync();

        foreach (var taxClass in defaults)
        {
            taxClass.TaxClassIsDefault = false;
            await Provider<TaxClassInfo>.Instance.SetAsync(taxClass);
        }
    }
}
