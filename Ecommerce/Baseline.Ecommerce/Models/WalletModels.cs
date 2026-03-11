namespace Baseline.Ecommerce;

#region Wallet Models

/// <summary>
/// Wallet balance summary for display.
/// </summary>
public record WalletSummary
{
    /// <summary>
    /// Wallet unique identifier.
    /// </summary>
    public Guid WalletId { get; init; }

    /// <summary>
    /// Type of wallet (StoreCredit, LoyaltyPoints, etc.).
    /// </summary>
    public string WalletType { get; init; } = string.Empty;

    /// <summary>
    /// Total balance in the wallet.
    /// </summary>
    public Money Balance { get; init; } = Money.Zero();

    /// <summary>
    /// Available balance (total minus held).
    /// </summary>
    public Money AvailableBalance { get; init; } = Money.Zero();

    /// <summary>
    /// Balance currently on hold.
    /// </summary>
    public Money HeldBalance { get; init; } = Money.Zero();

    /// <summary>
    /// Whether the wallet can be used for transactions.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Whether the wallet is frozen.
    /// </summary>
    public bool IsFrozen { get; init; }

    /// <summary>
    /// Optional expiration date.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// When the wallet was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Transaction history entry for display.
/// </summary>
public record WalletTransactionSummary
{
    /// <summary>
    /// Transaction unique identifier.
    /// </summary>
    public Guid TransactionId { get; init; }

    /// <summary>
    /// Transaction type.
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Transaction amount (signed: positive = credit, negative = debit).
    /// </summary>
    public Money Amount { get; init; } = Money.Zero();

    /// <summary>
    /// Balance after this transaction.
    /// </summary>
    public Money BalanceAfter { get; init; } = Money.Zero();

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// External reference (order number, etc.).
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Transaction status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// When the transaction occurred.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Related order ID if applicable.
    /// </summary>
    public int? OrderId { get; init; }
}

#endregion

#region Wallet Operation Requests

/// <summary>
/// Request to deposit funds into a wallet.
/// </summary>
public record WalletDepositRequest
{
    /// <summary>
    /// Member to deposit funds for.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Amount to deposit (must be positive).
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code (ISO 4217).
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Type of wallet to deposit into (defaults to StoreCredit).
    /// </summary>
    public string? WalletType { get; init; }

    /// <summary>
    /// Description for the transaction.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// External reference for tracking.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Idempotency key to prevent duplicate deposits.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// User ID performing the deposit (for admin operations).
    /// </summary>
    public int? CreatedByUserId { get; init; }
}

/// <summary>
/// Request to withdraw/use funds from a wallet.
/// </summary>
public record WalletWithdrawalRequest
{
    /// <summary>
    /// Member to withdraw funds from.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Amount to withdraw (must be positive).
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code (ISO 4217).
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Type of wallet to withdraw from (null = any available).
    /// </summary>
    public string? WalletType { get; init; }

    /// <summary>
    /// Description for the transaction.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// External reference for tracking.
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Related order ID.
    /// </summary>
    public int? OrderId { get; init; }

    /// <summary>
    /// Idempotency key to prevent duplicate withdrawals.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}

/// <summary>
/// Request to hold funds for a pending transaction.
/// </summary>
public record WalletHoldRequest
{
    /// <summary>
    /// Member to hold funds for.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Amount to hold.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Type of wallet to hold funds in (null = default wallet).
    /// </summary>
    public string? WalletType { get; init; }

    /// <summary>
    /// Reference for the hold (usually pending order ID).
    /// </summary>
    public string? Reference { get; init; }

    /// <summary>
    /// Related order ID for checkout holds.
    /// </summary>
    public int? OrderId { get; init; }

    /// <summary>
    /// How long to hold the funds.
    /// </summary>
    public TimeSpan? HoldDuration { get; init; }
}

/// <summary>
/// Request to adjust wallet balance (admin operation).
/// </summary>
public record WalletAdjustmentRequest
{
    /// <summary>
    /// Member ID to adjust wallet for.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Wallet to adjust (optional, if MemberId is provided).
    /// </summary>
    public Guid? WalletId { get; init; }

    /// <summary>
    /// Type of wallet to adjust (null = default wallet).
    /// </summary>
    public string? WalletType { get; init; }

    /// <summary>
    /// Adjustment amount (positive to add, negative to remove).
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Reason for the adjustment (required for audit).
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Admin user making the adjustment.
    /// </summary>
    public int AdminUserId { get; init; }
}

#endregion

#region Wallet Operation Results

/// <summary>
/// Result of a wallet operation.
/// </summary>
public record WalletOperationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Alias for Success for consistency with other result types.
    /// </summary>
    public bool IsSuccess => Success;

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Transaction ID if a transaction was created.
    /// </summary>
    public Guid? TransactionId { get; init; }

