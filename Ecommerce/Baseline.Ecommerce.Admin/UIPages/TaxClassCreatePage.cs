using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.TaxClassCreatePage),
    name: "Create Tax Class",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Tax Classes.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class TaxClassCreatePage : ModelEditPage<TaxClassViewModel>
{
    private TaxClassViewModel? model = null;

    public TaxClassCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    protected override TaxClassViewModel Model
    {
        get
        {
            model ??= new TaxClassViewModel
            {
                TaxClassGuid = Guid.NewGuid(),
                TaxClassEnabled = true,
                TaxClassOrder = 0
            };
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(TaxClassViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var taxClassInfo = new TaxClassInfo
            {
                TaxClassGuid = model.TaxClassGuid,
                TaxClassName = model.TaxClassName,
                TaxClassDisplayName = model.TaxClassDisplayName,
                TaxClassDescription = model.TaxClassDescription,
                TaxClassDefaultRate = model.TaxClassDefaultRate,
                TaxClassIsDefault = model.TaxClassIsDefault,
                TaxClassIsExempt = model.TaxClassIsExempt,
                TaxClassOrder = model.TaxClassOrder,
                TaxClassEnabled = model.TaxClassEnabled,
                TaxClassLastModified = DateTime.Now
            };

            // If this is marked as default, unset other defaults
            if (model.TaxClassIsDefault)
            {
                await ClearOtherDefaults();
            }

            await Provider<TaxClassInfo>.Instance.SetAsync(taxClassInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Tax class created successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error creating tax class: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }

    private static async Task ClearOtherDefaults()
    {
        var defaults = await Provider<TaxClassInfo>.Instance.Get()
            .WhereEquals(nameof(TaxClassInfo.TaxClassIsDefault), true)
            .GetEnumerableTypedResultAsync();

        foreach (var taxClass in defaults)
        {
            taxClass.TaxClassIsDefault = false;
            await Provider<TaxClassInfo>.Instance.SetAsync(taxClass);
        }
    }
}
