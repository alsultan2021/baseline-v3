using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletEditPage),
    name: "Edit Wallet",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing member Wallets.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class WalletEditPage : ModelEditPage<WalletViewModel>
{
    private WalletViewModel? model = null;

    public WalletEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override WalletViewModel Model
    {
        get
        {
            if (model == null)
            {
                var walletInfo = Provider<WalletInfo>.Instance.Get()
                    .WhereEquals(nameof(WalletInfo.WalletID), ObjectId)
                    .FirstOrDefault();

                if (walletInfo != null)
                {
                    model = new WalletViewModel
                    {
                        WalletID = walletInfo.WalletID,
                        WalletGuid = walletInfo.WalletGuid,
                        WalletMemberID = walletInfo.WalletMemberID.ToString(),
                        WalletCurrencyID = walletInfo.WalletCurrencyID.ToString(),
                        WalletType = walletInfo.WalletType,
                        WalletBalance = walletInfo.WalletBalance,
                        WalletHeldBalance = walletInfo.WalletHeldBalance,
                        WalletCreditLimit = walletInfo.WalletCreditLimit,
                        WalletEnabled = walletInfo.WalletEnabled,
                        WalletFrozen = walletInfo.WalletFrozen,
                        WalletFreezeReason = walletInfo.WalletFreezeReason,
                        WalletExpiresAt = walletInfo.WalletExpiresAt
                    };
                }
                else
                {
                    model = new WalletViewModel();
                }
            }
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(WalletViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var walletInfo = Provider<WalletInfo>.Instance.Get()
                .WhereEquals(nameof(WalletInfo.WalletID), ObjectId)
                .FirstOrDefault();

            if (walletInfo == null)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Wallet not found.");
                return await Task.FromResult(errorResponse);
            }

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

            // Map ViewModel back to Info object
            walletInfo.WalletMemberID = memberId;
            walletInfo.WalletCurrencyID = currencyId;
            walletInfo.WalletType = model.WalletType;
            walletInfo.WalletBalance = model.WalletBalance;
            walletInfo.WalletHeldBalance = model.WalletHeldBalance;
            walletInfo.WalletCreditLimit = model.WalletCreditLimit;
            walletInfo.WalletEnabled = model.WalletEnabled;
            walletInfo.WalletFrozen = model.WalletFrozen;
            walletInfo.WalletFreezeReason = model.WalletFreezeReason;
            walletInfo.WalletExpiresAt = model.WalletExpiresAt;
            walletInfo.WalletLastModified = DateTime.Now;

            await Provider<WalletInfo>.Instance.SetAsync(walletInfo);

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Wallet updated successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error updating wallet: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
