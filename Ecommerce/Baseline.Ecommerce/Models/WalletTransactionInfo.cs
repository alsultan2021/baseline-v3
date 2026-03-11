using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.WalletTransactionInfo), Baseline.Ecommerce.Models.WalletTransactionInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Wallet transaction info for tracking all balance changes.
/// Implements an append-only ledger for auditability.
/// Each transaction records the amount, type, and resulting balance.
/// </summary>
public class WalletTransactionInfo : AbstractInfo<WalletTransactionInfo, IInfoProvider<WalletTransactionInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.wallettransaction";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<WalletTransactionInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.WalletTransaction",
        idColumn: nameof(TransactionID),
        timeStampColumn: nameof(TransactionCreatedWhen),
        guidColumn: nameof(TransactionGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: nameof(TransactionWalletID),
        parentObjectType: WalletInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true,
        DependsOn =
        [
            new ObjectDependency(nameof(TransactionWalletID), WalletInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    #region Properties

    /// <summary>
    /// Transaction ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int TransactionID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TransactionID)), 0);
        set => SetValue(nameof(TransactionID), value);
    }

    /// <summary>
    /// Transaction GUID for external references.
    /// </summary>
    [DatabaseField]
    public virtual Guid TransactionGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(TransactionGuid)), Guid.Empty);
        set => SetValue(nameof(TransactionGuid), value);
    }

    /// <summary>
    /// Wallet ID this transaction belongs to.
    /// </summary>
    [DatabaseField]
    public virtual int TransactionWalletID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(TransactionWalletID)), 0);
        set => SetValue(nameof(TransactionWalletID), value);
    }

    /// <summary>
    /// Transaction type from <see cref="WalletTransactionTypes"/>.
    /// </summary>
    [DatabaseField]
    public virtual string TransactionType
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionType)), WalletTransactionTypes.Deposit);
        set => SetValue(nameof(TransactionType), value);
    }

    /// <summary>
    /// Transaction amount. Positive for credits (deposits), negative for debits (withdrawals).
    /// </summary>
    [DatabaseField]
    public virtual decimal TransactionAmount
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(TransactionAmount)), 0m);
        set => SetValue(nameof(TransactionAmount), value);
    }

    /// <summary>
    /// Wallet balance after this transaction was applied.
    /// Provides point-in-time balance for reconciliation.
    /// </summary>
    [DatabaseField]
    public virtual decimal TransactionBalanceAfter
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(TransactionBalanceAfter)), 0m);
        set => SetValue(nameof(TransactionBalanceAfter), value);
    }

    /// <summary>
    /// External reference (order number, refund ID, gift card code, etc.).
    /// </summary>
    [DatabaseField]
    public virtual string? TransactionReference
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionReference)), null);
        set => SetValue(nameof(TransactionReference), value);
    }

    /// <summary>
    /// Optional order ID for order-related transactions.
    /// References Commerce_Order.OrderID when applicable.
    /// </summary>
    [DatabaseField]
    public virtual int? TransactionOrderID
    {
        get
        {
            var val = GetValue(nameof(TransactionOrderID));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetInteger(val, 0);
        }
        set => SetValue(nameof(TransactionOrderID), value);
    }

    /// <summary>
    /// Human-readable description of the transaction.
    /// </summary>
    [DatabaseField]
    public virtual string? TransactionDescription
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionDescription)), null);
        set => SetValue(nameof(TransactionDescription), value);
    }

    /// <summary>
    /// Transaction status from <see cref="WalletTransactionStatuses"/>.
    /// </summary>
    [DatabaseField]
    public virtual string TransactionStatus
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionStatus)), WalletTransactionStatuses.Completed);
        set => SetValue(nameof(TransactionStatus), value);
    }

    /// <summary>
    /// User ID who created/approved this transaction.
    /// Null for system-generated transactions.
    /// </summary>
    [DatabaseField]
    public virtual int? TransactionCreatedBy
    {
        get
        {
            var val = GetValue(nameof(TransactionCreatedBy));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetInteger(val, 0);
        }
        set => SetValue(nameof(TransactionCreatedBy), value);
    }

    /// <summary>
    /// When the transaction was created.
    /// </summary>
    [DatabaseField]
    public virtual DateTime TransactionCreatedWhen
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(TransactionCreatedWhen)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(TransactionCreatedWhen), value);
    }

    /// <summary>
    /// Idempotency key to prevent duplicate transactions.
    /// Client provides this key to ensure the same request isn't processed twice.
    /// </summary>
    [DatabaseField]
    public virtual string? TransactionIdempotencyKey
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionIdempotencyKey)), null);
        set => SetValue(nameof(TransactionIdempotencyKey), value);
    }

    /// <summary>
    /// JSON metadata for additional transaction details.
    /// Can store original currency, conversion rate, IP address, etc.
    /// </summary>
    [DatabaseField]
    public virtual string? TransactionMetadata
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionMetadata)), null);
        set => SetValue(nameof(TransactionMetadata), value);
    }

    /// <summary>
    /// IP address of the request that created this transaction.
    /// For security audit purposes.
    /// </summary>
    [DatabaseField]
    public virtual string? TransactionIPAddress
    {
        get => ValidationHelper.GetString(GetValue(nameof(TransactionIPAddress)), null);
        set => SetValue(nameof(TransactionIPAddress), value);
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Whether this is a credit (adds to balance).
    /// </summary>
    public bool IsCredit => TransactionAmount > 0;

    /// <summary>
    /// Whether this is a debit (subtracts from balance).
    /// </summary>
    public bool IsDebit => TransactionAmount < 0;

    /// <summary>
    /// Absolute amount (always positive).
    /// </summary>
    public decimal AbsoluteAmount => Math.Abs(TransactionAmount);

    /// <summary>
    /// Whether the transaction is completed.
    /// </summary>
    public bool IsCompleted => TransactionStatus == WalletTransactionStatuses.Completed;

    /// <summary>
    /// Whether the transaction is pending.
    /// </summary>
    public bool IsPending => TransactionStatus == WalletTransactionStatuses.Pending;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="WalletTransactionInfo"/>.
    /// </summary>
    public WalletTransactionInfo() : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from the given DataRow.
    /// </summary>
    public WalletTransactionInfo(DataRow dr) : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Constructor for deserialization.
    /// </summary>
    protected WalletTransactionInfo(SerializationInfo info, StreamingContext context) : base(TYPEINFO)
    {
    }

    #endregion

    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject() => Provider.Delete(this);

    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject() => Provider.Set(this);
}

