using System.Text.Json;

namespace Baseline.Automation;

/// <summary>
/// Configuration for the Send Email step.
/// </summary>
public record SendEmailStepConfig
{
    /// <summary>Email channel code name to send from.</summary>
    public required string EmailChannelCodeName { get; init; }

    /// <summary>Email content item GUID to send.</summary>
    public required Guid EmailContentItemGuid { get; init; }

    /// <summary>Email purpose (Automation or FormAutoresponder).</summary>
    public string Purpose { get; init; } = "Automation";
}

/// <summary>
/// Configuration for the Wait step.
/// </summary>
public record WaitStepConfig
{
    /// <summary>Wait for a specific number of minutes.</summary>
    public int? IntervalMinutes { get; init; }

    /// <summary>Wait until a specific date/time.</summary>
    public DateTimeOffset? UntilDate { get; init; }
}

/// <summary>
/// Configuration for the Log Custom Activity step.
/// </summary>
public record LogActivityStepConfig
{
    /// <summary>Activity type code name to log.</summary>
    public required string ActivityTypeName { get; init; }

    /// <summary>Activity title/description.</summary>
    public string? Title { get; init; }

    /// <summary>Activity value.</summary>
    public string? Value { get; init; }
}

/// <summary>
/// Configuration for the Set Contact Field Value step.
/// </summary>
public record SetContactFieldStepConfig
{
    /// <summary>Contact field name to set.</summary>
    public required string FieldName { get; init; }

    /// <summary>Value to set the field to.</summary>
    public required string Value { get; init; }
}

/// <summary>
/// Configuration for the Condition step.
/// </summary>
public record ConditionStepConfig
{
    /// <summary>Type of condition to evaluate.</summary>
    public required ConditionType ConditionType { get; init; }

    /// <summary>Field name or identifier for the condition (context-dependent).</summary>
    public string? FieldName { get; init; }

    /// <summary>Comparison operator.</summary>
    public ComparisonOperator Operator { get; init; } = ComparisonOperator.Equals;

    /// <summary>Value to compare against.</summary>
    public string? CompareValue { get; init; }

    /// <summary>Custom expression string for CustomExpression type.</summary>
    public string? Expression { get; init; }
}

/// <summary>
/// Configuration for the Flag Contact step.
/// </summary>
public record FlagContactStepConfig
{
    /// <summary>Custom contact field name to use as the flag.</summary>
    public required string FlagFieldName { get; init; }

    /// <summary>Value to set on the flag field.</summary>
    public required string FlagValue { get; init; }

    /// <summary>Optional note or reason for the flag.</summary>
    public string? Note { get; init; }
}

/// <summary>
/// Configuration for the Update Contact Group step.
/// </summary>
public record UpdateContactGroupStepConfig
{
    /// <summary>Contact group code name.</summary>
    public required string ContactGroupCodeName { get; init; }

    /// <summary>Whether to add or remove the contact from the group.</summary>
    public bool Add { get; init; } = true;
}

/// <summary>
/// Configuration for the Call Webhook step.
/// </summary>
public record CallWebhookStepConfig
{
    /// <summary>URL to call.</summary>
    public required string Url { get; init; }

    /// <summary>HTTP method (POST, PUT, etc.).</summary>
    public string Method { get; init; } = "POST";

    /// <summary>Request headers as JSON object.</summary>
    public string? Headers { get; init; }

    /// <summary>Request body template. Supports placeholders like {ContactEmail}, {ContactFirstName}.</summary>
    public string? BodyTemplate { get; init; }

    /// <summary>Timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 30;
}

/// <summary>
/// Configuration for the Send Notification step.
/// </summary>
public record SendNotificationStepConfig
{
    /// <summary>Comma-separated recipient email addresses.</summary>
    public string RecipientEmails { get; init; } = string.Empty;

    /// <summary>Email subject template (supports macros).</summary>
    public string EmailSubject { get; init; } = "Automation Notification";

    /// <summary>Email template code name.</summary>
    public string EmailTemplate { get; init; } = string.Empty;

