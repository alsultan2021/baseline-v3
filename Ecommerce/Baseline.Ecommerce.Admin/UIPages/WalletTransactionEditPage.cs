using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionSectionPage),
    slug: "view",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.WalletTransactionEditPage),
    name: "View Transaction",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for viewing wallet transaction details.
/// Transactions are generally read-only after creation (append-only ledger).
/// </summary>
[UIEvaluatePermission(SystemPermissions.VIEW)]
public class WalletTransactionEditPage : ModelEditPage<WalletTransactionViewModel>
{
    private WalletTransactionViewModel? model = null;

    public WalletTransactionEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder)
        : base(formItemCollectionProvider, formDataBinder)
    {
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override WalletTransactionViewModel Model
    {
        get
        {
            if (model == null)
            {
                var transactionInfo = Provider<WalletTransactionInfo>.Instance.Get()
                    .WhereEquals(nameof(WalletTransactionInfo.TransactionID), ObjectId)
                    .FirstOrDefault();

                if (transactionInfo != null)
                {
                    model = new WalletTransactionViewModel
                    {
                        TransactionID = transactionInfo.TransactionID,
                        TransactionGuid = transactionInfo.TransactionGuid,
                        TransactionWalletID = transactionInfo.TransactionWalletID,
                        TransactionType = transactionInfo.TransactionType,
                        TransactionAmount = transactionInfo.TransactionAmount,
                        TransactionBalanceAfter = transactionInfo.TransactionBalanceAfter,
                        TransactionStatus = transactionInfo.TransactionStatus,
                        TransactionDescription = transactionInfo.TransactionDescription,
                        TransactionReference = transactionInfo.TransactionReference,
                        TransactionOrderID = transactionInfo.TransactionOrderID,
                        TransactionCreatedWhen = transactionInfo.TransactionCreatedWhen,
                        TransactionCreatedBy = transactionInfo.TransactionCreatedBy
                    };
                }
                else
                {
                    model = new WalletTransactionViewModel();
                }
            }
            return model;
        }
    }

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Make the form read-only since transactions are immutable (append-only ledger)
        PageConfiguration.EditMode = FormEditMode.Disabled;
    }

    protected override async Task<ICommandResponse> ProcessFormData(WalletTransactionViewModel model, ICollection<IFormItem> formItems)
    {
        // Transactions are immutable - this should not be called in read-only mode
        var errorResponse = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
        errorResponse.AddErrorMessage("Transactions cannot be modified. Create a new adjustment transaction instead.");
        return await Task.FromResult(errorResponse);
    }
}