    /// <summary>
    /// New total balance after the operation.
    /// </summary>
    public decimal NewBalance { get; init; }

    /// <summary>
    /// Available balance after the operation.
    /// </summary>
    public decimal AvailableBalance { get; init; }

    /// <summary>
    /// Updated wallet summary after the operation.
    /// </summary>
    public WalletSummary? Wallet { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static WalletOperationResult Succeeded(Guid transactionId, decimal newBalance, decimal availableBalance) =>
        new()
        {
            Success = true,
            TransactionId = transactionId,
            NewBalance = newBalance,
            AvailableBalance = availableBalance
        };

    /// <summary>
    /// Creates a successful result with wallet summary.
    /// </summary>
    public static WalletOperationResult Succeeded(WalletSummary wallet) =>
        new()
        {
            Success = true,
            Wallet = wallet,
            NewBalance = wallet.Balance.Amount,
            AvailableBalance = wallet.AvailableBalance.Amount
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static WalletOperationResult Failed(string error, string? errorCode = null) =>
        new()
        {
            Success = false,
            ErrorMessage = error,
            ErrorCode = errorCode
        };
}

/// <summary>
/// Common wallet error codes.
/// </summary>
public static class WalletErrorCodes
{
    public const string InsufficientFunds = "INSUFFICIENT_FUNDS";
    public const string WalletNotFound = "WALLET_NOT_FOUND";
    public const string WalletFrozen = "WALLET_FROZEN";
    public const string WalletDisabled = "WALLET_DISABLED";
    public const string WalletExpired = "WALLET_EXPIRED";
    public const string InvalidAmount = "INVALID_AMOUNT";
    public const string DuplicateTransaction = "DUPLICATE_TRANSACTION";
    public const string CurrencyMismatch = "CURRENCY_MISMATCH";
    public const string TransactionFailed = "TRANSACTION_FAILED";
    public const string MemberNotFound = "MEMBER_NOT_FOUND";
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string InvalidMember = "INVALID_MEMBER";
}

/// <summary>
/// Result of wallet validation.
/// </summary>
public record WalletValidationResult
{
    /// <summary>
    /// Default constructor.
    /// </summary>
    public WalletValidationResult() { }

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    public WalletValidationResult(bool isValid, string? errorMessage, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList();
    }

    /// <summary>
    /// Whether the wallet is valid for transactions.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors if invalid.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    /// Creates a valid result.
    /// </summary>
    public static WalletValidationResult Valid() => new() { IsValid = true };

    /// <summary>
    /// Creates an invalid result with errors.
    /// </summary>
    public static WalletValidationResult Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors };
}

#endregion

#region Checkout Integration Models

/// <summary>
/// Available wallet payment options for checkout.
/// </summary>
public record WalletPaymentOptions
{
    /// <summary>
    /// Total available balance across all wallets (in order currency).
    /// </summary>
    public Money TotalAvailable { get; init; } = Money.Zero();

    /// <summary>
    /// Individual wallet options.
    /// </summary>
    public IReadOnlyList<WalletPaymentOption> Wallets { get; init; } = [];

    /// <summary>
    /// Whether wallet balance can cover the full order amount.
    /// </summary>
    public bool CanPayFullAmount { get; init; }

    /// <summary>
    /// Maximum amount that can be applied from wallets.
    /// </summary>
    public Money MaxApplicable { get; init; } = Money.Zero();

    /// <summary>
    /// Suggested amount to apply (e.g., full balance or order total).
    /// </summary>
    public Money SuggestedAmount { get; init; } = Money.Zero();
}

/// <summary>
/// Individual wallet payment option.
/// </summary>
public record WalletPaymentOption
{
    /// <summary>
    /// Wallet identifier.
    /// </summary>
    public Guid WalletId { get; init; }

    /// <summary>
    /// Wallet type for display.
    /// </summary>
    public string WalletType { get; init; } = string.Empty;

    /// <summary>
    /// Display name for the wallet type.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Available balance in this wallet.
    /// </summary>
    public Money AvailableBalance { get; init; } = Money.Zero();

    /// <summary>
    /// Whether this wallet is recommended for use.
    /// </summary>
    public bool IsRecommended { get; init; }

