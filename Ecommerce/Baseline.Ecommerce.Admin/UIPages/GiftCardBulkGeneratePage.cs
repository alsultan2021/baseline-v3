using System.Text;
using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Admin.ViewModels;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;

using IFormItemCollectionProvider = Kentico.Xperience.Admin.Base.Forms.Internal.IFormItemCollectionProvider;

[assembly: UIPage(
    parentType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardApplication),
    slug: "generate",
    uiPageType: typeof(Baseline.Ecommerce.Admin.UIPages.GiftCardBulkGeneratePage),
    name: "Generate Gift Cards",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Admin page for bulk generating Gift Cards.
/// </summary>
[UINavigation(false)]
[UIEvaluatePermission(SystemPermissions.CREATE)]
public class GiftCardBulkGeneratePage : ModelEditPage<GiftCardBulkGenerateViewModel>
{
    private readonly IInfoProvider<GiftCardInfo> _giftCardProvider;
    private readonly IInfoProvider<CurrencyInfo> _currencyProvider;
    private GiftCardBulkGenerateViewModel? _model = null;

    public GiftCardBulkGeneratePage(
        IFormItemCollectionProvider formItemCollectionProvider,
        IFormDataBinder formDataBinder,
        IInfoProvider<GiftCardInfo> giftCardProvider,
        IInfoProvider<CurrencyInfo> currencyProvider)
        : base(formItemCollectionProvider, formDataBinder)
    {
        _giftCardProvider = giftCardProvider;
        _currencyProvider = currencyProvider;
    }

    protected override GiftCardBulkGenerateViewModel Model => _model ??= new GiftCardBulkGenerateViewModel();

    public override Task ConfigurePage()
    {
        PageConfiguration.SubmitConfiguration.Label = "Generate Gift Cards";
        return base.ConfigurePage();
    }

    protected override async Task<ICommandResponse> ProcessFormData(GiftCardBulkGenerateViewModel model, ICollection<IFormItem> formItems)
    {
        try
        {
            // Validate quantity
            if (model.Quantity < 1 || model.Quantity > 100)
            {
                return GetErrorResponse("Quantity must be between 1 and 100.");
            }

            // Validate amount
            if (model.Amount <= 0)
            {
                return GetErrorResponse("Amount must be greater than zero.");
            }

            // Validate currency
            if (!int.TryParse(model.CurrencyID, out var currencyId) || currencyId <= 0)
            {
                return GetErrorResponse("Please select a valid currency.");
            }

            var currency = await _currencyProvider.GetAsync(currencyId);
            if (currency == null)
            {
                return GetErrorResponse("Selected currency not found.");
            }

            // Generate gift cards
            var generatedCodes = new List<string>();
            var prefix = string.IsNullOrWhiteSpace(model.CodePrefix) ? "GIFT" : model.CodePrefix.Trim().ToUpperInvariant();
            var now = DateTime.UtcNow;

            for (int i = 0; i < model.Quantity; i++)
            {
                // Generate unique code
                string code;
                int attempts = 0;
                do
                {
                    code = GenerateGiftCardCode(prefix);
                    attempts++;
                    if (attempts > 100)
                    {
                        return GetErrorResponse($"Failed to generate unique codes after multiple attempts. Generated {generatedCodes.Count} cards.");
                    }
                } while (await CodeExistsAsync(code));

                var giftCardInfo = new GiftCardInfo
                {
                    GiftCardGuid = Guid.NewGuid(),
                    GiftCardCode = code,
                    GiftCardInitialAmount = model.Amount,
                    GiftCardRemainingBalance = model.Amount,
                    GiftCardCurrencyID = currencyId,
                    GiftCardStatus = GiftCardStatuses.Active,
                    GiftCardExpiresAt = model.ExpiresAt,
                    GiftCardEnabled = true,
                    GiftCardNotes = model.Notes,
                    GiftCardCreatedWhen = now,
                    GiftCardLastModified = now
                };

                await _giftCardProvider.SetAsync(giftCardInfo);
                generatedCodes.Add(code);
            }

            // Build success message with generated codes
            var message = new StringBuilder();
            message.AppendLine($"Successfully generated {generatedCodes.Count} gift cards with {currency.FormatAmount(model.Amount)} each.");
            message.AppendLine();
            message.AppendLine("Generated codes:");
            foreach (var code in generatedCodes)
            {
                message.AppendLine($"• {code}");
            }

            return GetSuccessResponse(message.ToString());
        }
        catch (Exception ex)
        {
            return GetErrorResponse($"Error generating gift cards: {ex.Message}");
        }
    }

    private async Task<bool> CodeExistsAsync(string code)
    {
        var existing = await _giftCardProvider
            .Get()
            .WhereEquals(nameof(GiftCardInfo.GiftCardCode), code)
            .TopN(1)
            .GetEnumerableTypedResultAsync();

        return existing.Any();
    }

    /// <summary>
    /// Generates a unique gift card code in the format PREFIX-XXXX-XXXX.
    /// </summary>
    private static string GenerateGiftCardCode(string prefix)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing characters like 0,O,I,1

        string GenerateSegment(int length)
        {
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        return $"{prefix}-{GenerateSegment(4)}-{GenerateSegment(4)}";
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
