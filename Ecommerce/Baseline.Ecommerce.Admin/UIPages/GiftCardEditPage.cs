using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardSectionPage),
    slug: "edit",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardEditPage),
    name: "Edit Gift Card",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for editing existing Gift Cards.
/// </summary>
[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class GiftCardEditPage : ModelEditPage<GiftCardViewModel>
{
    private readonly IInfoProvider<CurrencyInfo> _currencyProvider;
    private GiftCardViewModel? _model = null;

    public GiftCardEditPage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<CurrencyInfo> currencyProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _currencyProvider = currencyProvider;
    }

    [PageParameter(typeof(IntPageModelBinder))]
    public int ObjectId { get; set; }

    protected override GiftCardViewModel Model
    {
        get
        {
            if (_model == null)
            {
                var giftCardInfo = Provider<GiftCardInfo>.Instance.Get()
                    .WhereEquals(nameof(GiftCardInfo.GiftCardID), ObjectId)
                    .FirstOrDefault();

                if (giftCardInfo != null)
                {
                    _model = new GiftCardViewModel
                    {
                        GiftCardID = giftCardInfo.GiftCardID,
                        GiftCardGuid = giftCardInfo.GiftCardGuid,
                        GiftCardCode = giftCardInfo.GiftCardCode,
                        GiftCardInitialAmount = giftCardInfo.GiftCardInitialAmount,
                        GiftCardRemainingBalance = giftCardInfo.GiftCardRemainingBalance,
                        GiftCardCurrencyID = giftCardInfo.GiftCardCurrencyID.ToString(),
                        GiftCardRecipientMemberID = giftCardInfo.GiftCardRecipientMemberID,
                        GiftCardStatus = giftCardInfo.GiftCardStatus,
                        GiftCardExpiresAt = giftCardInfo.GiftCardExpiresAt,
                        GiftCardEnabled = giftCardInfo.GiftCardEnabled,
                        GiftCardNotes = giftCardInfo.GiftCardNotes
                    };
                }
                else
                {
                    _model = new GiftCardViewModel();
                }
            }
            return _model;
        }
    }

    protected override async Task<ICommandResponse> ProcessFormData(GiftCardViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            var giftCardInfo = Provider<GiftCardInfo>.Instance.Get()
                .WhereEquals(nameof(GiftCardInfo.GiftCardID), ObjectId)
                .FirstOrDefault();

            if (giftCardInfo == null)
            {
                return GetErrorResponse("Gift card not found.");
            }

            // Validate currency
            if (!int.TryParse(model.GiftCardCurrencyID, out var currencyId) || currencyId <= 0)
            {
                return GetErrorResponse("Please select a valid currency.");
            }

            var currency = await _currencyProvider.GetAsync(currencyId);
            if (currency == null)
            {
                return GetErrorResponse("Selected currency not found.");
            }

            // Validate remaining balance doesn't exceed initial amount
            if (model.GiftCardRemainingBalance > model.GiftCardInitialAmount)
            {
                return GetErrorResponse("Remaining balance cannot exceed initial amount.");
            }

            // Update status based on balance
            var status = model.GiftCardStatus;
            if (model.GiftCardRemainingBalance <= 0 && status != GiftCardStatuses.Cancelled)
            {
                status = GiftCardStatuses.FullyRedeemed;
            }
            else if (model.GiftCardRemainingBalance < model.GiftCardInitialAmount &&
                     model.GiftCardRemainingBalance > 0 &&
                     status == GiftCardStatuses.Active)
            {
                status = GiftCardStatuses.PartiallyRedeemed;
            }

            // Code cannot be changed after creation (for audit trail)
            // giftCardInfo.GiftCardCode = model.GiftCardCode;

            giftCardInfo.GiftCardInitialAmount = model.GiftCardInitialAmount;
            giftCardInfo.GiftCardRemainingBalance = model.GiftCardRemainingBalance;
            giftCardInfo.GiftCardCurrencyID = currencyId;
            giftCardInfo.GiftCardRecipientMemberID = model.GiftCardRecipientMemberID;
            giftCardInfo.GiftCardStatus = status;
            giftCardInfo.GiftCardExpiresAt = model.GiftCardExpiresAt;
            giftCardInfo.GiftCardEnabled = model.GiftCardEnabled;
            giftCardInfo.GiftCardNotes = model.GiftCardNotes;
            giftCardInfo.GiftCardLastModified = DateTime.UtcNow;

            // Offload to thread pool — Provider.Set() is sync-only
            await Task.Run(() => Provider<GiftCardInfo>.Instance.Set(giftCardInfo));

            return GetSuccessResponse("Gift card updated successfully.");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error updating gift card: {ex.Message}");
        }
    }

    private ICommandResponse GetSuccessResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationSuccess));
        response.AddSuccessMessage(message);
        return response;
    }

    private ICommandResponse GetErrorResponse(string message)
    {
        var response = ResponseFrom(new FormSubmissionResult(FormSubmissionStatus.ValidationFailure));
        response.AddErrorMessage(message);
        return response;
    }
}
