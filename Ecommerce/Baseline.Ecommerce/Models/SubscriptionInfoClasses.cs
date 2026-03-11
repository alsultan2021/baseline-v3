using System.ComponentModel;
using System.Data;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.CustomerSubscriptionInfo), Baseline.Ecommerce.CustomerSubscriptionInfo.OBJECT_TYPE)]
[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.SubscriptionPlanInfo), Baseline.Ecommerce.SubscriptionPlanInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce;

/// <summary>
/// Represents a customer subscription record.
/// </summary>
public class CustomerSubscriptionInfo : AbstractInfo<CustomerSubscriptionInfo, IInfoProvider<CustomerSubscriptionInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type identifier.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.customersubscription";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<CustomerSubscriptionInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.CustomerSubscription",
        idColumn: nameof(CustomerSubscriptionInfoID),
        timeStampColumn: nameof(ModifiedOn),
        guidColumn: nameof(SubscriptionGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>
    /// Creates a new instance of <see cref="CustomerSubscriptionInfo"/>.
    /// </summary>
    public CustomerSubscriptionInfo() : base(TYPEINFO) { }

    /// <summary>
    /// Creates a new instance from data row.
    /// </summary>
    public CustomerSubscriptionInfo(DataRow dr) : base(TYPEINFO, dr) { }

    /// <summary>
    /// Subscription ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int CustomerSubscriptionInfoID
    {
        get => GetIntegerValue(nameof(CustomerSubscriptionInfoID), 0);
        set => SetValue(nameof(CustomerSubscriptionInfoID), value);
    }

    /// <summary>
    /// Subscription GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid SubscriptionGuid
    {
        get => GetGuidValue(nameof(SubscriptionGuid), Guid.Empty);
        set => SetValue(nameof(SubscriptionGuid), value);
    }

    /// <summary>
    /// Customer ID.
    /// </summary>
    [DatabaseField]
    public virtual int CustomerId
    {
        get => GetIntegerValue(nameof(CustomerId), 0);
        set => SetValue(nameof(CustomerId), value);
    }

    /// <summary>
    /// Plan ID.
    /// </summary>
    [DatabaseField]
    public virtual int PlanId
    {
        get => GetIntegerValue(nameof(PlanId), 0);
        set => SetValue(nameof(PlanId), value);
    }

    /// <summary>
    /// Subscription status.
    /// </summary>
    [DatabaseField]
    public virtual string Status
    {
        get => GetStringValue(nameof(Status), nameof(UserSubscriptionState.Active));
        set => SetValue(nameof(Status), value);
    }

    /// <summary>
    /// Start date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime StartDate
    {
        get => GetDateTimeValue(nameof(StartDate), DateTime.MinValue);
        set => SetValue(nameof(StartDate), value);
    }

    /// <summary>
    /// Current period end date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime CurrentPeriodEnd
    {
        get => GetDateTimeValue(nameof(CurrentPeriodEnd), DateTime.MinValue);
        set => SetValue(nameof(CurrentPeriodEnd), value);
    }

    /// <summary>
    /// Trial end date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime? TrialEnd
    {
        get
        {
            var val = GetValue(nameof(TrialEnd));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(TrialEnd), value);
    }

    /// <summary>
    /// Cancelled at date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime? CancelledAt
    {
        get
        {
            var val = GetValue(nameof(CancelledAt));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(CancelledAt), value);
    }

    /// <summary>
    /// Cancel at date (effective cancellation).
    /// </summary>
    [DatabaseField]
    public virtual DateTime? CancelAt
    {
        get
        {
            var val = GetValue(nameof(CancelAt));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(CancelAt), value);
    }

    /// <summary>
    /// Whether to cancel at period end.
    /// </summary>
    [DatabaseField]
    public virtual bool CancelAtPeriodEnd
    {
        get => GetBooleanValue(nameof(CancelAtPeriodEnd), false);
        set => SetValue(nameof(CancelAtPeriodEnd), value);
    }

    /// <summary>
    /// Cancellation reason.
    /// </summary>
    [DatabaseField]
    public virtual string? CancellationReason
    {
        get => GetStringValue(nameof(CancellationReason), null);
        set => SetValue(nameof(CancellationReason), value);
    }

    /// <summary>
    /// Paused at date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime? PausedAt
    {
        get
        {
            var val = GetValue(nameof(PausedAt));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(PausedAt), value);
    }

    /// <summary>
    /// Resume at date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime? ResumeAt
    {
        get
        {
            var val = GetValue(nameof(ResumeAt));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(ResumeAt), value);
    }

    /// <summary>
    /// Pause reason.
    /// </summary>
    [DatabaseField]
    public virtual string? PauseReason
    {
        get => GetStringValue(nameof(PauseReason), null);
        set => SetValue(nameof(PauseReason), value);
    }

    /// <summary>
    /// External subscription ID (e.g., Stripe subscription ID).
    /// </summary>
    [DatabaseField]
    public virtual string? ExternalSubscriptionId
    {
        get => GetStringValue(nameof(ExternalSubscriptionId), null);
        set => SetValue(nameof(ExternalSubscriptionId), value);
    }

    /// <summary>
    /// Applied coupon code.
    /// </summary>
    [DatabaseField]
    public virtual string? CouponCode
    {
        get => GetStringValue(nameof(CouponCode), null);
        set => SetValue(nameof(CouponCode), value);
    }

    /// <summary>
    /// Created on date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime CreatedOn
    {
        get => GetDateTimeValue(nameof(CreatedOn), DateTime.MinValue);
        set => SetValue(nameof(CreatedOn), value);
    }

    /// <summary>
    /// Modified on date.
    /// </summary>
    [DatabaseField]
    public virtual DateTime? ModifiedOn
    {
        get
        {
            var val = GetValue(nameof(ModifiedOn));
            return val is DateTime dt ? dt : null;
        }
        set => SetValue(nameof(ModifiedOn), value);
    }
}

