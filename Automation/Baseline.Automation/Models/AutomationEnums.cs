namespace Baseline.Automation;

/// <summary>
/// Types of automation triggers that can start a process.
/// Covers Kentico-equivalent triggers plus generic extensible triggers.
/// </summary>
public enum AutomationTriggerType
{
    // --- Kentico-equivalent triggers ---

    /// <summary>Form submission trigger.</summary>
    FormSubmission,

    /// <summary>Member registration trigger.</summary>
    MemberRegistration,

    /// <summary>Custom activity logged trigger.</summary>
    CustomActivity,

    // --- Generic extensible triggers ---

    /// <summary>Webhook or external system event.</summary>
    Webhook,

    /// <summary>Manual trigger (started via API).</summary>
    Manual,

    /// <summary>Scheduled/time-based trigger.</summary>
    Scheduled,

    // --- Ecommerce-specific triggers ---

    /// <summary>Order has been placed.</summary>
    OrderPlaced,

    /// <summary>Order status has changed.</summary>
    OrderStatusChanged,

    /// <summary>Shopping cart has been abandoned.</summary>
    CartAbandoned,

    /// <summary>Specific product has been purchased.</summary>
    ProductPurchased,

    /// <summary>Payment has failed.</summary>
    PaymentFailed,

    /// <summary>Refund has been issued.</summary>
    RefundIssued,

    /// <summary>Product is back in stock.</summary>
    ProductBackInStock,

    /// <summary>Wishlist item has been added or removed.</summary>
    WishlistUpdated,

    /// <summary>Coupon has been used.</summary>
    CouponUsed,

    /// <summary>Subscription has been created.</summary>
    SubscriptionCreated,

    /// <summary>Subscription has been renewed.</summary>
    SubscriptionRenewed,

    /// <summary>Subscription has been cancelled.</summary>
    SubscriptionCancelled,

    /// <summary>Customer loyalty tier has changed.</summary>
    LoyaltyTierChanged,

    /// <summary>Customer reached a spending threshold.</summary>
    SpendingThresholdReached
}

/// <summary>
/// Types of steps within an automation process.
/// Covers Kentico-equivalent steps plus generic extensible steps.
/// </summary>
public enum AutomationStepType
{
    // --- Kentico-equivalent steps ---

    /// <summary>Starting trigger step.</summary>
    Trigger,

    /// <summary>Send an email to the contact.</summary>
    SendEmail,

    /// <summary>Wait for a time interval or until a specific date.</summary>
    Wait,

    /// <summary>Log a custom activity for the contact.</summary>
    LogCustomActivity,

    /// <summary>Set a contact field value.</summary>
    SetContactFieldValue,

    /// <summary>Evaluate a condition and branch.</summary>
    Condition,

    /// <summary>Finish step (process ends here).</summary>
    Finish,

    // --- Generic extensible steps ---

    /// <summary>Call an external webhook/API.</summary>
    CallWebhook,

    /// <summary>Flag the contact for follow-up.</summary>
    FlagContact,

    /// <summary>Update the contact's segment/contact group.</summary>
    UpdateContactGroup,

    /// <summary>Send an internal notification.</summary>
    SendNotification,

    /// <summary>Sync contact data to a CRM system.</summary>
    SyncToCrm,

    /// <summary>Send an SMS message.</summary>
    SendSms,

    /// <summary>Send a notification email to internal staff.</summary>
    SendNotificationEmail,

    /// <summary>Assign a contact to a sales representative.</summary>
    AssignToSalesRep,

    /// <summary>Log a contact form submission as an activity.</summary>
    LogContactFormSubmission,

    // --- Ecommerce-specific steps ---

    /// <summary>Award loyalty points to the contact/customer.</summary>
    AwardLoyaltyPoints,

    /// <summary>Apply a coupon or discount to the contact.</summary>
    ApplyCoupon,

    /// <summary>Create and send a gift card.</summary>
    CreateGiftCard,

    /// <summary>Update the customer segment/contact group.</summary>
    UpdateCustomerSegment,

    /// <summary>Update an order status.</summary>
    UpdateOrderStatus,

    /// <summary>Send an order notification email.</summary>
    SendOrderNotification,

    /// <summary>Add wallet credit to customer.</summary>
    AddWalletCredit
}

/// <summary>
/// Determines how an automation process handles recurrence for the same contact.
/// Mirrors Kentico's process recurrence settings.
/// </summary>
public enum ProcessRecurrence
{
    /// <summary>
    /// Runs whenever a contact meets trigger conditions, even if already running.
    /// </summary>
    Always,

    /// <summary>
    /// Runs once per contact, never repeats.
    /// </summary>
    OnlyOnce,

    /// <summary>
    /// Runs when triggered, but only if the process isn't currently running for the contact.
    /// Allows re-entry after the contact finishes the process.
    /// </summary>
    IfNotAlreadyRunning
}

/// <summary>
/// Status of a contact within an automation process step.
/// </summary>
public enum ProcessContactStatus
{
    /// <summary>Contact is currently at this step and active.</summary>
    Active,

    /// <summary>Contact has passed through this step.</summary>
    Completed,

    /// <summary>Contact is waiting (in a Wait step).</summary>
    Waiting,

    /// <summary>Process was finished for the contact.</summary>
    Finished,

    /// <summary>Contact was removed from the process.</summary>
    Removed,

    /// <summary>An error occurred processing this contact.</summary>
    Failed
}

/// <summary>
/// Types of conditions for branching within automation processes.
/// </summary>
public enum ConditionType
{
    /// <summary>Check a contact field value.</summary>
    ContactField,

    /// <summary>Check if contact is in a specific contact group.</summary>
    ContactGroup,

    /// <summary>Check if contact has performed a specific activity.</summary>
    ActivityPerformed,

    /// <summary>Check if contact is subscribed to a recipient list.</summary>
    IsSubscribed,

    /// <summary>Check if contact has an active member account.</summary>
    IsMember,

    /// <summary>Check if the contact has given a specific consent.</summary>
    HasConsent,

    /// <summary>Custom expression evaluator.</summary>
    CustomExpression,

    // --- Ecommerce conditions ---

    /// <summary>Check total order value.</summary>
    OrderValueThreshold,

    /// <summary>Check if contact has purchased a specific product.</summary>
    HasPurchasedProduct,

    /// <summary>Check the number of orders placed by the contact.</summary>
    OrderCountThreshold,

    /// <summary>Check if cart contains specific items.</summary>
    CartContainsProduct,

    /// <summary>Check loyalty points balance.</summary>
    LoyaltyPointsThreshold,

    /// <summary>Check wallet balance.</summary>
    WalletBalanceThreshold,

    /// <summary>Check if contact has an active subscription.</summary>
    HasActiveSubscription
}

/// <summary>
/// Comparison operators for condition evaluation.
/// </summary>
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    IsEmpty,
    IsNotEmpty
}
