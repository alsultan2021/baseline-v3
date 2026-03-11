namespace Baseline.Ecommerce.Models;

/// <summary>
/// Represents the steps in the checkout process.
/// </summary>
public enum CheckoutStep
{
    /// <summary>
    /// Represents the initial step for authentication choice (sign in or continue as guest).
    /// </summary>
    CheckoutStart = 0,

    /// <summary>
    /// Represents the step for entering customer information.
    /// </summary>
    CheckoutCustomer = 1,

    /// <summary>
    /// Represents the step for entering shipping information.
    /// </summary>
    CheckoutShipping = 2,

    /// <summary>
    /// Represents the step for selecting shipping method.
    /// </summary>
    CheckoutShippingMethod = 3,

    /// <summary>
    /// Represents the step for payment selection.
    /// </summary>
    CheckoutPayment = 4,

    /// <summary>
    /// Represents the step for confirming the order.
    /// </summary>
    OrderConfirmation = 5
}
