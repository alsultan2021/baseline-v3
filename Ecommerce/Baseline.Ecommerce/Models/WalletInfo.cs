using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.WalletInfo), Baseline.Ecommerce.Models.WalletInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Wallet info object for storing member account balances.
/// Supports store credit, loyalty points, prepaid funds, and gift cards.
/// Wallets are currency-specific and linked to members via MemberID.
/// </summary>
public class WalletInfo : AbstractInfo<WalletInfo, IInfoProvider<WalletInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.wallet";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<WalletInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.Wallet",
        idColumn: nameof(WalletID),
        timeStampColumn: nameof(WalletLastModified),
        guidColumn: nameof(WalletGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true,
        DependsOn =
        [
            new ObjectDependency(nameof(WalletCurrencyID), CurrencyInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    #region Properties

    /// <summary>
    /// Wallet ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int WalletID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(WalletID)), 0);
        set => SetValue(nameof(WalletID), value);
    }

    /// <summary>
    /// Wallet GUID for external references.
    /// </summary>
    [DatabaseField]
    public virtual Guid WalletGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(WalletGuid)), Guid.Empty);
        set => SetValue(nameof(WalletGuid), value);
    }

    /// <summary>
    /// Member ID who owns this wallet.
    /// References CMS_Member.MemberID.
    /// </summary>
    [DatabaseField]
    public virtual int WalletMemberID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(WalletMemberID)), 0);
        set => SetValue(nameof(WalletMemberID), value);
    }

    /// <summary>
    /// Currency ID for this wallet's balance.
    /// References Baseline_Currency.CurrencyID.
    /// </summary>
    [DatabaseField]
    public virtual int WalletCurrencyID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(WalletCurrencyID)), 0);
        set => SetValue(nameof(WalletCurrencyID), value);
    }

    /// <summary>
    /// Current total balance in the wallet.
    /// </summary>
    [DatabaseField]
    public virtual decimal WalletBalance
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(WalletBalance)), 0m);
        set => SetValue(nameof(WalletBalance), value);
    }

    /// <summary>
    /// Balance on hold (reserved for pending transactions).
    /// Available balance = WalletBalance - WalletHeldBalance.
    /// </summary>
    [DatabaseField]
    public virtual decimal WalletHeldBalance
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(WalletHeldBalance)), 0m);
        set => SetValue(nameof(WalletHeldBalance), value);
    }

    /// <summary>
    /// Optional credit limit for credit-type wallets.
    /// When set, allows spending beyond current balance up to this limit.
    /// </summary>
    [DatabaseField]
    public virtual decimal? WalletCreditLimit
    {
        get
        {
            var val = GetValue(nameof(WalletCreditLimit));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetDecimal(val, 0m);
        }
        set => SetValue(nameof(WalletCreditLimit), value);
    }

    /// <summary>
    /// Wallet type: "StoreCredit", "LoyaltyPoints", "PrepaidFunds", "GiftCard".
    /// </summary>
    [DatabaseField]
    public virtual string WalletType
    {
        get => ValidationHelper.GetString(GetValue(nameof(WalletType)), WalletTypes.StoreCredit);
        set => SetValue(nameof(WalletType), value);
    }

    /// <summary>
    /// Whether the wallet is enabled for transactions.
    /// </summary>
    [DatabaseField]
    public virtual bool WalletEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(WalletEnabled)), true);
        set => SetValue(nameof(WalletEnabled), value);
    }

    /// <summary>
    /// Whether the wallet is frozen (fraud prevention, disputes).
    /// Frozen wallets cannot perform any transactions.
    /// </summary>
    [DatabaseField]
    public virtual bool WalletFrozen
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(WalletFrozen)), false);
        set => SetValue(nameof(WalletFrozen), value);
    }

    /// <summary>
    /// Reason for freezing the wallet (if frozen).
    /// </summary>
    [DatabaseField]
    public virtual string? WalletFreezeReason
    {
        get => ValidationHelper.GetString(GetValue(nameof(WalletFreezeReason)), null);
        set => SetValue(nameof(WalletFreezeReason), value);
    }

    /// <summary>
    /// Optional expiration date for time-limited balances (e.g., gift cards, promotional credit).
    /// </summary>
    [DatabaseField]
    public virtual DateTime? WalletExpiresAt
    {
        get
        {
            var val = GetValue(nameof(WalletExpiresAt));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetDateTime(val, DateTime.MaxValue);
        }
        set => SetValue(nameof(WalletExpiresAt), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime WalletLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(WalletLastModified)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(WalletLastModified), value);
    }

    /// <summary>
    /// When the wallet was created.
    /// </summary>
    [DatabaseField]
    public virtual DateTime WalletCreatedWhen
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(WalletCreatedWhen)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(WalletCreatedWhen), value);
    }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the available balance (total balance minus held amount).
    /// </summary>
    public decimal AvailableBalance => WalletBalance - WalletHeldBalance;

    /// <summary>
    /// Gets the total spending power (available balance plus credit limit if applicable).
    /// </summary>
    public decimal SpendingPower => AvailableBalance + (WalletCreditLimit ?? 0m);

    /// <summary>
    /// Checks if the wallet can be used for transactions.
    /// </summary>
    public bool CanTransact => WalletEnabled && !WalletFrozen && !IsExpired;

    /// <summary>
    /// Checks if the wallet has expired.
    /// </summary>
    public bool IsExpired => WalletExpiresAt.HasValue && WalletExpiresAt <= DateTime.UtcNow;

    /// <summary>
    /// Checks if this is a loyalty points wallet.
    /// </summary>
    public bool IsLoyaltyPoints => WalletType == WalletTypes.LoyaltyPoints;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="WalletInfo"/>.
    /// </summary>
    public WalletInfo() : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from the given DataRow.
    /// </summary>
    public WalletInfo(DataRow dr) : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Constructor for deserialization.
    /// </summary>
    protected WalletInfo(SerializationInfo info, StreamingContext context) : base(TYPEINFO)
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
/// Constants for wallet types.
/// </summary>
public static class WalletTypes
{
    /// <summary>
    /// Store credit from returns, refunds, or promotions.
    /// </summary>
    public const string StoreCredit = "StoreCredit";

    /// <summary>
    /// Loyalty points that can be redeemed for purchases.
    /// </summary>
    public const string LoyaltyPoints = "LoyaltyPoints";

    /// <summary>
    /// Prepaid funds loaded by the customer.
    /// </summary>
    public const string PrepaidFunds = "PrepaidFunds";

    /// <summary>
    /// Gift card balance from purchased or gifted cards.
    /// </summary>
    public const string GiftCard = "GiftCard";

    /// <summary>
    /// All valid wallet types.
    /// </summary>
    public static readonly string[] All = [StoreCredit, LoyaltyPoints, PrepaidFunds, GiftCard];
}