    /// <summary>Whether to include trigger data in the email body.</summary>
    public bool IncludeTriggerData { get; init; } = true;

    /// <summary>Email priority: Normal, High, Low.</summary>
    public string Priority { get; init; } = "Normal";
}

/// <summary>
/// Configuration for the Sync to CRM step.
/// </summary>
public record SyncToCrmStepConfig
{
    public string CrmSystem { get; init; } = "Default";
    public string LeadSource { get; init; } = "Automation";
    public string CampaignId { get; init; } = string.Empty;
    public string SyncMode { get; init; } = "CreateOrUpdate";
}

/// <summary>
/// Configuration for the Send SMS step.
/// </summary>
public record SendSmsStepConfig
{
    public string MessageTemplate { get; init; } = string.Empty;
    public string PhoneNumberField { get; init; } = "ContactMobilePhone";
    public string SenderId { get; init; } = "Kentico";
}

/// <summary>
/// Configuration for the Send Notification Email step.
/// </summary>
public record SendNotificationEmailStepConfig
{
    public string RecipientEmails { get; init; } = string.Empty;
    public string EmailSubject { get; init; } = "New Contact Form Submission";
    public string EmailTemplate { get; init; } = string.Empty;
    public bool IncludeFormData { get; init; } = true;
    public string Priority { get; init; } = "Normal";
    public string CcEmails { get; init; } = string.Empty;
}

/// <summary>
/// Configuration for the Assign to Sales Rep step.
/// </summary>
public record AssignToSalesRepStepConfig
{
    public string AssignmentStrategy { get; init; } = "RoundRobin";
    public int SalesRepUserId { get; init; }
    public string TerritoryField { get; init; } = "ContactCountryID";
    public string SalesTeamRoleName { get; init; } = "SalesTeam";
    public bool NotifyAssignee { get; init; } = true;
    public string LeadSource { get; init; } = "Contact Form";
    public string Priority { get; init; } = "Normal";
}

/// <summary>
/// Configuration for the Log Contact Form Submission step.
/// </summary>
public record LogContactFormSubmissionStepConfig
{
    public string FormCodeName { get; init; } = "ContactForm";
    public string ActivityTitle { get; init; } = string.Empty;
    public string ActivityValue { get; init; } = string.Empty;
    public bool IncludeFormData { get; init; } = true;
}

// --- Trigger configurations ---

/// <summary>
/// Configuration for Form Submission trigger.
/// </summary>
public record FormSubmissionTriggerConfig
{
    /// <summary>Form code name that triggers the process.</summary>
    public required string FormCodeName { get; init; }
}

/// <summary>
/// Configuration for Custom Activity trigger.
/// </summary>
public record CustomActivityTriggerConfig
{
    /// <summary>Activity type code name that triggers the process.</summary>
    public required string ActivityTypeName { get; init; }
}

/// <summary>
/// Configuration for Webhook trigger.
/// </summary>
public record WebhookTriggerConfig
{
    /// <summary>Expected webhook event type/name.</summary>
    public required string EventName { get; init; }

    /// <summary>Optional secret for webhook validation.</summary>
    public string? Secret { get; init; }
}

// --- Ecommerce step configurations ---

/// <summary>
/// Configuration for the Award Loyalty Points step.
/// </summary>
public record AwardLoyaltyPointsStepConfig
{
    /// <summary>Number of points to award.</summary>
    public required int Points { get; init; }

    /// <summary>Reason for the award.</summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Configuration for the Apply Coupon step.
/// </summary>
public record ApplyCouponStepConfig
{
    /// <summary>Coupon code to apply/send to the contact.</summary>
    public required string CouponCode { get; init; }

    /// <summary>Whether to generate a unique coupon code for each contact.</summary>
    public bool GenerateUnique { get; init; }

    /// <summary>Promotion ID to associate with the generated coupon.</summary>
    public int? PromotionId { get; init; }
}

/// <summary>
/// Configuration for the Create Gift Card step.
/// </summary>
public record CreateGiftCardStepConfig
{
    /// <summary>Gift card value in the default currency.</summary>
    public required decimal Value { get; init; }

