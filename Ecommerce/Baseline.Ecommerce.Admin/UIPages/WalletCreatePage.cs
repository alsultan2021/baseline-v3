using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletCreatePage),
    name: "Create Wallet",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new member Wallets.
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class WalletCreatePage : ModelEditPage<WalletViewModel>
{
    private WalletViewModel? model = null;

    public WalletCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    protected override WalletViewModel Model
    {
        get
        {
            model ??= new WalletViewModel
            {
                WalletGuid = Guid.NewGuid(),
                WalletEnabled = true,
                WalletFrozen = false,
                WalletBalance = 0m,
                WalletHeldBalance = 0m,
                WalletType = "StoreCredit"
            };
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(WalletViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            // Parse string IDs from dropdown to integers
            if (!int.TryParse(model.WalletMemberID, out var memberId) || memberId <= 0)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Please select a valid member.");
                return await Task.FromResult(errorResponse);
            }

            if (!int.TryParse(model.WalletCurrencyID, out var currencyId) || currencyId <= 0)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Please select a valid currency.");
                return await Task.FromResult(errorResponse);
            }

            var walletInfo = new WalletInfo
            {
                WalletGuid = model.WalletGuid,
                WalletMemberID = memberId,
                WalletCurrencyID = currencyId,
                WalletType = model.WalletType,
                WalletBalance = model.WalletBalance,
                WalletHeldBalance = model.WalletHeldBalance,
                WalletCreditLimit = model.WalletCreditLimit,
                WalletEnabled = model.WalletEnabled,
                WalletFrozen = model.WalletFrozen,
                WalletFreezeReason = model.WalletFreezeReason,
                WalletExpiresAt = model.WalletExpiresAt,
                WalletCreatedWhen = DateTime.Now,
                WalletLastModified = DateTime.Now
            };

            await Provider<WalletInfo>.Instance.SetAsync(walletInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Wallet created successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error creating wallet: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
