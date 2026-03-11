using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// Renders a newsletter subscription form.
/// </summary>
public class SubscriptionFormViewComponent : ViewComponent
{
    /// <summary>
    /// Renders the subscription form.
    /// </summary>
    /// <param name="newsletterId">Optional specific newsletter ID.</param>
    /// <param name="buttonText">Submit button text.</param>
    /// <param name="placeholderText">Email input placeholder.</param>
    /// <param name="showNameField">Whether to show a name field.</param>
    /// <param name="successMessage">Success message after subscription.</param>
    public IViewComponentResult Invoke(
        Guid? newsletterId = null,
        string buttonText = "Subscribe",
        string placeholderText = "Enter your email",
        bool showNameField = false,
        string? successMessage = null)
    {
        var model = new SubscriptionFormViewModel
        {
            NewsletterId = newsletterId,
            ButtonText = buttonText,
            PlaceholderText = placeholderText,
            ShowNameField = showNameField,
            SuccessMessage = successMessage ?? "Thank you for subscribing!"
        };

        return View(model);
    }
}

/// <summary>
/// View model for subscription form.
/// </summary>
public class SubscriptionFormViewModel
{
    public Guid? NewsletterId { get; set; }
    public string ButtonText { get; set; } = "Subscribe";
    public string PlaceholderText { get; set; } = "Enter your email";
    public bool ShowNameField { get; set; }
    public string SuccessMessage { get; set; } = "Thank you for subscribing!";
    
    // Form fields
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    
    // Status
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