    /// <summary>
    /// Priority order for using this wallet (lower = first).
    /// </summary>
    public int Priority { get; init; }
}

/// <summary>
/// Request to apply wallet payment to an order.
/// </summary>
public record ApplyWalletPaymentRequest
{
    /// <summary>
    /// Member applying the payment.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Order to apply payment to.
    /// </summary>
    public int OrderId { get; init; }

    /// <summary>
    /// Total amount to apply from wallets.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Currency code for the payment.
    /// </summary>
    public string CurrencyCode { get; init; } = "USD";

    /// <summary>
    /// Optional specific allocations across wallets.
    /// If not provided, system will allocate automatically.
    /// </summary>
    public IReadOnlyList<WalletPaymentAllocation>? Allocations { get; init; }
}

/// <summary>
/// Allocation of payment across specific wallets.
/// </summary>
public record WalletPaymentAllocation
{
    /// <summary>
    /// Wallet to draw from.
    /// </summary>
    public Guid WalletId { get; init; }

    /// <summary>
    /// Amount to draw from this wallet.
    /// </summary>
    public decimal Amount { get; init; }
}

/// <summary>
/// Result of applying wallet payment.
/// </summary>
public record WalletPaymentResult
{
    /// <summary>
    /// Whether the payment was applied successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Amount successfully applied from wallets.
    /// </summary>
    public Money AmountApplied { get; init; } = Money.Zero();

    /// <summary>
    /// Remaining order balance to be paid by other methods.
    /// </summary>
    public Money RemainingOrderBalance { get; init; } = Money.Zero();

    /// <summary>
    /// Transaction IDs for each wallet used.
    /// </summary>
    public IReadOnlyList<Guid> TransactionIds { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static WalletPaymentResult Succeeded(Money applied, Money remaining, IReadOnlyList<Guid> transactions) =>
        new()
        {
            Success = true,
            AmountApplied = applied,
            RemainingOrderBalance = remaining,
            TransactionIds = transactions
        };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static WalletPaymentResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

#endregion

#region Loyalty Points Models

/// <summary>
/// Loyalty points balance information.
/// </summary>
public record LoyaltyPointsBalance
{
    /// <summary>
    /// Total points in the account.
    /// </summary>
    public int TotalPoints { get; init; }

    /// <summary>
    /// Points available for redemption.
    /// </summary>
    public int AvailablePoints { get; init; }

    /// <summary>
    /// Points pending from recent orders (not yet redeemable).
    /// </summary>
    public int PendingPoints { get; init; }

    /// <summary>
    /// Points expiring in the current month.
    /// </summary>
    public int PointsExpiringThisMonth { get; init; }

    /// <summary>
    /// Equivalent monetary value of available points.
    /// </summary>
    public Money EquivalentValue { get; init; } = Money.Zero();

    /// <summary>
    /// Points earned in the current year (for tier calculation).
    /// </summary>
    public int PointsEarnedThisYear { get; init; }

    /// <summary>
    /// Points redeemed in the current year.
    /// </summary>
    public int PointsRedeemedThisYear { get; init; }
}

/// <summary>
/// Loyalty tier information.
/// </summary>
public record LoyaltyTier
{
    /// <summary>
    /// Tier name (e.g., "Bronze", "Silver", "Gold", "Platinum").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Tier level (1 = lowest).
    /// </summary>
    public int Level { get; init; }

    /// <summary>
    /// Points multiplier for earning (e.g., 1.5 = 50% bonus).
    /// </summary>
    public decimal PointsMultiplier { get; init; } = 1m;

    /// <summary>
    /// Points needed to reach the next tier.
    /// </summary>
    public int PointsToNextTier { get; init; }

    /// <summary>
    /// Minimum points required to maintain this tier.
    /// </summary>
    public int MinimumPoints { get; init; }

    /// <summary>
    /// Benefits included in this tier.
    /// </summary>
    public IReadOnlyList<string> Benefits { get; init; } = [];

    /// <summary>
    /// Tier icon or badge URL.
    /// </summary>
    public string? BadgeUrl { get; init; }
}

/// <summary>
/// Request to award bonus points.
/// </summary>
public record AwardBonusPointsRequest
{
    /// <summary>
    /// Member to award points to.
    /// </summary>
    public int MemberId { get; init; }

    /// <summary>
    /// Number of points to award.
    /// </summary>
    public int Points { get; init; }

    /// <summary>
    /// Reason for the award.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Category of bonus (referral, birthday, promotion, etc.).
    /// </summary>
    public string? BonusCategory { get; init; }

    /// <summary>
    /// Optional expiration for these points.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }
}

#endregion