/// <summary>
/// Represents a subscription plan.
/// </summary>
public class SubscriptionPlanInfo : AbstractInfo<SubscriptionPlanInfo, IInfoProvider<SubscriptionPlanInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type identifier.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.subscriptionplan";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<SubscriptionPlanInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.SubscriptionPlan",
        idColumn: nameof(SubscriptionPlanInfoID),
        timeStampColumn: null,
        guidColumn: nameof(SubscriptionPlanGuid),
        codeNameColumn: nameof(PlanCode),
        displayNameColumn: nameof(Name),
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
    };

    /// <summary>
    /// Creates a new instance of <see cref="SubscriptionPlanInfo"/>.
    /// </summary>
    public SubscriptionPlanInfo() : base(TYPEINFO) { }

    /// <summary>
    /// Creates a new instance from data row.
    /// </summary>
    public SubscriptionPlanInfo(DataRow dr) : base(TYPEINFO, dr) { }

    /// <summary>
    /// Plan ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int SubscriptionPlanInfoID
    {
        get => GetIntegerValue(nameof(SubscriptionPlanInfoID), 0);
        set => SetValue(nameof(SubscriptionPlanInfoID), value);
    }

    /// <summary>
    /// Plan GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid SubscriptionPlanGuid
    {
        get => GetGuidValue(nameof(SubscriptionPlanGuid), Guid.Empty);
        set => SetValue(nameof(SubscriptionPlanGuid), value);
    }

    /// <summary>
    /// Unique plan code.
    /// </summary>
    [DatabaseField]
    public virtual string PlanCode
    {
        get => GetStringValue(nameof(PlanCode), string.Empty);
        set => SetValue(nameof(PlanCode), value);
    }

    /// <summary>
    /// Display name.
    /// </summary>
    [DatabaseField]
    public virtual string Name
    {
        get => GetStringValue(nameof(Name), string.Empty);
        set => SetValue(nameof(Name), value);
    }

    /// <summary>
    /// Description.
    /// </summary>
    [DatabaseField]
    public virtual string? Description
    {
        get => GetStringValue(nameof(Description), null);
        set => SetValue(nameof(Description), value);
    }

    /// <summary>
    /// Price per billing interval.
    /// </summary>
    [DatabaseField]
    public virtual decimal Price
    {
        get => GetDecimalValue(nameof(Price), 0);
        set => SetValue(nameof(Price), value);
    }

    /// <summary>
    /// Currency code.
    /// </summary>
    [DatabaseField]
    public virtual string Currency
    {
        get => GetStringValue(nameof(Currency), "USD");
        set => SetValue(nameof(Currency), value);
    }

    /// <summary>
    /// Billing interval (Daily, Weekly, Monthly, Quarterly, Yearly).
    /// </summary>
    [DatabaseField]
    public virtual string BillingInterval
    {
        get => GetStringValue(nameof(BillingInterval), "Monthly");
        set => SetValue(nameof(BillingInterval), value);
    }

    /// <summary>
    /// Number of billing intervals per cycle.
    /// </summary>
    [DatabaseField]
    public virtual int IntervalCount
    {
        get => GetIntegerValue(nameof(IntervalCount), 1);
        set => SetValue(nameof(IntervalCount), value);
    }

    /// <summary>
    /// Trial period in days.
    /// </summary>
    [DatabaseField]
    public virtual int TrialPeriodDays
    {
        get => GetIntegerValue(nameof(TrialPeriodDays), 0);
        set => SetValue(nameof(TrialPeriodDays), value);
    }

    /// <summary>
    /// Tier level for comparison.
    /// </summary>
    [DatabaseField]
    public virtual int TierLevel
    {
        get => GetIntegerValue(nameof(TierLevel), 0);
        set => SetValue(nameof(TierLevel), value);
    }

    /// <summary>
    /// Whether this is the featured plan.
    /// </summary>
    [DatabaseField]
    public virtual bool IsFeatured
    {
        get => GetBooleanValue(nameof(IsFeatured), false);
        set => SetValue(nameof(IsFeatured), value);
    }

    /// <summary>
    /// Whether the plan is active.
    /// </summary>
    [DatabaseField]
    public virtual bool IsActive
    {
        get => GetBooleanValue(nameof(IsActive), true);
        set => SetValue(nameof(IsActive), value);
    }

    /// <summary>
    /// External plan ID (e.g., Stripe price ID).
    /// </summary>
    [DatabaseField]
    public virtual string? ExternalPlanId
    {
        get => GetStringValue(nameof(ExternalPlanId), null);
        set => SetValue(nameof(ExternalPlanId), value);
    }
}
