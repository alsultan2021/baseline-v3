using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Tax Class create/edit forms.
/// </summary>
public class TaxClassViewModel
{
    /// <summary>
    /// Tax class ID (hidden on create).
    /// </summary>
    public int TaxClassID { get; set; }

    /// <summary>
    /// Tax class GUID.
    /// </summary>
    public Guid TaxClassGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Tax class code name (unique identifier).
    /// </summary>
    [TextInputComponent(
        Label = "Code Name",
        ExplanationText = "Unique identifier for the tax class (e.g., Standard, Reduced, Exempt)",
        WatermarkText = "Standard",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Code Name is required")]
    public string TaxClassName { get; set; } = string.Empty;

    /// <summary>
    /// Tax class display name.
    /// </summary>
    [TextInputComponent(
        Label = "Display Name",
        ExplanationText = "Name shown in the admin interface and reports",
        WatermarkText = "Standard Rate",
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Display Name is required")]
    public string TaxClassDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tax class description.
    /// </summary>
    [TextAreaComponent(
        Label = "Description",
        ExplanationText = "Describe when this tax class should be used",
        Order = 3)]
    public string TaxClassDescription { get; set; } = string.Empty;

    /// <summary>
    /// Default tax rate for this class (percentage).
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Default Rate (%)",
        ExplanationText = "Default tax rate as a percentage (e.g., 8.25 for 8.25%). Can be overridden by region-specific rates.",
        Order = 4)]
    public decimal TaxClassDefaultRate { get; set; } = 0m;

    /// <summary>
    /// Whether this is the default tax class.
    /// </summary>
    [CheckBoxComponent(
        Label = "Default Tax Class",
        ExplanationText = "If checked, this tax class will be used for products without an assigned tax class",
        Order = 5)]
    public bool TaxClassIsDefault { get; set; }

    /// <summary>
    /// Whether this tax class is tax-exempt.
    /// </summary>
    [CheckBoxComponent(
        Label = "Tax Exempt",
        ExplanationText = "If checked, items in this class will not have tax applied",
        Order = 6)]
    public bool TaxClassIsExempt { get; set; }

    /// <summary>
    /// Display order.
    /// </summary>
    [NumberInputComponent(
        Label = "Display Order",
        ExplanationText = "Order in which the tax class appears in lists",
        Order = 7)]
    public int TaxClassOrder { get; set; }

    /// <summary>
    /// Whether the tax class is enabled.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "If unchecked, this tax class will not be available for selection",
        Order = 8)]
    public bool TaxClassEnabled { get; set; } = true;
}
