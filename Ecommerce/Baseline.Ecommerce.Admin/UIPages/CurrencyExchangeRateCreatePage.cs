using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.CurrencyExchangeRateCreatePage),
    name: "Create Exchange Rate",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Exchange Rates.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class CurrencyExchangeRateCreatePage : ModelEditPage<CurrencyExchangeRateViewModel>
{
    private CurrencyExchangeRateViewModel? model = null;

    public CurrencyExchangeRateCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    protected override CurrencyExchangeRateViewModel Model
    {
        get
        {
            model ??= new CurrencyExchangeRateViewModel
            {
                ExchangeRateGuid = Guid.NewGuid(),
                ExchangeRateEnabled = true,
                ExchangeRateValue = 1m,
                ExchangeRateSource = "Manual"
            };
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(CurrencyExchangeRateViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var rateInfo = new CurrencyExchangeRateInfo
            {
                ExchangeRateGuid = model.ExchangeRateGuid,
                ExchangeRateFromCurrencyID = model.ExchangeRateFromCurrencyID,
                ExchangeRateToCurrencyID = model.ExchangeRateToCurrencyID,
                ExchangeRateValue = model.ExchangeRateValue,
                ExchangeRateValidFrom = model.ExchangeRateValidFrom ?? DateTime.MinValue,
                ExchangeRateValidTo = model.ExchangeRateValidTo ?? DateTime.MaxValue,
                ExchangeRateSource = model.ExchangeRateSource ?? "Manual",
                ExchangeRateEnabled = model.ExchangeRateEnabled,
                ExchangeRateLastModified = DateTime.Now
            };

            await Provider<CurrencyExchangeRateInfo>.Instance.SetAsync(rateInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Exchange rate created successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error creating exchange rate: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
