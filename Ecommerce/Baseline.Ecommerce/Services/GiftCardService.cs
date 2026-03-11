using Baseline.Ecommerce;
using Baseline.Ecommerce.Models;
using CMS.DataEngine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation of <see cref="IGiftCardService"/> for gift card management.
/// Handles creation, validation, and redemption of gift cards.
/// </summary>
public class GiftCardService(
    IInfoProvider<GiftCardInfo> giftCardProvider,
    IInfoProvider<CurrencyInfo> currencyProvider,
    IWalletService walletService,
    IOptions<BaselineEcommerceOptions> ecommerceOptions,
    ILogger<GiftCardService> logger) : IGiftCardService
{
    private readonly string _defaultCurrency = ecommerceOptions.Value.Pricing.DefaultCurrency;
    private const string CodeCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Excludes confusing characters

    /// <inheritdoc/>
    public async Task<GiftCardResult> CreateGiftCardAsync(CreateGiftCardRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate currency exists
            var currency = await currencyProvider.GetAsync(request.CurrencyId, cancellationToken);
            if (currency == null)
            {
                return GiftCardResult.Failed("Invalid currency specified.");
            }

            // Validate amount
            if (request.Amount <= 0)
            {
                return GiftCardResult.Failed("Gift card amount must be greater than zero.");
            }

            // Generate or validate code
            var code = string.IsNullOrWhiteSpace(request.Code)
                ? await GenerateUniqueCodeAsync(cancellationToken)
                : request.Code.Trim().ToUpperInvariant();

            // Check for duplicate code if custom code provided
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                var existing = await GetByCodeAsync(code, cancellationToken);
                if (existing != null)
                {
                    return GiftCardResult.Failed("A gift card with this code already exists.");
                }
            }

            var giftCard = new GiftCardInfo
            {
                GiftCardGuid = Guid.NewGuid(),
                GiftCardCode = code,
                GiftCardInitialAmount = request.Amount,
                GiftCardRemainingBalance = request.Amount,
                GiftCardCurrencyID = request.CurrencyId,
                GiftCardRecipientMemberID = request.RecipientMemberId,
                GiftCardStatus = GiftCardStatuses.Active,
                GiftCardExpiresAt = request.ExpiresAt,
                GiftCardEnabled = true,
                GiftCardNotes = BuildNotes(request),
                GiftCardCreatedWhen = DateTime.UtcNow,
                GiftCardLastModified = DateTime.UtcNow
            };

            await giftCardProvider.SetAsync(giftCard, cancellationToken);

            logger.LogDebug(
                "Created gift card {Code} with amount {Amount} {Currency}",
                code,
                request.Amount,
                currency.CurrencyCode);

            return GiftCardResult.Succeeded(giftCard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create gift card");
            return GiftCardResult.Failed($"Failed to create gift card: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<GiftCardInfo?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalizedCode = code.Trim().ToUpperInvariant();
        var results = await giftCardProvider.Get()
            .WhereEquals(nameof(GiftCardInfo.GiftCardCode), normalizedCode)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return results.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<GiftCardInfo?> GetByIdAsync(int giftCardId, CancellationToken cancellationToken = default)
    {
        return await giftCardProvider.GetAsync(giftCardId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GiftCardInfo?> GetByGuidAsync(Guid giftCardGuid, CancellationToken cancellationToken = default)
    {
        var results = await giftCardProvider.Get()
            .WhereEquals(nameof(GiftCardInfo.GiftCardGuid), giftCardGuid)
            .GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);

        return results.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task<GiftCardValidationResult> ValidateCodeAsync(string code, int? memberId = null, CancellationToken cancellationToken = default)
    {
        var giftCard = await GetByCodeAsync(code, cancellationToken);

        if (giftCard == null)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                ErrorCode = GiftCardValidationError.NotFound,
                ErrorMessage = "Gift card code not found."
            };
        }

        // Check if disabled
        if (!giftCard.GiftCardEnabled)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.Disabled,
                ErrorMessage = "This gift card is not active."
            };
        }

        // Check status
        if (giftCard.GiftCardStatus == GiftCardStatuses.FullyRedeemed)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.FullyRedeemed,
                ErrorMessage = "This gift card has already been fully redeemed."
            };
        }

        if (giftCard.GiftCardStatus == GiftCardStatuses.Cancelled)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.Cancelled,
                ErrorMessage = "This gift card has been cancelled."
            };
        }

        if (giftCard.GiftCardStatus == GiftCardStatuses.Expired)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.Expired,
                ErrorMessage = "This gift card has expired."
            };
        }

        // Check expiration date
        if (giftCard.GiftCardExpiresAt.HasValue && giftCard.GiftCardExpiresAt.Value < DateTime.UtcNow)
        {
            // Update status to expired
            giftCard.GiftCardStatus = GiftCardStatuses.Expired;
            giftCard.GiftCardLastModified = DateTime.UtcNow;
            await giftCardProvider.SetAsync(giftCard, cancellationToken);

            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.Expired,
                ErrorMessage = "This gift card has expired."
            };
        }

        // Check balance
        if (giftCard.GiftCardRemainingBalance <= 0)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.NoBalance,
                ErrorMessage = "This gift card has no remaining balance."
            };
        }

        // Check recipient restriction
        if (giftCard.GiftCardRecipientMemberID.HasValue &&
            memberId.HasValue &&
            giftCard.GiftCardRecipientMemberID.Value != memberId.Value)
        {
            return new GiftCardValidationResult
            {
                IsValid = false,
                GiftCard = giftCard,
                ErrorCode = GiftCardValidationError.WrongRecipient,
                ErrorMessage = "This gift card is not valid for your account."
            };
        }

        return new GiftCardValidationResult
        {
            IsValid = true,
            GiftCard = giftCard,
            AvailableBalance = giftCard.GiftCardRemainingBalance
        };
    }

    /// <inheritdoc/>
    public async Task<GiftCardRedemptionResult> RedeemToWalletAsync(string code, int memberId, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCodeAsync(code, memberId, cancellationToken);
        if (!validation.IsValid)
        {
            return GiftCardRedemptionResult.Failed(validation.ErrorMessage ?? "Gift card validation failed.");
        }

        var giftCard = validation.GiftCard!;
        var amount = giftCard.GiftCardRemainingBalance;

        try
        {
            // Get currency code
            var currency = await currencyProvider.GetAsync(giftCard.GiftCardCurrencyID, cancellationToken);
            var currencyCode = currency?.CurrencyCode ?? _defaultCurrency;

            // Deposit to wallet using GiftCard wallet type
            var depositRequest = new WalletDepositRequest
            {
                MemberId = memberId,
                WalletType = WalletTypes.GiftCard,
                Amount = amount,
                CurrencyCode = currencyCode,
                Description = $"Gift card redemption: {giftCard.GiftCardCode}",
                Reference = giftCard.GiftCardCode,
                IdempotencyKey = $"giftcard-redeem-{giftCard.GiftCardCode}-{memberId}"
            };
            var walletResult = await walletService.DepositAsync(depositRequest, cancellationToken);

            if (!walletResult.Success)
            {
                return GiftCardRedemptionResult.Failed(walletResult.ErrorMessage ?? "Failed to deposit to wallet.");
            }

            // Update gift card
            giftCard.GiftCardRemainingBalance = 0;
            giftCard.GiftCardStatus = GiftCardStatuses.FullyRedeemed;
            giftCard.GiftCardRedeemedByMemberID = memberId;
            giftCard.GiftCardRedeemedWhen = DateTime.UtcNow;
            giftCard.GiftCardLastModified = DateTime.UtcNow;

            await giftCardProvider.SetAsync(giftCard, cancellationToken);

            logger.LogDebug(
                "Gift card {Code} redeemed to wallet for member {MemberId}, amount {Amount}",
                giftCard.GiftCardCode,
                memberId,
                amount);

            return GiftCardRedemptionResult.Succeeded(giftCard.GiftCardCode, amount, 0);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to redeem gift card {Code} to wallet", code);
            return GiftCardRedemptionResult.Failed($"Failed to redeem gift card: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<GiftCardRedemptionResult> ApplyToOrderAsync(string code, int orderId, decimal amount, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateCodeAsync(code, null, cancellationToken);
        if (!validation.IsValid)
        {
            return GiftCardRedemptionResult.Failed(validation.ErrorMessage ?? "Gift card validation failed.");
        }

        var giftCard = validation.GiftCard!;

        // Ensure amount doesn't exceed remaining balance
        var actualAmount = Math.Min(amount, giftCard.GiftCardRemainingBalance);

        try
        {
            // Deduct from gift card balance
            giftCard.GiftCardRemainingBalance -= actualAmount;

            // Update status based on remaining balance
            if (giftCard.GiftCardRemainingBalance <= 0)
            {
                giftCard.GiftCardStatus = GiftCardStatuses.FullyRedeemed;
                giftCard.GiftCardRedeemedWhen = DateTime.UtcNow;
            }
            else
            {
                giftCard.GiftCardStatus = GiftCardStatuses.PartiallyRedeemed;
            }

            // Add order reference to notes
            var existingNotes = giftCard.GiftCardNotes ?? string.Empty;
            giftCard.GiftCardNotes = $"{existingNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Applied {actualAmount:C} to order #{orderId}".Trim();
            giftCard.GiftCardLastModified = DateTime.UtcNow;

            await giftCardProvider.SetAsync(giftCard, cancellationToken);

            logger.LogDebug(
                "Gift card {Code} applied {Amount} to order {OrderId}, remaining balance: {Remaining}",
                giftCard.GiftCardCode,
                actualAmount,
                orderId,
                giftCard.GiftCardRemainingBalance);

            return GiftCardRedemptionResult.Succeeded(giftCard.GiftCardCode, actualAmount, giftCard.GiftCardRemainingBalance);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply gift card {Code} to order {OrderId}", code, orderId);
            return GiftCardRedemptionResult.Failed($"Failed to apply gift card: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<GiftCardInfo>> GetByRecipientAsync(int memberId, bool includeRedeemed = false, CancellationToken cancellationToken = default)
    {
        var query = giftCardProvider.Get()
            .WhereEquals(nameof(GiftCardInfo.GiftCardRecipientMemberID), memberId)
            .WhereTrue(nameof(GiftCardInfo.GiftCardEnabled));

        if (!includeRedeemed)
        {
            query = query.WhereNotEquals(nameof(GiftCardInfo.GiftCardStatus), GiftCardStatuses.FullyRedeemed);
        }

        return await query.GetEnumerableTypedResultAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GiftCardResult> CancelAsync(int giftCardId, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var giftCard = await GetByIdAsync(giftCardId, cancellationToken);
            if (giftCard == null)
            {
                return GiftCardResult.Failed("Gift card not found.");
            }

            if (giftCard.GiftCardStatus == GiftCardStatuses.FullyRedeemed)
            {
                return GiftCardResult.Failed("Cannot cancel a fully redeemed gift card.");
            }

            giftCard.GiftCardStatus = GiftCardStatuses.Cancelled;
            giftCard.GiftCardEnabled = false;
            giftCard.GiftCardLastModified = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(reason))
            {
                var existingNotes = giftCard.GiftCardNotes ?? string.Empty;
                giftCard.GiftCardNotes = $"{existingNotes}\n[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] Cancelled: {reason}".Trim();
            }

            await giftCardProvider.SetAsync(giftCard, cancellationToken);

            logger.LogDebug("Gift card {Code} cancelled. Reason: {Reason}", giftCard.GiftCardCode, reason ?? "Not specified");

            return GiftCardResult.Succeeded(giftCard);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to cancel gift card {GiftCardId}", giftCardId);
            return GiftCardResult.Failed($"Failed to cancel gift card: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public string GenerateUniqueCode()
    {
        var random = Random.Shared;

        string GenerateSegment(int length)
        {
            return new string(Enumerable.Range(0, length)
                .Select(_ => CodeCharacters[random.Next(CodeCharacters.Length)])
                .ToArray());
        }

        return $"GIFT-{GenerateSegment(4)}-{GenerateSegment(4)}";
    }

    /// <summary>
    /// Generates a unique code and verifies it doesn't already exist.
    /// </summary>
    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            var code = GenerateUniqueCode();
            var existing = await GetByCodeAsync(code, cancellationToken);

            if (existing == null)
            {
                return code;
            }
        }

        // Fallback with GUID suffix for guaranteed uniqueness
        return $"GIFT-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Builds the notes field from the request.
    /// </summary>
    private static string BuildNotes(CreateGiftCardRequest request)
    {
        var notes = new List<string>();

        if (request.SourceOrderId.HasValue)
        {
            notes.Add($"Source Order: #{request.SourceOrderId}");
        }

        if (!string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            notes.Add($"Recipient Email: {request.RecipientEmail}");
        }

        if (!string.IsNullOrWhiteSpace(request.RecipientName))
        {
            notes.Add($"Recipient Name: {request.RecipientName}");
        }

        if (!string.IsNullOrWhiteSpace(request.PersonalMessage))
        {
            notes.Add($"Message: {request.PersonalMessage}");
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            notes.Add(request.Notes);
        }

        return notes.Count > 0 ? string.Join("\n", notes) : string.Empty;
    }
}
