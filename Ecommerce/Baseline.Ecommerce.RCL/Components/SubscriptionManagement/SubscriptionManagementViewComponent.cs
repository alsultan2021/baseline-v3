using Microsoft.AspNetCore.Mvc;

namespace Baseline.Ecommerce.Components;

/// <summary>
/// Renders subscription management for logged-in users.
/// </summary>
public class SubscriptionManagementViewComponent(ISubscriptionService subscriptionService) : ViewComponent
{
    /// <summary>
    /// Renders the subscription management interface.
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var email = HttpContext.User.Identity?.Name;
        
        if (string.IsNullOrEmpty(email))
        {
            return View("NotLoggedIn");
        }

        var subscriptions = await subscriptionService.GetSubscriptionsAsync(email);

        var model = new SubscriptionManagementViewModel
        {
            Email = email,
            Subscriptions = subscriptions
        };

        return View(model);
    }
}

/// <summary>
/// View model for subscription management.
/// </summary>
public class SubscriptionManagementViewModel
{
    public string? Email { get; set; }
    public IReadOnlyList<SubscriptionInfo> Subscriptions { get; set; } = [];
}
