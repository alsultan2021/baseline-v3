using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.CouponInfo), Baseline.Ecommerce.Models.CouponInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Coupon info object for storing discount coupon codes.
/// </summary>
public class CouponInfo : AbstractInfo<CouponInfo, IInfoProvider<CouponInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.coupon";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<CouponInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.Coupon",
        idColumn: nameof(CouponID),
        timeStampColumn: nameof(CouponLastModified),
        guidColumn: nameof(CouponGuid),
        codeNameColumn: nameof(CouponCode),
        displayNameColumn: nameof(CouponCode),
        binaryColumn: null,
        parentIDColumn: nameof(CouponPromotionID),
        parentObjectType: PromotionInfo.OBJECT_TYPE)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true,
        DependsOn =
        [
            new ObjectDependency(nameof(CouponPromotionID), PromotionInfo.OBJECT_TYPE, ObjectDependencyEnum.Required)
        ]
    };

    /// <summary>
    /// Coupon ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int CouponID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CouponID)), 0);
        set => SetValue(nameof(CouponID), value);
    }

    /// <summary>
    /// Coupon GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid CouponGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(CouponGuid)), Guid.Empty);
        set => SetValue(nameof(CouponGuid), value);
    }

    /// <summary>
    /// Coupon code (unique identifier for redemption).
    /// </summary>
    [DatabaseField]
    public virtual string CouponCode
    {
        get => ValidationHelper.GetString(GetValue(nameof(CouponCode)), string.Empty);
        set => SetValue(nameof(CouponCode), value);
    }

    /// <summary>
    /// Associated promotion ID.
    /// </summary>
    [DatabaseField]
    public virtual int CouponPromotionID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CouponPromotionID)), 0);
        set => SetValue(nameof(CouponPromotionID), value);
    }

    /// <summary>
    /// Coupon type (SingleUse = 0, MultiUse = 1, Unlimited = 2).
    /// </summary>
    [DatabaseField]
    public virtual int CouponType
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CouponType)), 0);
        set => SetValue(nameof(CouponType), value);
    }

    /// <summary>
    /// Maximum number of redemptions (null for unlimited).
    /// </summary>
    [DatabaseField]
    public virtual int? CouponUsageLimit
    {
        get
        {
            var val = GetValue(nameof(CouponUsageLimit));
            return val is null or DBNull ? null : ValidationHelper.GetInteger(val, 0);
        }
        set => SetValue(nameof(CouponUsageLimit), value, value.HasValue);
    }

    /// <summary>
    /// Current redemption count.
    /// </summary>
    [DatabaseField]
    public virtual int CouponUsageCount
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(CouponUsageCount)), 0);
        set => SetValue(nameof(CouponUsageCount), value);
    }

    /// <summary>
    /// Coupon expiration date (optional).
    /// </summary>
    [DatabaseField]
    public virtual DateTime? CouponExpirationDate
    {
        get
        {
            var val = GetValue(nameof(CouponExpirationDate));
            return val is null or DBNull ? null : ValidationHelper.GetDateTime(val, DateTimeHelper.ZERO_TIME);
        }
        set => SetValue(nameof(CouponExpirationDate), value, value.HasValue);
    }

    /// <summary>
    /// Whether the coupon is enabled.
    /// </summary>
    [DatabaseField]
    public virtual bool CouponEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(CouponEnabled)), true);
        set => SetValue(nameof(CouponEnabled), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime CouponLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(CouponLastModified)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(CouponLastModified), value);
    }

    /// <summary>
    /// Created date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime CouponCreated
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(CouponCreated)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(CouponCreated), value);
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
    protected CouponInfo(SerializationInfo info, StreamingContext context)
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates an empty instance.
    /// </summary>
    public CouponInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from a DataRow.
    /// </summary>
    public CouponInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Checks if the coupon is valid for use.
    /// </summary>
    public bool IsValid()
    {
        if (!CouponEnabled)
        {
            return false;
        }

        if (CouponExpirationDate.HasValue && DateTime.UtcNow > CouponExpirationDate.Value)
        {
            return false;
        }

        if (HasReachedUsageLimit())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the coupon has reached its usage limit.
    /// </summary>
    public bool HasReachedUsageLimit() =>
        CouponUsageLimit.HasValue && CouponUsageCount >= CouponUsageLimit.Value;
}
