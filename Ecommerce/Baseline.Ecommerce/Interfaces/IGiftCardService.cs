using Baseline.Ecommerce.Models;

namespace Baseline.Ecommerce;

/// <summary>
/// Service for managing gift card operations including creation, validation, and redemption.
/// Gift cards can be created manually via admin or automatically upon purchase.
/// </summary>
public interface IGiftCardService
{
    /// <summary>
    /// Creates a new gift card with the specified parameters.
    /// Generates a unique code if not provided.
    /// </summary>
    /// <param name="request">The gift card creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the created gift card or error details.</returns>
    Task<GiftCardResult> CreateGiftCardAsync(CreateGiftCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a gift card code and returns its current state.
    /// </summary>
    /// <param name="code">The gift card redemption code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gift card if valid, null if not found.</returns>
    Task<GiftCardInfo?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a gift card by its ID.
    /// </summary>
    /// <param name="giftCardId">The gift card ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gift card if found, null otherwise.</returns>
    Task<GiftCardInfo?> GetByIdAsync(int giftCardId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a gift card by its GUID.
    /// </summary>
    /// <param name="giftCardGuid">The gift card GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gift card if found, null otherwise.</returns>
    Task<GiftCardInfo?> GetByGuidAsync(Guid giftCardGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a gift card code can be redeemed.
    /// Checks status, balance, expiration, and optional member restrictions.
    /// </summary>
    /// <param name="code">The gift card code to validate.</param>
    /// <param name="memberId">Optional member ID to validate against recipient restrictions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with details.</returns>
    Task<GiftCardValidationResult> ValidateCodeAsync(string code, int? memberId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Redeems a gift card's full remaining balance to a member's wallet.
    /// Marks the gift card as fully redeemed.
    /// </summary>
    /// <param name="code">The gift card code.</param>
    /// <param name="memberId">The member ID to credit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with redeemed amount and wallet balance.</returns>
    Task<GiftCardRedemptionResult> RedeemToWalletAsync(string code, int memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a gift card as payment toward an order.
    /// Deducts the specified amount from the gift card balance.
    /// </summary>
    /// <param name="code">The gift card code.</param>
    /// <param name="orderId">The order ID being paid.</param>
    /// <param name="amount">The amount to apply (up to remaining balance).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with applied amount and remaining balance.</returns>
    Task<GiftCardRedemptionResult> ApplyToOrderAsync(string code, int orderId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all gift cards for a specific recipient member.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="includeRedeemed">Whether to include fully redeemed cards.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of gift cards.</returns>
    Task<IEnumerable<GiftCardInfo>> GetByRecipientAsync(int memberId, bool includeRedeemed = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a gift card, preventing future redemption.
    /// </summary>
    /// <param name="giftCardId">The gift card ID to cancel.</param>
    /// <param name="reason">Optional reason for cancellation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<GiftCardResult> CancelAsync(int giftCardId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a unique gift card code in the standard format.
    /// </summary>
    /// <returns>A unique gift card code (e.g., GIFT-XXXX-XXXX).</returns>
    string GenerateUniqueCode();
}

/// <summary>
/// Request model for creating a new gift card.
/// </summary>
public record CreateGiftCardRequest
{
    /// <summary>
    /// The initial balance to load onto the gift card.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The currency ID for the gift card balance.
    /// </summary>
    public required int CurrencyId { get; init; }

    /// <summary>
    /// Optional custom code. If not provided, one will be generated.
    /// </summary>
    public string? Code { get; init; }

    /// <summary>
    /// Optional recipient member ID. If set, only this member can redeem.
    /// </summary>
    public int? RecipientMemberId { get; init; }

    /// <summary>
    /// Optional recipient email for gift card delivery.
    /// </summary>
    public string? RecipientEmail { get; init; }

    /// <summary>
    /// Optional recipient name for personalization.
    /// </summary>
    public string? RecipientName { get; init; }

    /// <summary>
    /// Optional personal message from the purchaser.
    /// </summary>
    public string? PersonalMessage { get; init; }

    /// <summary>
    /// Optional expiration date for the gift card.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// Optional order ID if the gift card was purchased.
    /// Links the gift card to its source order.
    /// </summary>
    public int? SourceOrderId { get; init; }

    /// <summary>
    /// Optional order item ID if part of a multi-item order.
    /// </summary>
    public int? SourceOrderItemId { get; init; }

    /// <summary>
    /// Admin notes for the gift card.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Result model for gift card operations.
/// </summary>
public record GiftCardResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The gift card if operation succeeded.
    /// </summary>
    public GiftCardInfo? GiftCard { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static GiftCardResult Succeeded(GiftCardInfo giftCard) => new()
    {
        Success = true,
        GiftCard = giftCard
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static GiftCardResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}

/// <summary>
/// Result model for gift card validation.
/// </summary>
public record GiftCardValidationResult
{
    /// <summary>
    /// Whether the gift card is valid for redemption.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The gift card if found.
    /// </summary>
    public GiftCardInfo? GiftCard { get; init; }

    /// <summary>
    /// The available balance for redemption.
    /// </summary>
    public decimal AvailableBalance { get; init; }

    /// <summary>
    /// Validation error message if not valid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public GiftCardValidationError? ErrorCode { get; init; }
}

/// <summary>
/// Gift card validation error codes.
/// </summary>
public enum GiftCardValidationError
{
    /// <summary>Gift card code not found.</summary>
    NotFound,

    /// <summary>Gift card has been fully redeemed.</summary>
    FullyRedeemed,

    /// <summary>Gift card has expired.</summary>
    Expired,

    /// <summary>Gift card has been cancelled.</summary>
    Cancelled,

    /// <summary>Gift card is disabled.</summary>
    Disabled,

    /// <summary>Gift card has no remaining balance.</summary>
    NoBalance,

    /// <summary>Gift card is restricted to a different member.</summary>
    WrongRecipient
}

/// <summary>
/// Result model for gift card redemption operations.
/// </summary>
public record GiftCardRedemptionResult
{
    /// <summary>
    /// Whether the redemption succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The amount that was redeemed/applied.
    /// </summary>
    public decimal RedeemedAmount { get; init; }

    /// <summary>
    /// The remaining balance on the gift card after redemption.
    /// </summary>
    public decimal RemainingBalance { get; init; }

    /// <summary>
    /// The gift card code that was redeemed.
    /// </summary>
    public string? GiftCardCode { get; init; }

    /// <summary>
    /// Error message if redemption failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful redemption result.
    /// </summary>
    public static GiftCardRedemptionResult Succeeded(string code, decimal redeemed, decimal remaining) => new()
    {
        Success = true,
        GiftCardCode = code,
        RedeemedAmount = redeemed,
        RemainingBalance = remaining
    };

    /// <summary>
    /// Creates a failed redemption result.
    /// </summary>
    public static GiftCardRedemptionResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