    /// <summary>Whether to email the gift card to the contact.</summary>
    public bool SendEmail { get; init; } = true;

    /// <summary>Expiration days from creation. Null for no expiration.</summary>
    public int? ExpirationDays { get; init; }
}

/// <summary>
/// Configuration for the Update Customer Segment step.
/// </summary>
public record UpdateCustomerSegmentStepConfig
{
    /// <summary>Contact group code name to add the contact to.</summary>
    public required string ContactGroupCodeName { get; init; }

    /// <summary>Whether to add or remove the contact from the group.</summary>
    public bool Add { get; init; } = true;
}

/// <summary>
/// Configuration for the Update Order Status step.
/// </summary>
public record UpdateOrderStatusStepConfig
{
    /// <summary>New status to set on the order.</summary>
    public required string NewStatus { get; init; }

    /// <summary>Whether to send a notification to the customer.</summary>
    public bool SendNotification { get; init; } = true;
}

/// <summary>
/// Configuration for the Add Wallet Credit step.
/// </summary>
public record AddWalletCreditStepConfig
{
    /// <summary>Amount to credit to the wallet.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Wallet type (StoreCredit, LoyaltyPoints, etc.).</summary>
    public string WalletType { get; init; } = "StoreCredit";

    /// <summary>Transaction reason.</summary>
    public string? Reason { get; init; }
}

// --- Ecommerce trigger configurations ---

/// <summary>
/// Configuration for Order Placed trigger.
/// </summary>
public record OrderPlacedTriggerConfig
{
    /// <summary>Minimum order value to trigger (optional).</summary>
    public decimal? MinOrderValue { get; init; }

    /// <summary>Specific product IDs that must be in the order (optional).</summary>
    public IList<int>? RequiredProductIds { get; init; }
}

/// <summary>
/// Configuration for Cart Abandoned trigger.
/// </summary>
public record CartAbandonedTriggerConfig
{
    /// <summary>Delay value before cart is considered abandoned.</summary>
    public int AbandonmentValue { get; init; } = 1;

    /// <summary>Unit for the abandonment delay (minutes, hours, days).</summary>
    public string AbandonmentUnit { get; init; } = "hours";

    /// <summary>Computed abandonment delay in minutes.</summary>
    public int AbandonmentMinutes => AbandonmentUnit switch
    {
        "hours" => AbandonmentValue * 60,
        "days" => AbandonmentValue * 1440,
        _ => AbandonmentValue
    };

    /// <summary>Minimum cart value to trigger.</summary>
    public decimal? MinCartValue { get; init; }

    /// <summary>Whether to exclude returning/repeat customers.</summary>
    public bool ExcludeReturningCustomers { get; init; }

    /// <summary>Maximum number of reminders per contact (0 = unlimited).</summary>
    public int MaxReminders { get; init; } = 3;
}

/// <summary>
/// Configuration for Order Status Changed trigger.
/// </summary>
public record OrderStatusChangedTriggerConfig
{
    /// <summary>Status the order changed from (optional, any if null).</summary>
    public string? FromStatus { get; init; }

    /// <summary>Status the order changed to.</summary>
    public required string ToStatus { get; init; }
}

/// <summary>
/// Configuration for Product Purchased trigger.
/// </summary>
public record ProductPurchasedTriggerConfig
{
    /// <summary>Product content item IDs to watch.</summary>
    public required IList<int> ProductIds { get; init; }
}

/// <summary>
/// Configuration for Spending Threshold trigger.
/// </summary>
public record SpendingThresholdTriggerConfig
{
    /// <summary>Total lifetime spending amount that triggers the process.</summary>
    public required decimal ThresholdAmount { get; init; }

