namespace Baseline.Ecommerce;

#region Newsletter/Email Subscription Models

/// <summary>
/// Request to subscribe to a newsletter.
/// </summary>
public record SubscriptionRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Newsletter code name.
    /// </summary>
    public string? NewsletterName { get; init; }

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Whether double opt-in is required.
    /// </summary>
    public bool RequireDoubleOptIn { get; init; } = true;
}

/// <summary>
/// Result of a subscription operation.
/// </summary>
public record SubscriptionResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Whether confirmation email was sent.
    /// </summary>
    public bool ConfirmationSent { get; init; }

    public static SubscriptionResult Succeeded(bool confirmationSent = false) =>
        new() { Success = true, ConfirmationSent = confirmationSent };

    public static SubscriptionResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Subscriber preferences for newsletters.
/// </summary>
public record SubscriberPreferences
{
    /// <summary>
    /// Newsletter subscriptions.
    /// </summary>
    public IEnumerable<string> SubscribedNewsletters { get; init; } = [];

    /// <summary>
    /// Email format preference.
    /// </summary>
    public EmailFormat EmailFormat { get; init; } = EmailFormat.Html;

    /// <summary>
    /// Frequency preference.
    /// </summary>
    public EmailFrequency Frequency { get; init; } = EmailFrequency.Regular;
}

/// <summary>
/// Email format preference.
/// </summary>
public enum EmailFormat
{
    Html,
    PlainText
}

/// <summary>
/// Email frequency preference.
/// </summary>
public enum EmailFrequency
{
    Regular,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Newsletter information.
/// </summary>
public record NewsletterInfo
{
    /// <summary>
    /// Newsletter ID.
    /// </summary>
    public int NewsletterID { get; init; }

