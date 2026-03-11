using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyCreatePage),
    name: "Create Currency",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Currencies.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class CurrencyCreatePage : ModelEditPage<CurrencyViewModel>
{
    private CurrencyViewModel? model = null;

    public CurrencyCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    protected override CurrencyViewModel Model
    {
        get
        {
            model ??= new CurrencyViewModel
            {
                CurrencyGuid = Guid.NewGuid(),
                CurrencyEnabled = true,
                CurrencyDecimalPlaces = 2,
                CurrencyOrder = 0
            };
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(CurrencyViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var currencyInfo = new CurrencyInfo
            {
                CurrencyGuid = model.CurrencyGuid,
                CurrencyCode = model.CurrencyCode?.ToUpperInvariant() ?? string.Empty,
                CurrencyDisplayName = model.CurrencyDisplayName,
                CurrencySymbol = model.CurrencySymbol,
                CurrencyDecimalPlaces = model.CurrencyDecimalPlaces,
                CurrencyFormatPattern = model.CurrencyFormatPattern,
                CurrencyIsDefault = false, // Default is now managed via CommerceChannelSettings
                CurrencyEnabled = model.CurrencyEnabled,
                CurrencyOrder = model.CurrencyOrder,
                CurrencyLastModified = DateTime.Now
            };

            await Provider<CurrencyInfo>.Instance.SetAsync(currencyInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Currency created successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error creating currency: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