    /// <summary>Currency code for the threshold.</summary>
    public string CurrencyCode { get; init; } = "USD";
}

/// <summary>
/// Configuration for Refund Issued trigger.
/// </summary>
public record RefundIssuedTriggerConfig
{
    /// <summary>Minimum refund amount to trigger (optional).</summary>
    public decimal? MinRefundAmount { get; init; }
}

/// <summary>
/// Configuration for Product Back In Stock trigger.
/// </summary>
public record ProductBackInStockTriggerConfig
{
    /// <summary>Specific product content item IDs to watch (optional, all if null).</summary>
    public IList<int>? ProductIds { get; init; }
}

/// <summary>
/// Configuration for Wishlist Updated trigger.
/// </summary>
public record WishlistUpdatedTriggerConfig
{
    /// <summary>Specific product IDs to watch for wishlist adds (optional, any if null).</summary>
    public IList<int>? ProductIds { get; init; }

    /// <summary>Whether to trigger only on add (true), remove (false), or both (null).</summary>
    public bool? OnAddOnly { get; init; }
}

/// <summary>
/// Configuration for Coupon Used trigger.
/// </summary>
public record CouponUsedTriggerConfig
{
    /// <summary>Specific coupon code to watch (optional, any if null).</summary>
    public string? CouponCode { get; init; }

    /// <summary>Promotion ID the coupon belongs to (optional).</summary>
    public int? PromotionId { get; init; }
}

/// <summary>
/// Configuration for Loyalty Tier Changed trigger.
/// </summary>
public record LoyaltyTierChangedTriggerConfig
{
    /// <summary>New tier name the contact must have reached (optional, any change if null).</summary>
    public string? NewTier { get; init; }

    /// <summary>Previous tier name (optional, any previous tier if null).</summary>
    public string? PreviousTier { get; init; }
}

/// <summary>
/// Context data passed through the automation process for a specific contact.
/// </summary>
public record AutomationContext
{
    /// <summary>The contact ID being processed.</summary>
    public required int ContactId { get; init; }

    /// <summary>The process being executed.</summary>
    public required AutomationProcess Process { get; init; }

    /// <summary>The current step being executed.</summary>
    public required AutomationStep CurrentStep { get; init; }

    /// <summary>The process contact state.</summary>
    public required ProcessContactState State { get; init; }

    /// <summary>
    /// Trigger-specific context data as JSON.
    /// </summary>
    public string? TriggerData { get; init; }

    /// <summary>
    /// Deserializes trigger data to a typed object.
    /// </summary>
    public T? GetTriggerData<T>() where T : class =>
        string.IsNullOrEmpty(TriggerData) ? null :
        JsonSerializer.Deserialize<T>(TriggerData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}

/// <summary>
/// Result of executing an automation step action.
/// </summary>
public record StepExecutionResult
{
    /// <summary>Whether the step executed successfully.</summary>
    public required bool Success { get; init; }

    /// <summary>Error message if the step failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>For condition steps: the result of the condition evaluation.</summary>
    public bool? ConditionResult { get; init; }

    /// <summary>Optional data to pass to subsequent steps.</summary>
    public string? OutputData { get; init; }

    /// <summary>Whether the engine should wait before advancing (e.g., Wait step).</summary>
    public bool ShouldWait { get; init; }

    /// <summary>If ShouldWait, when to advance to the next step.</summary>
    public DateTimeOffset? WaitUntil { get; init; }

    public static StepExecutionResult Succeeded(string? outputData = null) =>
        new() { Success = true, OutputData = outputData };

    public static StepExecutionResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };

    public static StepExecutionResult ConditionEvaluated(bool result) =>
        new() { Success = true, ConditionResult = result };

    public static StepExecutionResult WaitRequired(DateTimeOffset until) =>
        new() { Success = true, ShouldWait = true, WaitUntil = until };
}

/// <summary>
/// Event data passed when a trigger fires.
/// </summary>
public record TriggerEventData
{
    /// <summary>The trigger type that fired.</summary>
    public required AutomationTriggerType TriggerType { get; init; }

    /// <summary>Contact ID the trigger applies to.</summary>
    public required int ContactId { get; init; }

    /// <summary>
    /// Trigger-specific data as JSON (form data, etc.).
    /// </summary>
    public string? Data { get; init; }

    /// <summary>When the trigger event occurred.</summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