/// <summary>
/// Constants for wallet transaction types.
/// </summary>
public static class WalletTransactionTypes
{
    /// <summary>
    /// Funds deposited into the wallet.
    /// </summary>
    public const string Deposit = "Deposit";

    /// <summary>
    /// Funds withdrawn from the wallet.
    /// </summary>
    public const string Withdrawal = "Withdrawal";

    /// <summary>
    /// Funds placed on hold (reserved for pending order).
    /// </summary>
    public const string Hold = "Hold";

    /// <summary>
    /// Held funds released back to available balance.
    /// </summary>
    public const string Release = "Release";

    /// <summary>
    /// Refund credited to the wallet.
    /// </summary>
    public const string Refund = "Refund";

    /// <summary>
    /// Manual adjustment by administrator.
    /// </summary>
    public const string Adjustment = "Adjustment";

    /// <summary>
    /// Transfer between wallets.
    /// </summary>
    public const string Transfer = "Transfer";

    /// <summary>
    /// Expired funds removed from wallet.
    /// </summary>
    public const string Expiration = "Expiration";

    /// <summary>
    /// Purchase payment using wallet funds.
    /// </summary>
    public const string Purchase = "Purchase";

    /// <summary>
    /// Loyalty points earned from purchase.
    /// </summary>
    public const string LoyaltyEarn = "LoyaltyEarn";

    /// <summary>
    /// Loyalty points redeemed for discount.
    /// </summary>
    public const string LoyaltyRedeem = "LoyaltyRedeem";

    /// <summary>
    /// Promotional credit awarded.
    /// </summary>
    public const string Promotional = "Promotional";

    /// <summary>
    /// Gift card activation.
    /// </summary>
    public const string GiftCardActivation = "GiftCardActivation";

    /// <summary>
    /// All valid transaction types.
    /// </summary>
    public static readonly string[] All =
    [
        Deposit, Withdrawal, Hold, Release, Refund, Adjustment, Transfer,
        Expiration, Purchase, LoyaltyEarn, LoyaltyRedeem, Promotional, GiftCardActivation
    ];
}

/// <summary>
/// Constants for wallet transaction statuses.
/// </summary>
public static class WalletTransactionStatuses
{
    /// <summary>
    /// Transaction is pending completion (e.g., hold awaiting capture).
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// Transaction completed successfully.
    /// </summary>
    public const string Completed = "Completed";

    /// <summary>
    /// Transaction was cancelled.
    /// </summary>
    public const string Cancelled = "Cancelled";

    /// <summary>
    /// Transaction failed.
    /// </summary>
    public const string Failed = "Failed";

    /// <summary>
    /// All valid statuses.
    /// </summary>
    public static readonly string[] All = [Pending, Completed, Cancelled, Failed];
}
