using Baseline.Ecommerce.Admin.DataProviders;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Customer Subscription edit forms.
/// </summary>
public class CustomerSubscriptionViewModel
{
    public int CustomerSubscriptionInfoID { get; set; }

    public Guid SubscriptionGuid { get; set; } = Guid.NewGuid();

    [NumberInputComponent(
        Label = "Customer ID",
        ExplanationText = "The Kentico Commerce customer ID",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Customer ID is required")]
    public int CustomerId { get; set; }

    [DropDownComponent(
        Label = "Subscription Plan",
        ExplanationText = "The plan this subscription is for",
        DataProviderType = typeof(SubscriptionPlanDataProvider),
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Plan is required")]
    public string PlanId { get; set; } = "";

    [DropDownComponent(
        Label = "Status",
        ExplanationText = "Current subscription status",
        DataProviderType = typeof(SubscriptionStatusDataProvider),
        Order = 3)]
    public string Status { get; set; } = "Active";

    [DateTimeInputComponent(
        Label = "Start Date",
        ExplanationText = "When the subscription started",
        Order = 4)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    [DateTimeInputComponent(
        Label = "Current Period End",
        ExplanationText = "When the current billing period ends",
        Order = 5)]
    public DateTime CurrentPeriodEnd { get; set; } = DateTime.UtcNow.AddMonths(1);

    [DateTimeInputComponent(
        Label = "Trial End",
        ExplanationText = "When the trial period ends (leave empty for no trial)",
        Order = 6)]
    public DateTime? TrialEnd { get; set; }

    [CheckBoxComponent(
        Label = "Cancel at Period End",
        ExplanationText = "If checked, subscription will be cancelled at the end of the current period",
        Order = 7)]
    public bool CancelAtPeriodEnd { get; set; }

    [TextAreaComponent(
        Label = "Cancellation Reason",
        ExplanationText = "Reason for cancellation (if applicable)",
        Order = 8)]
    public string? CancellationReason { get; set; }

    [TextInputComponent(
        Label = "External Subscription ID",
        ExplanationText = "External payment provider subscription ID (e.g., Stripe sub_xxx)",
        WatermarkText = "sub_xxx",
        Order = 9)]
    public string? ExternalSubscriptionId { get; set; }

    [TextInputComponent(
        Label = "Coupon Code",
        ExplanationText = "Applied coupon/discount code",
        Order = 10)]
    public string? CouponCode { get; set; }
}
