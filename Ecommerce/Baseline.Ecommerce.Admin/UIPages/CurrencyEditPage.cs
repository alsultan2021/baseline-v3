using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencySectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyEditPage),
    name: "Edit Currency",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Currencies.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class CurrencyEditPage : ModelEditPage<CurrencyViewModel>
{
    private CurrencyViewModel? model = null;

    public CurrencyEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override CurrencyViewModel Model
    {
        get
        {
            if (model == null)
            {
                var currencyInfo = Provider<CurrencyInfo>.Instance.Get()
                    .WhereEquals(nameof(CurrencyInfo.CurrencyID), ObjectId)
                    .FirstOrDefault();

                if (currencyInfo != null)
                {
                    model = new CurrencyViewModel
                    {
                        CurrencyID = currencyInfo.CurrencyID,
                        CurrencyGuid = currencyInfo.CurrencyGuid,
                        CurrencyCode = currencyInfo.CurrencyCode,
                        CurrencyDisplayName = currencyInfo.CurrencyDisplayName,
                        CurrencySymbol = currencyInfo.CurrencySymbol,
                        CurrencyDecimalPlaces = currencyInfo.CurrencyDecimalPlaces,
                        CurrencyFormatPattern = currencyInfo.CurrencyFormatPattern,
                        CurrencyEnabled = currencyInfo.CurrencyEnabled,
                        CurrencyOrder = currencyInfo.CurrencyOrder
                    };
                }
                else
                {
                    model = new CurrencyViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(CurrencyViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var currencyInfo = Provider<CurrencyInfo>.Instance.Get()
                .WhereEquals(nameof(CurrencyInfo.CurrencyID), ObjectId)
                .FirstOrDefault();

            if (currencyInfo == null)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Currency not found.");
                return await Task.FromResult(errorResponse);
            }

            // Map ViewModel back to Info object
            currencyInfo.CurrencyCode = model.CurrencyCode?.ToUpperInvariant() ?? string.Empty;
            currencyInfo.CurrencyDisplayName = model.CurrencyDisplayName;
            currencyInfo.CurrencySymbol = model.CurrencySymbol;
            currencyInfo.CurrencyDecimalPlaces = model.CurrencyDecimalPlaces;
            currencyInfo.CurrencyFormatPattern = model.CurrencyFormatPattern;
            currencyInfo.CurrencyEnabled = model.CurrencyEnabled;
            currencyInfo.CurrencyOrder = model.CurrencyOrder;
            currencyInfo.CurrencyLastModified = DateTime.Now;

            await Provider<CurrencyInfo>.Instance.SetAsync(currencyInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Currency updated successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error updating currency: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
