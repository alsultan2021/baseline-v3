using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// Handles unsubscription confirmation.
/// </summary>
public class UnsubscribeViewComponent(ISubscriptionService subscriptionService) : ViewComponent
{
    /// <summary>
    /// Renders the unsubscribe confirmation.
    /// </summary>
    /// <param name="email">Email to unsubscribe.</param>
    /// <param name="hash">Verification hash.</param>
    /// <param name="newsletterId">Optional specific newsletter ID.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        string email,
        string hash,
        Guid? newsletterId = null)
    {
        var result = await subscriptionService.ValidateUnsubscribeRequestAsync(email, hash);

        var model = new UnsubscribeViewModel
        {
            Email = email,
            Hash = hash,
            NewsletterId = newsletterId,
            IsValid = result.IsValid,
            ErrorMessage = result.ErrorMessage
        };

        return View(model);
    }
}

/// <summary>
/// View model for unsubscribe.
/// </summary>
public class UnsubscribeViewModel
{
    public string? Email { get; set; }
    public string? Hash { get; set; }
    public Guid? NewsletterId { get; set; }
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsConfirmed { get; set; }
}
