using Baseline.Ecommerce.Admin.DataProviders;

using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.FormAnnotations.Internal;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Subscription Plan create/edit forms.
/// </summary>
public class SubscriptionPlanViewModel
{
    public int SubscriptionPlanInfoID { get; set; }

    public Guid SubscriptionPlanGuid { get; set; } = Guid.NewGuid();

    [CodeNameComponent(
        Label = "Plan Code",
        ExplanationText = "Unique code identifier (e.g., pro-monthly)",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Plan Code is required")]
    public string PlanCode { get; set; } = "";

    [TextInputComponent(
        Label = "Plan Name",
        ExplanationText = "Display name shown to customers",
        WatermarkText = "Pro Monthly",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Plan Name is required")]
    public string Name { get; set; } = "";

    [TextAreaComponent(
        Label = "Description",
        ExplanationText = "Describe what this plan includes",
        Order = 3)]
    public string Description { get; set; } = "";

    [DecimalNumberInputComponent(
        Label = "Price",
        ExplanationText = "Price per billing interval",
        Order = 4)]
    [RequiredValidationRule(ErrorMessage = "Price is required")]
    public decimal Price { get; set; }

    [DropDownComponent(
        Label = "Currency",
        ExplanationText = "Select currency for this plan",
        DataProviderType = typeof(CurrencyDataProvider),
        Order = 5)]
    public string Currency { get; set; } = "USD";

    [DropDownComponent(
        Label = "Billing Interval",
        ExplanationText = "How often the customer is billed",
        DataProviderType = typeof(BillingIntervalDataProvider),
        Order = 6)]
    public string BillingInterval { get; set; } = "Monthly";

    [NumberInputComponent(
        Label = "Interval Count",
        ExplanationText = "Number of intervals per billing cycle (e.g., 1 = every month, 3 = every 3 months)",
        Order = 7)]
    public int IntervalCount { get; set; } = 1;

    [NumberInputComponent(
        Label = "Trial Period (Days)",
        ExplanationText = "Number of free trial days (0 = no trial)",
        Order = 8)]
    public int TrialPeriodDays { get; set; }

    [NumberInputComponent(
        Label = "Tier Level",
        ExplanationText = "Numeric tier for plan comparison (higher = more features)",
        Order = 9)]
    public int TierLevel { get; set; }

    [CheckBoxComponent(
        Label = "Featured Plan",
        ExplanationText = "Highlight this plan on pricing pages",
        Order = 10)]
    public bool IsFeatured { get; set; }

    [CheckBoxComponent(
        Label = "Active",
        ExplanationText = "Whether this plan is available for new subscriptions",
        Order = 11)]
    public bool IsActive { get; set; } = true;

    [DropDownComponent(
        Label = "External Plan ID",
        ExplanationText = "Select a price/plan from the payment provider",
        DataProviderType = typeof(ExternalPlanIdDataProvider),
        Order = 12)]
    public string? ExternalPlanId { get; set; }
}
