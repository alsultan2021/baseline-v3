using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateEditPage),
    name: "Edit Exchange Rate",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Exchange Rates.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class CurrencyExchangeRateEditPage : ModelEditPage<CurrencyExchangeRateViewModel>
{
    private CurrencyExchangeRateViewModel? model = null;

    public CurrencyExchangeRateEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override CurrencyExchangeRateViewModel Model
    {
        get
        {
            if (model == null)
            {
                var rateInfo = Provider<CurrencyExchangeRateInfo>.Instance.Get()
                    .WhereEquals(nameof(CurrencyExchangeRateInfo.ExchangeRateID), ObjectId)
                    .FirstOrDefault();

                if (rateInfo != null)
                {
                    model = new CurrencyExchangeRateViewModel
                    {
                        ExchangeRateID = rateInfo.ExchangeRateID,
                        ExchangeRateGuid = rateInfo.ExchangeRateGuid,
                        ExchangeRateFromCurrencyID = rateInfo.ExchangeRateFromCurrencyID,
                        ExchangeRateToCurrencyID = rateInfo.ExchangeRateToCurrencyID,
                        ExchangeRateValue = rateInfo.ExchangeRateValue,
                        ExchangeRateValidFrom = rateInfo.ExchangeRateValidFrom,
                        ExchangeRateValidTo = rateInfo.ExchangeRateValidTo,
                        ExchangeRateSource = rateInfo.ExchangeRateSource,
                        ExchangeRateEnabled = rateInfo.ExchangeRateEnabled
                    };
                }
                else
                {
                    model = new CurrencyExchangeRateViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(CurrencyExchangeRateViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var rateInfo = Provider<CurrencyExchangeRateInfo>.Instance.Get()
                .WhereEquals(nameof(CurrencyExchangeRateInfo.ExchangeRateID), ObjectId)
                .FirstOrDefault();

            if (rateInfo == null)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Exchange rate not found.");
                return await Task.FromResult(errorResponse);
            }

            // Map ViewModel back to Info object
            rateInfo.ExchangeRateFromCurrencyID = model.ExchangeRateFromCurrencyID;
            rateInfo.ExchangeRateToCurrencyID = model.ExchangeRateToCurrencyID;
            rateInfo.ExchangeRateValue = model.ExchangeRateValue;
            rateInfo.ExchangeRateValidFrom = model.ExchangeRateValidFrom ?? DateTime.MinValue;
            rateInfo.ExchangeRateValidTo = model.ExchangeRateValidTo ?? DateTime.MaxValue;
            rateInfo.ExchangeRateSource = model.ExchangeRateSource ?? "Manual";
            rateInfo.ExchangeRateEnabled = model.ExchangeRateEnabled;
            rateInfo.ExchangeRateLastModified = DateTime.Now;

            await Provider<CurrencyExchangeRateInfo>.Instance.SetAsync(rateInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Exchange rate updated successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error updating exchange rate: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
