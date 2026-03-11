using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.PromotionInfo), Baseline.Ecommerce.Models.PromotionInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Promotion info object for storing catalog and order promotions.
/// Supports percentage and fixed amount discounts.
/// </summary>
public class PromotionInfo : AbstractInfo<PromotionInfo, IInfoProvider<PromotionInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.promotion";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<PromotionInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.Promotion",
        idColumn: nameof(PromotionID),
        timeStampColumn: nameof(PromotionLastModified),
        guidColumn: nameof(PromotionGuid),
        codeNameColumn: nameof(PromotionName),
        displayNameColumn: nameof(PromotionDisplayName),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>
    /// Promotion ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int PromotionID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionID)), 0);
        set => SetValue(nameof(PromotionID), value);
    }

    /// <summary>
    /// Promotion GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid PromotionGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(PromotionGuid)), Guid.Empty);
        set => SetValue(nameof(PromotionGuid), value);
    }

    /// <summary>
    /// Promotion code name.
    /// </summary>
    [DatabaseField]
    public virtual string PromotionName
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionName)), string.Empty);
        set => SetValue(nameof(PromotionName), value);
    }

    /// <summary>
    /// Promotion display name.
    /// </summary>
    [DatabaseField]
    public virtual string PromotionDisplayName
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionDisplayName)), string.Empty);
        set => SetValue(nameof(PromotionDisplayName), value);
    }

    /// <summary>
    /// Promotion description.
    /// </summary>
    [DatabaseField]
    public virtual string PromotionDescription
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionDescription)), string.Empty);
        set => SetValue(nameof(PromotionDescription), value);
    }

    /// <summary>
    /// Promotion type (Catalog = 0, Order = 1).
    /// </summary>
    [DatabaseField]
    public virtual int PromotionType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionType)), 0);
        set => SetValue(nameof(PromotionType), value);
    }

    /// <summary>
    /// Discount type (Percentage = 0, FixedAmount = 1).
    /// </summary>
    [DatabaseField]
    public virtual int PromotionDiscountType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionDiscountType)), 0);
        set => SetValue(nameof(PromotionDiscountType), value);
    }

    /// <summary>
    /// Discount value (percentage or amount).
    /// </summary>
    [DatabaseField]
    public virtual decimal PromotionDiscountValue
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(PromotionDiscountValue)), 0m);
        set => SetValue(nameof(PromotionDiscountValue), value);
    }

    /// <summary>
    /// Promotion active from date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime PromotionActiveFrom
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(PromotionActiveFrom)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(PromotionActiveFrom), value);
    }

    /// <summary>
    /// Promotion active to date (optional).
    /// </summary>
    [DatabaseField]
    public virtual DateTime? PromotionActiveTo
    {
        get
        {
            var val = GetValue(nameof(PromotionActiveTo));
            return val is null or DBNull ? null : ValidationHelper.GetDateTime(val, DateTimeHelper.ZERO_TIME);
        }
        set => SetValue(nameof(PromotionActiveTo), value, value.HasValue);
    }

    /// <summary>
    /// Whether the promotion is enabled.
    /// </summary>
    [DatabaseField]
    public virtual bool PromotionEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(PromotionEnabled)), true);
        set => SetValue(nameof(PromotionEnabled), value);
    }

    /// <summary>
    /// Minimum requirement type for order promotions (None = 0, MinAmount = 1, MinQuantity = 2).
    /// </summary>
    [DatabaseField]
    public virtual int PromotionMinimumRequirementType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionMinimumRequirementType)), 0);
        set => SetValue(nameof(PromotionMinimumRequirementType), value);
    }

    /// <summary>
    /// Minimum requirement value.
    /// </summary>
    [DatabaseField]
    public virtual decimal? PromotionMinimumRequirementValue
    {
        get
        {
            var val = GetValue(nameof(PromotionMinimumRequirementValue));
            return val is null or DBNull ? null : ValidationHelper.GetDecimal(val, 0m);
        }
        set => SetValue(nameof(PromotionMinimumRequirementValue), value, value.HasValue);
    }

    /// <summary>
    /// Target categories (JSON array of taxonomy codes).
    /// </summary>
    [DatabaseField]
    public virtual string PromotionTargetCategories
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionTargetCategories)), string.Empty);
        set => SetValue(nameof(PromotionTargetCategories), value);
    }

    /// <summary>
    /// Target product IDs (JSON array of content item IDs).
    /// </summary>
    [DatabaseField]
    public virtual string PromotionTargetProducts
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionTargetProducts)), string.Empty);
        set => SetValue(nameof(PromotionTargetProducts), value);
    }

    /// <summary>
    /// Rule type identifier (for custom rules).
    /// </summary>
    [DatabaseField]
    public virtual string PromotionRuleType
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionRuleType)), string.Empty);
        set => SetValue(nameof(PromotionRuleType), value);
    }

    /// <summary>
    /// Rule properties (JSON).
    /// </summary>
    [DatabaseField]
    public virtual string PromotionRuleProperties
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionRuleProperties)), string.Empty);
        set => SetValue(nameof(PromotionRuleProperties), value);
    }

    /// <summary>
    /// Display order.
    /// </summary>
    [DatabaseField]
    public virtual int PromotionOrder
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionOrder)), 0);
        set => SetValue(nameof(PromotionOrder), value);
    }

    /// <summary>
    /// Total redemption count.
    /// </summary>
    [DatabaseField]
    public virtual int PromotionRedemptionCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionRedemptionCount)), 0);
        set => SetValue(nameof(PromotionRedemptionCount), value);
    }

    /// <summary>
    /// Buy quantity for Buy X Get Y promotions.
    /// </summary>
    [DatabaseField]
    public virtual int PromotionBuyQuantity
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionBuyQuantity)), 1);
        set => SetValue(nameof(PromotionBuyQuantity), value);
    }

    /// <summary>
    /// Get quantity for Buy X Get Y promotions.
    /// </summary>
    [DatabaseField]
    public virtual int PromotionGetQuantity
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionGetQuantity)), 1);
        set => SetValue(nameof(PromotionGetQuantity), value);
    }

    /// <summary>
    /// Discount percentage applied to the "get" items (100 = free).
    /// </summary>
    [DatabaseField]
    public virtual decimal PromotionGetDiscountPercentage
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(PromotionGetDiscountPercentage)), 100m);
        set => SetValue(nameof(PromotionGetDiscountPercentage), value);
    }

    /// <summary>
    /// Shipping discount type for shipping promotions (FreeShipping = 0, ReducedRate = 1, FlatRate = 2).
    /// </summary>
    [DatabaseField]
    public virtual int PromotionShippingDiscountType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(PromotionShippingDiscountType)), 0);
        set => SetValue(nameof(PromotionShippingDiscountType), value);
    }

    /// <summary>
    /// Maximum shipping discount amount (optional cap).
    /// </summary>
    [DatabaseField]
    public virtual decimal? PromotionMaxShippingDiscount
    {
        get
        {
            var val = GetValue(nameof(PromotionMaxShippingDiscount));
            return val is null or DBNull ? null : ValidationHelper.GetDecimal(val, 0m);
        }
        set => SetValue(nameof(PromotionMaxShippingDiscount), value, value.HasValue);
    }

    /// <summary>
    /// Target shipping zones (JSON array). Empty = all zones.
    /// </summary>
    [DatabaseField]
    public virtual string PromotionTargetShippingZones
    {
        get => ValidationHelper.GetString(GetValue(nameof(PromotionTargetShippingZones)), string.Empty);
        set => SetValue(nameof(PromotionTargetShippingZones), value);
    }

    /// <summary>
    /// Created date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime PromotionCreated
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(PromotionCreated)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(PromotionCreated), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime PromotionLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(PromotionLastModified)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(PromotionLastModified), value);
    }

    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject() =>
        Provider.Delete(this);

    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject() =>
        Provider.Set(this);

    /// <summary>
    /// Constructor for deserialization.
    /// </summary>
    protected PromotionInfo(SerializationInfo info, StreamingContext context)
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates an empty instance.
    /// </summary>
    public PromotionInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from a DataRow.
    /// </summary>
    public PromotionInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Checks if the promotion is currently active.
    /// </summary>
    public bool IsCurrentlyActive()
    {
        if (!PromotionEnabled)
        {
            return false;
        }

        var now = DateTime.UtcNow;

        if (now < PromotionActiveFrom)
        {
            return false;
        }

        if (PromotionActiveTo.HasValue && now > PromotionActiveTo.Value)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the promotion status.
    /// </summary>
    public PromotionStatus GetStatus()
    {
        if (!PromotionEnabled)
        {
            return PromotionStatus.Deactivated;
        }

        var now = DateTime.UtcNow;

        if (now < PromotionActiveFrom)
        {
            return PromotionStatus.Scheduled;
        }

        if (PromotionActiveTo.HasValue && now > PromotionActiveTo.Value)
        {
            return PromotionStatus.Expired;
        }

        return PromotionStatus.Active;
    }

    /// <summary>
    /// Calculates the discount amount for a given price.
    /// </summary>
    public decimal CalculateDiscount(decimal price)
    {
        return (PromotionDiscountType)PromotionDiscountType switch
        {
            Ecommerce.PromotionDiscountType.Percentage => Math.Round(price * (PromotionDiscountValue / 100m), 2),
            Ecommerce.PromotionDiscountType.FixedAmount => Math.Min(PromotionDiscountValue, price),
            _ => 0m
        };
    }

    /// <summary>
    /// Calculates the shipping discount for a given shipping cost.
    /// </summary>
    public decimal CalculateShippingDiscount(decimal shippingCost)
    {
        var discount = (ShippingDiscountType)PromotionShippingDiscountType switch
        {
            Ecommerce.ShippingDiscountType.FreeShipping => shippingCost,
            Ecommerce.ShippingDiscountType.ReducedRate => Math.Round(shippingCost * (PromotionDiscountValue / 100m), 2),
            Ecommerce.ShippingDiscountType.FlatRate => Math.Max(0m, shippingCost - PromotionDiscountValue),
            _ => 0m
        };

        if (PromotionMaxShippingDiscount.HasValue)
        {
            discount = Math.Min(discount, PromotionMaxShippingDiscount.Value);
        }

        return Math.Min(discount, shippingCost);
    }

    /// <summary>
    /// Calculates the Buy X Get Y discount for a given unit price and quantity.
    /// Returns the total discount amount and the number of free items.
    /// </summary>
    public (decimal DiscountAmount, int FreeItems) CalculateBuyXGetYDiscount(decimal unitPrice, int quantity)
    {
        int cycleSize = PromotionBuyQuantity + PromotionGetQuantity;

        if (cycleSize <= 0 || quantity < cycleSize)
        {
            return (0m, 0);
        }

        int fullCycles = quantity / cycleSize;
        int freeItems = fullCycles * PromotionGetQuantity;
        decimal discountPerItem = Math.Round(unitPrice * (PromotionGetDiscountPercentage / 100m), 2);
        decimal totalDiscount = freeItems * discountPerItem;

        return (totalDiscount, freeItems);
    }
}

/// <summary>
/// Promotion type enumeration (for database storage).
/// </summary>
public enum PromotionTypeEnum
{
    /// <summary>Catalog promotion (product-level).</summary>
    Catalog = 0,

    /// <summary>Order promotion (order-level).</summary>
    Order = 1,

    /// <summary>Shipping promotion (free/discounted shipping).</summary>
    Shipping = 2,

    /// <summary>Buy X Get Y promotion.</summary>
    BuyXGetY = 3
}
