using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Baseline.Automation.Triggers;

/// <summary>
/// Trigger handler for Form Submission events.
/// </summary>
public class FormSubmissionTriggerHandler(
    ILogger<FormSubmissionTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.FormSubmission;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<FormSubmissionTriggerConfig>();
        if (config == null)
        {
            logger.LogWarning("FormSubmission trigger has no configuration, matching all forms");
            return Task.FromResult(true);
        }

        var triggerFormData = ParseTriggerData(eventData.Data);
        var formCodeName = triggerFormData?.GetValueOrDefault("FormCodeName");

        var matches = string.Equals(config.FormCodeName, formCodeName, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(matches);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

/// <summary>
/// Trigger handler for Member Registration events.
/// </summary>
public class MemberRegistrationTriggerHandler(
    ILogger<MemberRegistrationTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.MemberRegistration;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        logger.LogDebug("MemberRegistration trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }
}

/// <summary>
/// Trigger handler for Custom Activity events.
/// </summary>
public class CustomActivityTriggerHandler(
    ILogger<CustomActivityTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.CustomActivity;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<CustomActivityTriggerConfig>();
        if (config == null)
        {
            return Task.FromResult(true);
        }

        var triggerData = ParseTriggerData(eventData.Data);
        var activityType = triggerData?.GetValueOrDefault("ActivityTypeName");

        var matches = string.Equals(config.ActivityTypeName, activityType, StringComparison.OrdinalIgnoreCase);
        logger.LogDebug(
            "CustomActivity trigger {ActivityType} match result: {Matches}",
            config.ActivityTypeName, matches);
        return Task.FromResult(matches);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

/// <summary>
/// Trigger handler for Webhook/External events.
/// </summary>
public class WebhookTriggerHandler(
    ILogger<WebhookTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.Webhook;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<WebhookTriggerConfig>();
        if (config == null)
        {
            return Task.FromResult(true);
        }

        var data = ParseTriggerData(eventData.Data);
        var eventName = data?.GetValueOrDefault("EventName");

        var matches = string.Equals(config.EventName, eventName, StringComparison.OrdinalIgnoreCase);
        logger.LogDebug("Webhook trigger {EventName} match: {Matches}", config.EventName, matches);
        return Task.FromResult(matches);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

// --- Ecommerce Trigger Handlers ---

public class OrderPlacedTriggerHandler(
    ILogger<OrderPlacedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.OrderPlaced;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<OrderPlacedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        if (config.MinOrderValue.HasValue)
        {
            var data = ParseTriggerData(eventData.Data);
            var orderValue = decimal.TryParse(data?.GetValueOrDefault("OrderTotal"), out var v) ? v : 0;
            if (orderValue < config.MinOrderValue.Value)
                return Task.FromResult(false);
        }

        logger.LogDebug("OrderPlaced trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class OrderStatusChangedTriggerHandler(
    ILogger<OrderStatusChangedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.OrderStatusChanged;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<OrderStatusChangedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);
        var toStatus = data?.GetValueOrDefault("ToStatus");
        var fromStatus = data?.GetValueOrDefault("FromStatus");

        if (!string.Equals(config.ToStatus, toStatus, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        if (config.FromStatus != null &&
            !string.Equals(config.FromStatus, fromStatus, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        logger.LogDebug("OrderStatusChanged trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class CartAbandonedTriggerHandler(
    ILogger<CartAbandonedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.CartAbandoned;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<CartAbandonedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);

        // Check minimum cart value
        if (config.MinCartValue.HasValue)
        {
            var cartValue = decimal.TryParse(data?.GetValueOrDefault("CartTotal"), out var v) ? v : 0;
            if (cartValue < config.MinCartValue.Value)
                return Task.FromResult(false);
        }

        // Check returning customer exclusion
        if (config.ExcludeReturningCustomers)
        {
            var isReturning = bool.TryParse(data?.GetValueOrDefault("IsReturningCustomer"), out var r) && r;
            if (isReturning)
                return Task.FromResult(false);
        }

        // Check max reminders
        if (config.MaxReminders > 0)
        {
            var remindersSent = int.TryParse(data?.GetValueOrDefault("RemindersSent"), out var count) ? count : 0;
            if (remindersSent >= config.MaxReminders)
                return Task.FromResult(false);
        }

        logger.LogDebug("CartAbandoned trigger matched for contact {ContactId} (delay: {Delay}m)",
            eventData.ContactId, config.AbandonmentMinutes);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class ProductPurchasedTriggerHandler(
    ILogger<ProductPurchasedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.ProductPurchased;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<ProductPurchasedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);
        var productId = int.TryParse(data?.GetValueOrDefault("ProductId"), out var pid) ? pid : 0;
        var matches = config.ProductIds.Contains(productId);
        logger.LogDebug("ProductPurchased trigger match: {Matches} for product {ProductId}", matches, productId);
        return Task.FromResult(matches);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class PaymentFailedTriggerHandler(
    ILogger<PaymentFailedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.PaymentFailed;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        logger.LogDebug("PaymentFailed trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }
}

public class RefundIssuedTriggerHandler(
    ILogger<RefundIssuedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.RefundIssued;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<RefundIssuedTriggerConfig>();
        if (config?.MinRefundAmount.HasValue == true)
        {
            var data = ParseTriggerData(eventData.Data);
            var amount = decimal.TryParse(data?.GetValueOrDefault("RefundAmount"), out var v) ? v : 0;
            if (amount < config.MinRefundAmount.Value)
                return Task.FromResult(false);
        }

        logger.LogDebug("RefundIssued trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class ProductBackInStockTriggerHandler(
    ILogger<ProductBackInStockTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.ProductBackInStock;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<ProductBackInStockTriggerConfig>();
        if (config?.ProductIds is { Count: > 0 })
        {
            var data = ParseTriggerData(eventData.Data);
            var productId = int.TryParse(data?.GetValueOrDefault("ProductId"), out var pid) ? pid : 0;
            if (!config.ProductIds.Contains(productId))
                return Task.FromResult(false);
        }

        logger.LogDebug("ProductBackInStock trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class WishlistUpdatedTriggerHandler(
    ILogger<WishlistUpdatedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.WishlistUpdated;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<WishlistUpdatedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);

        if (config.OnAddOnly.HasValue)
        {
            var isAdd = string.Equals(data?.GetValueOrDefault("Action"), "Add", StringComparison.OrdinalIgnoreCase);
            if (config.OnAddOnly.Value != isAdd)
                return Task.FromResult(false);
        }

        if (config.ProductIds is { Count: > 0 })
        {
            var productId = int.TryParse(data?.GetValueOrDefault("ProductId"), out var pid) ? pid : 0;
            if (!config.ProductIds.Contains(productId))
                return Task.FromResult(false);
        }

        logger.LogDebug("WishlistUpdated trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class CouponUsedTriggerHandler(
    ILogger<CouponUsedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.CouponUsed;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<CouponUsedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);

        if (!string.IsNullOrEmpty(config.CouponCode))
        {
            var code = data?.GetValueOrDefault("CouponCode");
            if (!string.Equals(config.CouponCode, code, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(false);
        }

        logger.LogDebug("CouponUsed trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class SubscriptionCreatedTriggerHandler(
    ILogger<SubscriptionCreatedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.SubscriptionCreated;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        logger.LogDebug("SubscriptionCreated trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }
}

public class SubscriptionRenewedTriggerHandler(
    ILogger<SubscriptionRenewedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.SubscriptionRenewed;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        logger.LogDebug("SubscriptionRenewed trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }
}

public class SubscriptionCancelledTriggerHandler(
    ILogger<SubscriptionCancelledTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.SubscriptionCancelled;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        logger.LogDebug("SubscriptionCancelled trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }
}

public class LoyaltyTierChangedTriggerHandler(
    ILogger<LoyaltyTierChangedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.LoyaltyTierChanged;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<LoyaltyTierChangedTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);
        var newTier = data?.GetValueOrDefault("NewTier");

        if (!string.IsNullOrEmpty(config.NewTier) &&
            !string.Equals(config.NewTier, newTier, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        logger.LogDebug("LoyaltyTierChanged trigger matched for contact {ContactId}", eventData.ContactId);
        return Task.FromResult(true);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}

public class SpendingThresholdReachedTriggerHandler(
    ILogger<SpendingThresholdReachedTriggerHandler> logger) : IAutomationTriggerHandler
{
    public AutomationTriggerType TriggerType => AutomationTriggerType.SpendingThresholdReached;

    public Task<bool> MatchesAsync(AutomationTrigger trigger, TriggerEventData eventData)
    {
        var config = trigger.GetConfiguration<SpendingThresholdTriggerConfig>();
        if (config == null)
            return Task.FromResult(true);

        var data = ParseTriggerData(eventData.Data);
        var totalSpent = decimal.TryParse(data?.GetValueOrDefault("TotalSpent"), out var v) ? v : 0;
        var matches = totalSpent >= config.ThresholdAmount;
        logger.LogDebug("SpendingThreshold trigger match: {Matches} ({TotalSpent} >= {Threshold})",
            matches, totalSpent, config.ThresholdAmount);
        return Task.FromResult(matches);
    }

    private static Dictionary<string, string>? ParseTriggerData(string? data) =>
        string.IsNullOrEmpty(data) ? null :
        JsonSerializer.Deserialize<Dictionary<string, string>>(data);
}
