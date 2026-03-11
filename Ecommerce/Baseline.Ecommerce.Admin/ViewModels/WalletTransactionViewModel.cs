using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for WalletTransaction create/view forms.
/// Transactions are generally append-only but admins may need to view and create adjustment transactions.
/// </summary>
public class WalletTransactionViewModel
{
    /// <summary>
    /// Transaction ID (primary key).
    /// </summary>
    public int TransactionID { get; set; }

    /// <summary>
    /// Transaction GUID.
    /// </summary>
    public Guid TransactionGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Wallet ID this transaction belongs to.
    /// </summary>
    [NumberInputComponent(
        Label = "Wallet ID",
        ExplanationText = "The ID of the wallet this transaction belongs to",
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Wallet ID is required")]
    public int TransactionWalletID { get; set; }

    /// <summary>
    /// Transaction type.
    /// </summary>
    [DropDownComponent(
        Label = "Transaction Type",
        ExplanationText = "Type of transaction",
        DataProviderType = typeof(TransactionTypeDropdownProvider),
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Transaction type is required")]
    public string TransactionType { get; set; } = "Adjustment";

    /// <summary>
    /// Transaction amount. Positive for credits (deposits), negative for debits (withdrawals).
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Amount",
        ExplanationText = "Transaction amount (positive for credits, negative for debits)",
        Order = 3)]
    [RequiredValidationRule(ErrorMessage = "Amount is required")]
    public decimal TransactionAmount { get; set; }

    /// <summary>
    /// Transaction status.
    /// </summary>
    [DropDownComponent(
        Label = "Status",
        ExplanationText = "Transaction status",
        DataProviderType = typeof(TransactionStatusDropdownProvider),
        Order = 4)]
    public string TransactionStatus { get; set; } = "Completed";

    /// <summary>
    /// Human-readable description of the transaction.
    /// </summary>
    [TextAreaComponent(
        Label = "Description",
        ExplanationText = "Human-readable description of the transaction",
        Order = 5)]
    public string? TransactionDescription { get; set; }

    /// <summary>
    /// External reference (order number, refund ID, gift card code, etc.).
    /// </summary>
    [TextInputComponent(
        Label = "Reference",
        ExplanationText = "External reference (order number, refund ID, etc.)",
        Order = 6)]
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Optional order ID for order-related transactions.
    /// </summary>
    [NumberInputComponent(
        Label = "Order ID",
        ExplanationText = "Related order ID (if applicable)",
        Order = 7)]
    public int? TransactionOrderID { get; set; }

    /// <summary>
    /// Wallet balance after this transaction was applied.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Balance After",
        ExplanationText = "Wallet balance after this transaction (auto-calculated on create)",
        Order = 8)]
    public decimal TransactionBalanceAfter { get; set; }

    /// <summary>
    /// When the transaction was created.
    /// </summary>
    public DateTime TransactionCreatedWhen { get; set; } = DateTime.Now;

    /// <summary>
    /// User ID who created this transaction.
    /// </summary>
    public int? TransactionCreatedBy { get; set; }
}

/// <summary>
/// Provides transaction type options for dropdown.
/// </summary>
public class TransactionTypeDropdownProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Text = "Deposit", Value = "Deposit" },
            new() { Text = "Withdrawal", Value = "Withdrawal" },
            new() { Text = "Payment", Value = "Payment" },
            new() { Text = "Refund", Value = "Refund" },
            new() { Text = "Hold", Value = "Hold" },
            new() { Text = "Hold Released", Value = "HoldReleased" },
            new() { Text = "Hold Captured", Value = "HoldCaptured" },
            new() { Text = "Adjustment", Value = "Adjustment" },
            new() { Text = "Transfer In", Value = "TransferIn" },
            new() { Text = "Transfer Out", Value = "TransferOut" },
            new() { Text = "Expiration", Value = "Expiration" },
            new() { Text = "Loyalty Earned", Value = "LoyaltyEarned" },
            new() { Text = "Loyalty Redeemed", Value = "LoyaltyRedeemed" }
        };

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Provides transaction status options for dropdown.
/// </summary>
public class TransactionStatusDropdownProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Text = "Pending", Value = "Pending" },
            new() { Text = "Completed", Value = "Completed" },
            new() { Text = "Failed", Value = "Failed" },
            new() { Text = "Cancelled", Value = "Cancelled" },
            new() { Text = "Reversed", Value = "Reversed" }
        };

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
