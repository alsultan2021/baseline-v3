using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for FulfillmentType create/edit forms.
/// </summary>
public class FulfillmentTypeViewModel
{
    /// <summary>
    /// Fulfillment type ID (primary key).
    /// </summary>
    public int FulfillmentTypeID { get; set; }

    /// <summary>
    /// Fulfillment type GUID.
    /// </summary>
    public Guid FulfillmentTypeGUID { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Code name for the fulfillment type.
    /// </summary>
    [TextInputComponent(
        Label = "Code Name",
        ExplanationText = "Unique identifier used in code (e.g., Physical, Ticket, Food)",
        WatermarkText = "Physical",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Code Name is required")]
    [MaxLengthValidationRule(100, ErrorMessage = "Code Name must be 100 characters or less")]
    public string FulfillmentTypeCodeName { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the fulfillment type.
    /// </summary>
    [TextInputComponent(
        Label = "Display Name",
        ExplanationText = "User-friendly name shown in admin and reports",
        WatermarkText = "Physical Product",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Display Name is required")]
    [MaxLengthValidationRule(200, ErrorMessage = "Display Name must be 200 characters or less")]
    public string FulfillmentTypeDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the fulfillment type behavior.
    /// </summary>
    [TextAreaComponent(
        Label = "Description",
        ExplanationText = "Describe when this fulfillment type should be used",
        WatermarkText = "Physical products requiring shipping address and carrier selection",
        Order = 3)]
    public string? FulfillmentTypeDescription { get; set; }

    /// <summary>
    /// Whether this fulfillment type requires shipping address.
    /// </summary>
    [CheckBoxComponent(
        Label = "Requires Shipping",
        ExplanationText = "If checked, checkout will require shipping address and carrier selection",
        Order = 4)]
    public bool FulfillmentTypeRequiresShipping { get; set; }

    /// <summary>
    /// Whether this fulfillment type requires billing address.
    /// </summary>
    [CheckBoxComponent(
        Label = "Requires Billing Address",
        ExplanationText = "If checked, checkout will require billing address entry",
        Order = 5)]
    public bool FulfillmentTypeRequiresBillingAddress { get; set; } = true;

    /// <summary>
    /// Whether this fulfillment type supports delivery/pickup options.
    /// </summary>
    [CheckBoxComponent(
        Label = "Supports Delivery Options",
        ExplanationText = "If checked, allows selection between delivery and pickup during checkout",
        Order = 6)]
    public bool FulfillmentTypeSupportsDeliveryOptions { get; set; }

    /// <summary>
    /// Whether this fulfillment type is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "If unchecked, this fulfillment type will not be available",
        Order = 7)]
    public bool FulfillmentTypeIsEnabled { get; set; } = true;
}
