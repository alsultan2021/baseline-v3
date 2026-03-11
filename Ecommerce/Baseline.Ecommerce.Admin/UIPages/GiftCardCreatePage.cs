using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardApplication),
    slug: "create",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardCreatePage),
    name: "Create Gift Card",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for creating new Gift Cards.
/// </summary>
[UINavigation(false)]
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class GiftCardCreatePage : ModelEditPage<GiftCardViewModel>
{
    private readonly IInfoProvider<GiftCardInfo> _giftCardProvider;
    private readonly IInfoProvider<CurrencyInfo> _currencyProvider;
    private GiftCardViewModel? _model = null;

    public GiftCardCreatePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<GiftCardInfo> giftCardProvider,
        IInfoProvider<CurrencyInfo> currencyProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _giftCardProvider = giftCardProvider;
        _currencyProvider = currencyProvider;
    }

    protected override GiftCardViewModel Model => _model ??= new GiftCardViewModel
    {
        GiftCardGuid = Guid.NewGuid(),
        GiftCardEnabled = true,
        GiftCardStatus = GiftCardStatuses.Active
    };

    protected override async Task<ICommandResponse> ProcessFormData(GiftCardViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            // Generate code if empty
            var code = string.IsNullOrWhiteSpace(model.GiftCardCode)
                ? GenerateGiftCardCode()
                : model.GiftCardCode.ToUpperInvariant().Trim();

            // Check for duplicate code
            var existing = await _giftCardProvider
                .Get()
                .WhereEquals(nameof(GiftCardInfo.GiftCardCode), code)
                .GetEnumerableTypedResultAsync();

            if (existing.Any())
            {
                return GetErrorResponse("A gift card with this code already exists.");
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

            // Remaining balance should equal initial amount on creation
            var remainingBalance = model.GiftCardInitialAmount;

            var giftCardInfo = new GiftCardInfo
            {
                GiftCardGuid = model.GiftCardGuid,
                GiftCardCode = code,
                GiftCardInitialAmount = model.GiftCardInitialAmount,
                GiftCardRemainingBalance = remainingBalance,
                GiftCardCurrencyID = currencyId,
                GiftCardRecipientMemberID = model.GiftCardRecipientMemberID,
                GiftCardStatus = model.GiftCardStatus,
                GiftCardExpiresAt = model.GiftCardExpiresAt,
                GiftCardEnabled = model.GiftCardEnabled,
                GiftCardNotes = model.GiftCardNotes,
                GiftCardCreatedWhen = DateTime.UtcNow,
                GiftCardLastModified = DateTime.UtcNow
            };

            await _giftCardProvider.SetAsync(giftCardInfo);

            return GetSuccessResponse($"Gift card created successfully. Code: {code}");
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error creating gift card: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a unique gift card code in the format GIFT-XXXX-XXXX.
    /// </summary>
    private static string GenerateGiftCardCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing characters like 0,O,I,1

        string GenerateSegment(int length)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        return $"GIFT-{GenerateSegment(4)}-{GenerateSegment(4)}";
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
