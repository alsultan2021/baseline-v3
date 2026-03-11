using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.GiftCardInfo), Baseline.Ecommerce.Models.GiftCardInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Gift card info object for storing gift card definitions and balances.
/// Gift cards are issued with a unique code and can be redeemed for wallet credit.
/// </summary>
public class GiftCardInfo : AbstractInfo<GiftCardInfo, IInfoProvider<GiftCardInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.giftcard";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<GiftCardInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.GiftCard",
        idColumn: nameof(GiftCardID),
        timeStampColumn: nameof(GiftCardLastModified),
        guidColumn: nameof(GiftCardGuid),
        codeNameColumn: nameof(GiftCardCode),
        displayNameColumn: nameof(GiftCardCode),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true,
        DependsOn =
        [
            new ObjectDependency(nameof(GiftCardCurrencyID), CurrencyInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    #region Properties

    /// <summary>
    /// Gift card ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int GiftCardID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(GiftCardID)), 0);
        set => SetValue(nameof(GiftCardID), value);
    }

    /// <summary>
    /// Gift card GUID for external references.
    /// </summary>
    [DatabaseField]
    public virtual Guid GiftCardGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(GiftCardGuid)), Guid.Empty);
        set => SetValue(nameof(GiftCardGuid), value);
    }

    /// <summary>
    /// Unique gift card redemption code.
    /// This is what customers enter to redeem the gift card.
    /// </summary>
    [DatabaseField]
    public virtual string GiftCardCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(GiftCardCode)), string.Empty);
        set => SetValue(nameof(GiftCardCode), value);
    }

    /// <summary>
    /// Initial amount loaded on the gift card at creation.
    /// </summary>
    [DatabaseField]
    public virtual decimal GiftCardInitialAmount
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(GiftCardInitialAmount)), 0m);
        set => SetValue(nameof(GiftCardInitialAmount), value);
    }

    /// <summary>
    /// Remaining balance on the gift card.
    /// </summary>
    [DatabaseField]
    public virtual decimal GiftCardRemainingBalance
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(GiftCardRemainingBalance)), 0m);
        set => SetValue(nameof(GiftCardRemainingBalance), value);
    }

    /// <summary>
    /// Currency ID for this gift card's balance.
    /// References Baseline_Currency.CurrencyID.
    /// </summary>
    [DatabaseField]
    public virtual int GiftCardCurrencyID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(GiftCardCurrencyID)), 0);
        set => SetValue(nameof(GiftCardCurrencyID), value);
    }

    /// <summary>
    /// Optional Member ID if the gift card was purchased for or assigned to a specific member.
    /// Null means the gift card can be redeemed by anyone with the code.
    /// </summary>
    [DatabaseField]
    public virtual int? GiftCardRecipientMemberID
    {
        get
        {
            object? val = GetValue(nameof(GiftCardRecipientMemberID));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetInteger(val, 0);
        }
        set => SetValue(nameof(GiftCardRecipientMemberID), value);
    }

    /// <summary>
    /// Member ID who redeemed the gift card.
    /// Set when the gift card is fully redeemed.
    /// </summary>
    [DatabaseField]
    public virtual int? GiftCardRedeemedByMemberID
    {
        get
        {
            object? val = GetValue(nameof(GiftCardRedeemedByMemberID));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetInteger(val, 0);
        }
        set => SetValue(nameof(GiftCardRedeemedByMemberID), value);
    }

    /// <summary>
    /// Gift card status: "Active", "PartiallyRedeemed", "FullyRedeemed", "Expired", "Cancelled".
    /// </summary>
    [DatabaseField]
    public virtual string GiftCardStatus
    {
        get => ValidationHelper.GetString(GetValue(nameof(GiftCardStatus)), GiftCardStatuses.Active);
        set => SetValue(nameof(GiftCardStatus), value);
    }

    /// <summary>
    /// Optional expiration date for the gift card.
    /// After this date, the gift card cannot be redeemed.
    /// </summary>
    [DatabaseField]
    public virtual DateTime? GiftCardExpiresAt
    {
        get
        {
            object? val = GetValue(nameof(GiftCardExpiresAt));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetDateTime(val, DateTime.MaxValue);
        }
        set => SetValue(nameof(GiftCardExpiresAt), value);
    }

    /// <summary>
    /// When the gift card was redeemed (first use).
    /// </summary>
    [DatabaseField]
    public virtual DateTime? GiftCardRedeemedWhen
    {
        get
        {
            object? val = GetValue(nameof(GiftCardRedeemedWhen));
            return val == null || val == DBNull.Value ? null : ValidationHelper.GetDateTime(val, DateTime.MinValue);
        }
        set => SetValue(nameof(GiftCardRedeemedWhen), value);
    }

    /// <summary>
    /// Whether the gift card is enabled for redemption.
    /// </summary>
    [DatabaseField]
    public virtual bool GiftCardEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(GiftCardEnabled)), true);
        set => SetValue(nameof(GiftCardEnabled), value);
    }

    /// <summary>
    /// When the gift card was created.
    /// </summary>
    [DatabaseField]
    public virtual DateTime GiftCardCreatedWhen
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(GiftCardCreatedWhen)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(GiftCardCreatedWhen), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime GiftCardLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(GiftCardLastModified)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(GiftCardLastModified), value);
    }

    /// <summary>
    /// Optional notes for admin use.
    /// </summary>
    [DatabaseField]
    public virtual string? GiftCardNotes
    {
        get => ValidationHelper.GetString(GetValue(nameof(GiftCardNotes)), null);
        set => SetValue(nameof(GiftCardNotes), value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="GiftCardInfo"/>.
    /// </summary>
    public GiftCardInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from the given DataRow.
    /// </summary>
    public GiftCardInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    #endregion

    #region Methods

    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject() => Provider.Delete(this);

    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject() => Provider.Set(this);

    /// <summary>
    /// Checks if the gift card is valid for redemption.
    /// </summary>
    /// <returns>True if the gift card can be redeemed.</returns>
    public bool IsRedeemable()
    {
        if (!GiftCardEnabled)
            return false;

        if (GiftCardStatus == GiftCardStatuses.FullyRedeemed ||
            GiftCardStatus == GiftCardStatuses.Cancelled ||
            GiftCardStatus == GiftCardStatuses.Expired)
            return false;

        if (GiftCardRemainingBalance <= 0)
            return false;

        if (GiftCardExpiresAt.HasValue && GiftCardExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    /// Gets the available balance that can be redeemed.
    /// </summary>
    /// <returns>Available balance or 0 if not redeemable.</returns>
    public decimal GetAvailableBalance()
    {
        return IsRedeemable() ? GiftCardRemainingBalance : 0m;
    }

    #endregion
}

/// <summary>
/// Gift card status constants.
/// </summary>
public static class GiftCardStatuses
{
    /// <summary>
    /// Gift card is active and ready for use.
    /// </summary>
    public const string Active = "Active";

    /// <summary>
    /// Gift card has been partially redeemed but still has balance.
    /// </summary>
    public const string PartiallyRedeemed = "PartiallyRedeemed";

    /// <summary>
    /// Gift card has been fully redeemed (balance is 0).
    /// </summary>
    public const string FullyRedeemed = "FullyRedeemed";

    /// <summary>
    /// Gift card has expired.
    /// </summary>
    public const string Expired = "Expired";

    /// <summary>
    /// Gift card has been cancelled by admin.
    /// </summary>
    public const string Cancelled = "Cancelled";
}