    /// <summary>
    /// Code name.
    /// </summary>
    public required string NewsletterName { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public required string NewsletterDisplayName { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether currently active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Subscription information for a subscriber.
/// </summary>
public record SubscriptionInfo
{
    /// <summary>
    /// Newsletter code name.
    /// </summary>
    public required string NewsletterName { get; init; }

    /// <summary>
    /// Newsletter display name.
    /// </summary>
    public required string NewsletterDisplayName { get; init; }

    /// <summary>
    /// Subscription date.
    /// </summary>
    public DateTimeOffset SubscribedOn { get; init; }

    /// <summary>
    /// Whether confirmed.
    /// </summary>
    public bool IsConfirmed { get; init; }
}

/// <summary>
/// Subscription status for a subscriber.
/// </summary>
public record SubscriptionStatus
{
    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Whether subscribed.
    /// </summary>
    public bool IsSubscribed { get; init; }

    /// <summary>
    /// Whether confirmed (double opt-in).
    /// </summary>
    public bool IsConfirmed { get; init; }

    /// <summary>
    /// Subscription date.
    /// </summary>
    public DateTimeOffset? SubscribedOn { get; init; }
}

/// <summary>
/// Result of unsubscribe validation.
/// </summary>
public record UnsubscribeValidationResult
{
    /// <summary>
    /// Whether valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if invalid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Email to unsubscribe.
    /// </summary>
    public string? Email { get; init; }
}

/// <summary>
/// Newsletter subscriber.
/// </summary>
public record Subscriber
{
    /// <summary>
    /// Subscriber ID.
    /// </summary>
    public int SubscriberId { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// First name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// When created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; init; }

    /// <summary>
    /// Whether bounced.
    /// </summary>
    public bool IsBounced { get; init; }
}

/// <summary>
/// Query for searching subscribers.
/// </summary>
public record SubscriberQuery
{
    /// <summary>
    /// Email filter.
    /// </summary>
    public string? EmailFilter { get; init; }

    /// <summary>
    /// Newsletter filter.
    /// </summary>
    public string? NewsletterName { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; } = 50;
}

/// <summary>
/// Result of subscriber search.
/// </summary>
public record SubscriberSearchResult
{
    /// <summary>
    /// Subscribers.
    /// </summary>
    public IReadOnlyList<Subscriber> Subscribers { get; init; } = [];

    /// <summary>
    /// Total count.
    /// </summary>
    public int TotalCount { get; init; }
}

/// <summary>
/// Result of importing subscribers.
/// </summary>
public record ImportSubscribersResult
{
    /// <summary>
    /// Whether succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Count imported.
    /// </summary>
    public int ImportedCount { get; init; }

    /// <summary>
    /// Count skipped.
    /// </summary>
    public int SkippedCount { get; init; }

    /// <summary>
    /// Error messages.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];
}

/// <summary>
/// Result of sending an email.
/// </summary>
public record SendResult
{
    /// <summary>
    /// Whether succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Count sent.
    /// </summary>
    public int SentCount { get; init; }

    public static SendResult Succeeded(int sentCount = 1) =>
        new() { Success = true, SentCount = sentCount };

    public static SendResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Newsletter send status.
/// </summary>
public record NewsletterSendStatus
{
    /// <summary>
    /// Issue ID.
    /// </summary>
    public int IssueId { get; init; }

    /// <summary>
    /// Status.
    /// </summary>
    public SendStatus Status { get; init; }

    /// <summary>
    /// Scheduled time.
    /// </summary>
    public DateTimeOffset? ScheduledFor { get; init; }

    /// <summary>
    /// Sent time.
    /// </summary>
    public DateTimeOffset? SentOn { get; init; }

    /// <summary>
    /// Recipients count.
    /// </summary>
    public int RecipientsCount { get; init; }

    /// <summary>
    /// Sent count.
    /// </summary>
    public int SentCount { get; init; }
}

/// <summary>
/// Send status enum.
/// </summary>
public enum SendStatus
{
    Draft,
    Scheduled,
    Sending,
    Sent,
    Cancelled,
    Failed
}

/// <summary>
/// Analytics for a newsletter issue.
/// </summary>
public record IssueAnalytics
{
    /// <summary>
    /// Issue ID.
    /// </summary>
    public int IssueId { get; init; }

    /// <summary>
    /// Total sent.
    /// </summary>
    public int TotalSent { get; init; }

    /// <summary>
    /// Open count.
    /// </summary>
    public int OpenCount { get; init; }

    /// <summary>
    /// Open rate.
    /// </summary>
    public double OpenRate => TotalSent > 0 ? (double)OpenCount / TotalSent : 0;

    /// <summary>
    /// Click count.
    /// </summary>
    public int ClickCount { get; init; }

    /// <summary>
    /// Click rate.
    /// </summary>
    public double ClickRate => TotalSent > 0 ? (double)ClickCount / TotalSent : 0;

    /// <summary>
    /// Bounce count.
    /// </summary>
    public int BounceCount { get; init; }

    /// <summary>
    /// Unsubscribe count.
    /// </summary>
    public int UnsubscribeCount { get; init; }
}

/// <summary>
/// Overall newsletter analytics.
/// </summary>
public record NewsletterAnalytics
{
    /// <summary>
    /// Newsletter name.
    /// </summary>
    public required string NewsletterName { get; init; }

    /// <summary>
    /// Total subscribers.
    /// </summary>
    public int TotalSubscribers { get; init; }

    /// <summary>
    /// Active subscribers.
    /// </summary>
    public int ActiveSubscribers { get; init; }

    /// <summary>
    /// Average open rate.
    /// </summary>
    public double AverageOpenRate { get; init; }

    /// <summary>
    /// Average click rate.
    /// </summary>
    public double AverageClickRate { get; init; }

    /// <summary>
    /// Growth rate.
    /// </summary>
    public double GrowthRate { get; init; }
}

/// <summary>
/// Coupon/discount code.
/// </summary>
public record Coupon
{
    /// <summary>
    /// Coupon ID.
    /// </summary>
    public int CouponId { get; init; }

    /// <summary>
    /// Coupon code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Discount type.
    /// </summary>
    public DiscountType DiscountType { get; init; }

    /// <summary>
    /// Discount value (percentage or fixed amount).
    /// </summary>
    public decimal DiscountValue { get; init; }

    /// <summary>
    /// Currency (for fixed amount).
    /// </summary>
    public string? Currency { get; init; }

    /// <summary>
    /// Valid from.
    /// </summary>
    public DateTimeOffset? ValidFrom { get; init; }

    /// <summary>
    /// Valid until.
    /// </summary>
    public DateTimeOffset? ValidUntil { get; init; }

    /// <summary>
    /// Maximum uses.
    /// </summary>
    public int? MaxUses { get; init; }

    /// <summary>
    /// Current use count.
    /// </summary>
    public int UseCount { get; init; }

    /// <summary>
    /// Whether active.
    /// </summary>
    public bool IsActive { get; init; } = true;
}

// Note: DiscountType enum is defined in EcommerceModels.cs

/// <summary>
/// Coupon error codes.
/// </summary>
public enum CouponErrorCode
{
    NotFound,
    Expired,
    MaxUsesReached,
    NotApplicable,
    MinimumNotMet
}

#endregion

#region Subscription Plans

/// <summary>
/// Represents a subscription plan (SaaS pricing tier).
/// </summary>
public record SubscriptionPlan
{
    /// <summary>
    /// Plan ID.
    /// </summary>
    public int PlanId { get; init; }

    /// <summary>
    /// Unique plan code.
    /// </summary>
    public required string PlanCode { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Plan description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Base price per billing interval.
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Currency code (e.g., USD, EUR).
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Billing interval.
    /// </summary>
    public BillingInterval BillingInterval { get; init; } = BillingInterval.Monthly;

    /// <summary>
    /// Number of billing intervals per cycle.
    /// </summary>
    public int IntervalCount { get; init; } = 1;

    /// <summary>
    /// Trial period in days (0 = no trial).
    /// </summary>
    public int TrialPeriodDays { get; init; }

    /// <summary>
    /// Tier level for plan comparison (higher = more features).
    /// </summary>
    public int TierLevel { get; init; }

    /// <summary>
    /// Whether this is the featured/recommended plan.
    /// </summary>
    public bool IsFeatured { get; init; }

    /// <summary>
    /// Whether the plan is active.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Plan features.
    /// </summary>
    public IEnumerable<PlanFeature> Features { get; init; } = [];

    /// <summary>
    /// External provider plan ID (e.g., Stripe price ID).
    /// </summary>
    public string? ExternalPlanId { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// A feature included in a subscription plan.
/// </summary>
public record PlanFeature
{
    /// <summary>
    /// Feature name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Feature description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the feature is included.
    /// </summary>
    public bool IsIncluded { get; init; } = true;

    /// <summary>
    /// Limit value (null = unlimited).
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Display value (e.g., "Unlimited", "10 GB", "5 users").
    /// </summary>
    public string? DisplayValue { get; init; }
}

/// <summary>
/// Billing interval options.
/// </summary>
public enum BillingInterval
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}

/// <summary>
/// Plan comparison result.
/// </summary>
public record PlanComparison
{
    /// <summary>
    /// Plans being compared.
    /// </summary>
    public IEnumerable<SubscriptionPlan> Plans { get; init; } = [];

    /// <summary>
    /// All unique features across plans.
    /// </summary>
    public IEnumerable<FeatureComparison> Features { get; init; } = [];
}

/// <summary>
/// Feature comparison across plans.
/// </summary>
public record FeatureComparison
{
    /// <summary>
    /// Feature name.
    /// </summary>
    public required string FeatureName { get; init; }

    /// <summary>
    /// Value for each plan (keyed by plan ID).
    /// </summary>
    public IDictionary<int, FeatureValue> PlanValues { get; init; } = new Dictionary<int, FeatureValue>();
}

/// <summary>
/// Feature value for a specific plan.
/// </summary>
public record FeatureValue
{
    /// <summary>
    /// Whether the feature is included.
    /// </summary>
    public bool IsIncluded { get; init; }

    /// <summary>
    /// Display value.
    /// </summary>
    public string? DisplayValue { get; init; }

    /// <summary>
    /// Numeric limit.
    /// </summary>
    public int? Limit { get; init; }
}

#endregion

#region Customer Subscriptions

/// <summary>
/// Represents a customer's subscription.
/// </summary>
public record CustomerSubscription
{
    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// Customer ID.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Plan ID.
    /// </summary>
    public int PlanId { get; init; }

    /// <summary>
    /// The subscription plan.
    /// </summary>
    public SubscriptionPlan? Plan { get; init; }

    /// <summary>
    /// Subscription status.
    /// </summary>
    public UserSubscriptionState Status { get; init; }

    /// <summary>
    /// When the subscription started.
    /// </summary>
    public DateTimeOffset StartDate { get; init; }

    /// <summary>
    /// When the current period ends.
    /// </summary>
    public DateTimeOffset CurrentPeriodEnd { get; init; }

    /// <summary>
    /// When the trial ends (null if no trial).
    /// </summary>
    public DateTimeOffset? TrialEnd { get; init; }

    /// <summary>
    /// When the subscription was cancelled (null if not cancelled).
    /// </summary>
    public DateTimeOffset? CancelledAt { get; init; }

    /// <summary>
    /// When the subscription ends after cancellation.
    /// </summary>
    public DateTimeOffset? CancelAt { get; init; }

    /// <summary>
    /// Whether to cancel at period end.
    /// </summary>
    public bool CancelAtPeriodEnd { get; init; }

    /// <summary>
    /// External subscription ID (e.g., Stripe subscription ID).
    /// </summary>
    public string? ExternalSubscriptionId { get; init; }

    /// <summary>
    /// Applied coupon code.
    /// </summary>
    public string? CouponCode { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// User subscription status (for SaaS plans).
/// </summary>
public enum UserSubscriptionState
{
    Trialing,
    Active,
    PastDue,
    Paused,
    Cancelled,
    Expired,
    Incomplete,
    IncompleteExpired
}

#endregion

#region Subscription Requests/Results

/// <summary>
/// Request to create a subscription.
/// </summary>
public record CreateSubscriptionRequest
{
    /// <summary>
    /// Customer ID.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Plan ID.
    /// </summary>
    public int PlanId { get; init; }

    /// <summary>
    /// External customer ID from payment provider (e.g., Stripe cus_xxx).
    /// </summary>
    public string? ExternalCustomerId { get; init; }

    /// <summary>
    /// Payment method ID.
    /// </summary>
    public string? PaymentMethodId { get; init; }

    /// <summary>
    /// Coupon code to apply.
    /// </summary>
    public string? CouponCode { get; init; }

    /// <summary>
    /// Trial days override (null = use plan default).
    /// </summary>
    public int? TrialDays { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of subscription creation.
/// </summary>
public record CreateSubscriptionResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The created subscription.
    /// </summary>
    public CustomerSubscription? Subscription { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Client secret for payment confirmation (e.g., Stripe).
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Whether payment action is required.
    /// </summary>
    public bool RequiresAction { get; init; }

    public static CreateSubscriptionResult Succeeded(CustomerSubscription subscription) =>
        new() { Success = true, Subscription = subscription };

    public static CreateSubscriptionResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };

    public static CreateSubscriptionResult RequiresPaymentAction(string clientSecret) =>
        new() { Success = false, RequiresAction = true, ClientSecret = clientSecret };
}

/// <summary>
/// Request to change subscription plan.
/// </summary>
public record ChangePlanRequest
{
    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// New plan ID.
    /// </summary>
    public int NewPlanId { get; init; }

    /// <summary>
    /// Whether to prorate the change.
    /// </summary>
    public bool Prorate { get; init; } = true;

    /// <summary>
    /// Whether to bill immediately.
    /// </summary>
    public bool BillImmediately { get; init; }
}

/// <summary>
/// Result of plan change.
/// </summary>
public record ChangePlanResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The updated subscription.
    /// </summary>
    public CustomerSubscription? Subscription { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Proration amount.
    /// </summary>
    public decimal? ProrationAmount { get; init; }

    public static ChangePlanResult Succeeded(CustomerSubscription subscription, decimal? proration = null) =>
        new() { Success = true, Subscription = subscription, ProrationAmount = proration };

    public static ChangePlanResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Request to cancel a subscription.
/// </summary>
public record CancelSubscriptionRequest
{
    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// Whether to cancel immediately or at period end.
    /// </summary>
    public bool CancelImmediately { get; init; }

    /// <summary>
    /// Cancellation reason.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Feedback comments.
    /// </summary>
    public string? Feedback { get; init; }
}

/// <summary>
/// Result of subscription cancellation.
/// </summary>
public record CancelSubscriptionResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The cancelled subscription.
    /// </summary>
    public CustomerSubscription? Subscription { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the subscription will end.
    /// </summary>
    public DateTimeOffset? EffectiveDate { get; init; }

    public static CancelSubscriptionResult Succeeded(CustomerSubscription subscription, DateTimeOffset? effectiveDate) =>
        new() { Success = true, Subscription = subscription, EffectiveDate = effectiveDate };

    public static CancelSubscriptionResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Request to pause a subscription.
/// </summary>
public record PauseSubscriptionRequest
{
    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// When to resume (null = indefinite pause).
    /// </summary>
    public DateTimeOffset? ResumeAt { get; init; }

    /// <summary>
    /// Reason for pausing.
    /// </summary>
    public string? Reason { get; init; }
}

#endregion

#region Subscription Invoices

/// <summary>
/// Subscription invoice.
/// </summary>
public record SubscriptionInvoice
{
    /// <summary>
    /// Invoice ID.
    /// </summary>
    public int InvoiceId { get; init; }

    /// <summary>
    /// Invoice number.
    /// </summary>
    public required string InvoiceNumber { get; init; }

    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// Customer ID.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Invoice status.
    /// </summary>
    public InvoiceStatus Status { get; init; }

    /// <summary>
    /// Subtotal amount.
    /// </summary>
    public decimal Subtotal { get; init; }

    /// <summary>
    /// Discount amount.
    /// </summary>
    public decimal Discount { get; init; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal Tax { get; init; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal Total { get; init; }

    /// <summary>
    /// Amount paid.
    /// </summary>
    public decimal AmountPaid { get; init; }

    /// <summary>
    /// Amount due.
    /// </summary>
    public decimal AmountDue { get; init; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Invoice date.
    /// </summary>
    public DateTimeOffset InvoiceDate { get; init; }

    /// <summary>
    /// Due date.
    /// </summary>
    public DateTimeOffset? DueDate { get; init; }

    /// <summary>
    /// When the invoice was paid.
    /// </summary>
    public DateTimeOffset? PaidAt { get; init; }

    /// <summary>
    /// Period start date.
    /// </summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>
    /// Period end date.
    /// </summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>
    /// External invoice ID.
    /// </summary>
    public string? ExternalInvoiceId { get; init; }

    /// <summary>
    /// Invoice PDF URL.
    /// </summary>
    public string? PdfUrl { get; init; }

    /// <summary>
    /// Line items.
    /// </summary>
    public IEnumerable<InvoiceLineItem> LineItems { get; init; } = [];
}

/// <summary>
/// Invoice status.
/// </summary>
public enum InvoiceStatus
{
    Draft,
    Open,
    Paid,
    Void,
    Uncollectible
}

/// <summary>
/// Invoice line item.
/// </summary>
public record InvoiceLineItem
{
    /// <summary>
    /// Description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Quantity.
    /// </summary>
    public decimal Quantity { get; init; } = 1;

    /// <summary>
    /// Unit price.
    /// </summary>
    public decimal UnitPrice { get; init; }

    /// <summary>
    /// Line total.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Period start (for subscription items).
    /// </summary>
    public DateTimeOffset? PeriodStart { get; init; }

    /// <summary>
    /// Period end (for subscription items).
    /// </summary>
    public DateTimeOffset? PeriodEnd { get; init; }
}

#endregion

#region Subscription Coupons

/// <summary>
/// Subscription-specific coupon settings.
/// </summary>
public record SubscriptionCouponSettings
{
    /// <summary>
    /// Coupon ID.
    /// </summary>
    public int CouponId { get; init; }

    /// <summary>
    /// Duration of the discount.
    /// </summary>
    public CouponDuration Duration { get; init; } = CouponDuration.Once;

    /// <summary>
    /// Number of months for repeating coupons.
    /// </summary>
    public int? DurationInMonths { get; init; }

    /// <summary>
    /// Applicable plan IDs (empty = all plans).
    /// </summary>
    public IEnumerable<int> ApplicablePlanIds { get; init; } = [];

    /// <summary>
    /// Whether first-time customers only.
    /// </summary>
    public bool FirstTimeOnly { get; init; }
}

/// <summary>
/// Coupon duration type.
/// </summary>
public enum CouponDuration
{
    Once,
    Forever,
    Repeating
}

/// <summary>
/// Result of coupon validation for subscriptions.
/// </summary>
public record CouponValidationResult
{
    /// <summary>
    /// Whether the coupon is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Error message if invalid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Error code.
    /// </summary>
    public CouponErrorCode? ErrorCode { get; init; }

    /// <summary>
    /// The coupon if valid.
    /// </summary>
    public Coupon? Coupon { get; init; }

    /// <summary>
    /// Subscription-specific coupon settings.
    /// </summary>
    public SubscriptionCouponSettings? CouponSettings { get; init; }

    /// <summary>
    /// Calculated discount amount.
    /// </summary>
    public decimal DiscountAmount { get; init; }

    public static CouponValidationResult Valid(Coupon coupon, decimal discountAmount, SubscriptionCouponSettings? settings = null) =>
        new() { IsValid = true, Coupon = coupon, DiscountAmount = discountAmount, CouponSettings = settings };

    public static CouponValidationResult Invalid(CouponErrorCode code, string message) =>
        new() { IsValid = false, ErrorCode = code, ErrorMessage = message };
}

#endregion

#region Subscription Usage (Metered Billing)

/// <summary>
/// Usage record for metered billing.
/// </summary>
public record SubscriptionUsage
{
    /// <summary>
    /// Usage record ID.
    /// </summary>
    public int UsageId { get; init; }

    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// Meter ID.
    /// </summary>
    public required string MeterId { get; init; }

    /// <summary>
    /// Quantity used.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// When the usage occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Idempotency key.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Usage summary for a billing period.
/// </summary>
public record UsageSummary
{
    /// <summary>
    /// Subscription ID.
    /// </summary>
    public int SubscriptionId { get; init; }

    /// <summary>
    /// Period start.
    /// </summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>
    /// Period end.
    /// </summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>
    /// Usage by meter.
    /// </summary>
    public IDictionary<string, MeterUsageSummary> MeterUsage { get; init; } = new Dictionary<string, MeterUsageSummary>();
}

/// <summary>
/// Usage summary for a specific meter.
/// </summary>
public record MeterUsageSummary
{
    /// <summary>
    /// Meter ID.
    /// </summary>
    public required string MeterId { get; init; }

    /// <summary>
    /// Meter display name.
    /// </summary>
    public string? MeterName { get; init; }

    /// <summary>
    /// Total quantity used.
    /// </summary>
    public decimal TotalQuantity { get; init; }

    /// <summary>
    /// Included quantity (from plan).
    /// </summary>
    public decimal IncludedQuantity { get; init; }

    /// <summary>
    /// Overage quantity.
    /// </summary>
    public decimal OverageQuantity { get; init; }

    /// <summary>
    /// Overage unit price.
    /// </summary>
    public decimal OverageUnitPrice { get; init; }

    /// <summary>
    /// Total overage cost.
    /// </summary>
    public decimal OverageCost { get; init; }
}

#endregion

#region Payment Provider Models

/// <summary>
/// Request to create a customer in the payment provider.
/// </summary>
public record ProviderCustomerRequest
{
    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Customer name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Phone number.
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Billing address.
    /// </summary>
    public Address? BillingAddress { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of customer creation in payment provider.
/// </summary>
public record ProviderCustomerResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// External customer ID.
    /// </summary>
    public string? ExternalCustomerId { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static ProviderCustomerResult Succeeded(string externalCustomerId) =>
        new() { Success = true, ExternalCustomerId = externalCustomerId };

    public static ProviderCustomerResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Customer from payment provider.
/// </summary>
public record ProviderCustomer
{
    /// <summary>
    /// External customer ID.
    /// </summary>
    public required string ExternalCustomerId { get; init; }

    /// <summary>
    /// Email.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Default payment method ID.
    /// </summary>
    public string? DefaultPaymentMethodId { get; init; }
}

/// <summary>
/// Payment method from provider.
/// </summary>
public record ProviderPaymentMethod
{
    /// <summary>
    /// Payment method ID.
    /// </summary>
    public required string PaymentMethodId { get; init; }

    /// <summary>
    /// Payment method type (e.g., "card", "bank_account").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Card brand (for card payments).
    /// </summary>
    public string? CardBrand { get; init; }

    /// <summary>
    /// Last 4 digits.
    /// </summary>
    public string? Last4 { get; init; }

    /// <summary>
    /// Expiration month.
    /// </summary>
    public int? ExpMonth { get; init; }

    /// <summary>
    /// Expiration year.
    /// </summary>
    public int? ExpYear { get; init; }

    /// <summary>
    /// Whether this is the default payment method.
    /// </summary>
    public bool IsDefault { get; init; }
}

/// <summary>
/// Request to create a subscription in the payment provider.
/// </summary>
public record ProviderSubscriptionRequest
{
    /// <summary>
    /// External customer ID.
    /// </summary>
    public required string ExternalCustomerId { get; init; }

    /// <summary>
    /// External plan/price ID.
    /// </summary>
    public required string ExternalPlanId { get; init; }

    /// <summary>
    /// Payment method ID.
    /// </summary>
    public string? PaymentMethodId { get; init; }

    /// <summary>
    /// Coupon ID to apply.
    /// </summary>
    public string? CouponId { get; init; }

    /// <summary>
    /// Trial period in days.
    /// </summary>
    public int? TrialDays { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to update a subscription in the payment provider.
/// </summary>
public record ProviderSubscriptionUpdateRequest
{
    /// <summary>
    /// New external plan/price ID.
    /// </summary>
    public string? NewExternalPlanId { get; init; }

    /// <summary>
    /// New payment method ID.
    /// </summary>
    public string? PaymentMethodId { get; init; }

    /// <summary>
    /// Whether to prorate.
    /// </summary>
    public bool? Prorate { get; init; }

    /// <summary>
    /// Whether to cancel at period end.
    /// </summary>
    public bool? CancelAtPeriodEnd { get; init; }

    /// <summary>
    /// Metadata.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Result of subscription operation in payment provider.
/// </summary>
public record ProviderSubscriptionResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// External subscription ID.
    /// </summary>
    public string? ExternalSubscriptionId { get; init; }

    /// <summary>
    /// Subscription status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Client secret for payment confirmation.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// Whether payment action is required.
    /// </summary>
    public bool RequiresAction { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static ProviderSubscriptionResult Succeeded(string externalSubscriptionId, string status) =>
        new() { Success = true, ExternalSubscriptionId = externalSubscriptionId, Status = status };

    public static ProviderSubscriptionResult Failed(string error) =>
        new() { Success = false, ErrorMessage = error };

    public static ProviderSubscriptionResult RequiresPaymentAction(string clientSecret) =>
        new() { Success = false, RequiresAction = true, ClientSecret = clientSecret };
}

/// <summary>
/// Represents an external price/plan from a payment provider.
/// </summary>
public record ProviderPlan
{
    public required string ExternalPlanId { get; init; }
    public required string DisplayName { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string? Interval { get; init; }
    public bool IsActive { get; init; }
}

#endregion
