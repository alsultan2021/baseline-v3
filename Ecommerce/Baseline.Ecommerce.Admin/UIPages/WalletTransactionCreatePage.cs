using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionListPage),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionCreatePage),
    name: "Create Transaction",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new wallet transactions (admin adjustments).
/// </summary>
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class WalletTransactionCreatePage : ModelEditPage<WalletTransactionViewModel>
{
    private WalletTransactionViewModel? model = null;

    [PageParameter(typeof(IntPageModelBinder), typeof(WalletSectionPage))]
    public int WalletId { get; set; }

    public WalletTransactionCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    protected override WalletTransactionViewModel Model
    {
        get
        {
            model ??= new WalletTransactionViewModel
            {
                TransactionGuid = Guid.NewGuid(),
                TransactionWalletID = WalletId,
                TransactionType = "Adjustment",
                TransactionStatus = "Completed",
                TransactionCreatedWhen = DateTime.Now
            };
            return model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(WalletTransactionViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            // Get current wallet to calculate balance after
            var wallet = Provider<WalletInfo>.Instance.Get()
                .WhereEquals(nameof(WalletInfo.WalletID), model.TransactionWalletID)
                .FirstOrDefault();

            if (wallet == null)
            {
                var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
                errorResponse.AddErrorMessage("Wallet not found.");
                return await Task.FromResult(errorResponse);
            }

            var newBalance = wallet.WalletBalance + model.TransactionAmount;

            var transactionInfo = new WalletTransactionInfo
            {
                TransactionGuid = model.TransactionGuid,
                TransactionWalletID = model.TransactionWalletID,
                TransactionType = model.TransactionType,
                TransactionAmount = model.TransactionAmount,
                TransactionBalanceAfter = newBalance,
                TransactionStatus = model.TransactionStatus,
                TransactionDescription = model.TransactionDescription,
                TransactionReference = model.TransactionReference,
                TransactionOrderID = model.TransactionOrderID,
                TransactionCreatedWhen = DateTime.Now,
                TransactionCreatedBy = null // TODO: Get current admin user ID
            };

            await Provider<WalletTransactionInfo>.Instance.SetAsync(transactionInfo);

            // Update wallet balance if transaction is completed
            if (model.TransactionStatus == "Completed")
            {
                wallet.WalletBalance = newBalance;
                wallet.WalletLastModified = DateTime.Now;
                await Provider<WalletInfo>.Instance.SetAsync(wallet);
            }

            var successResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
            successResponse.AddSuccessMessage("Transaction created successfully.");
            return await Task.FromResult(successResponse);
        }
        catch (Exception ex)
        {
            var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
            errorResponse.AddErrorMessage($"Error creating transaction: {ex.Message}");
            return await Task.FromResult(errorResponse);
        }
    }
}
