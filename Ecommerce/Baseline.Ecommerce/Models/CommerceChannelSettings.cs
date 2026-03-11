using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using XperienceCommunity.ChannelSettings.Attributes;

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Channel settings for Commerce module.
/// Configure currency, URLs, and other commerce settings per channel.
/// </summary>
public class CommerceChannelSettings
{
    #region Currency Settings

    /// <summary>
    /// Default currency code (ISO 4217) - selected from available currencies.
    /// </summary>
    [XperienceSettingsData("Commerce.DefaultCurrency", "")]
    [DropDownComponent(
        Label = "Default Currency",
        ExplanationText = "Select the default currency for this channel. Currencies are managed in Commerce > Currencies.",
        DataProviderType = typeof(CurrencyCodeDataProvider),
        Order = 1)]
    public virtual string DefaultCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Currency symbol position (Before/After)
    /// </summary>
    [XperienceSettingsData("Commerce.CurrencySymbolPosition", "Before")]
    [DropDownComponent(
        Label = "Currency Symbol Position",
        ExplanationText = "Where to display the currency symbol relative to the amount",
        DataProviderType = typeof(CurrencyPositionDataProvider),
        Order = 2)]
    public virtual string CurrencySymbolPosition { get; set; } = "Before";

    /// <summary>
    /// Number of decimal places for prices
    /// </summary>
    [XperienceSettingsData("Commerce.DecimalPlaces", 2)]
    [NumberInputComponent(
        Label = "Decimal Places",
        ExplanationText = "Number of decimal places to display for prices",
        Order = 3)]
    public virtual int DecimalPlaces { get; set; } = 2;

    /// <summary>
    /// Thousands separator
    /// </summary>
    [XperienceSettingsData("Commerce.ThousandsSeparator", ",")]
    [TextInputComponent(
        Label = "Thousands Separator",
        ExplanationText = "Character used to separate thousands (e.g., comma or period)",
        Order = 4)]
    public virtual string ThousandsSeparator { get; set; } = ",";

    /// <summary>
    /// Decimal separator
    /// </summary>
    [XperienceSettingsData("Commerce.DecimalSeparator", ".")]
    [TextInputComponent(
        Label = "Decimal Separator",
        ExplanationText = "Character used for decimal point (e.g., period or comma)",
        Order = 5)]
    public virtual string DecimalSeparator { get; set; } = ".";

    #endregion

    #region Tax Settings

    /// <summary>
    /// Whether prices include tax
    /// </summary>
    [XperienceSettingsData("Commerce.PricesIncludeTax", false)]
    [CheckBoxComponent(
        Label = "Prices Include Tax",
        ExplanationText = "If checked, all displayed prices include tax",
        Order = 10)]
    public virtual bool PricesIncludeTax { get; set; } = false;

    /// <summary>
    /// Default tax rate percentage
    /// </summary>
    [XperienceSettingsData("Commerce.DefaultTaxRate", 0.0)]
    [DecimalNumberInputComponent(
        Label = "Default Tax Rate (%)",
        ExplanationText = "Default tax rate as a percentage (e.g., 8.25)",
        Order = 11)]
    public virtual decimal DefaultTaxRate { get; set; } = 0;

    /// <summary>
    /// Calculate tax based on shipping or billing address
    /// </summary>
    [XperienceSettingsData("Commerce.TaxAddressType", "Shipping")]
    [DropDownComponent(
        Label = "Tax Calculation Address",
        ExplanationText = "Use shipping or billing address for tax calculation",
        DataProviderType = typeof(TaxAddressTypeDataProvider),
        Order = 12)]
    public virtual string TaxAddressType { get; set; } = "Shipping";

    #endregion

    #region Page URLs

    /// <summary>
    /// URL to the Store page. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Commerce.StorePagePath", "/Store")]
    [UrlSelectorComponent(
        Label = "Store Page URL",
        ExplanationText = "Select the store landing page from the content tree or enter a URL manually",
        Order = 20)]
    public virtual string StorePagePath { get; set; } = "/Store";

    /// <summary>
    /// URL to the Shopping Cart page. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Commerce.ShoppingCartPagePath", "/Store/Shopping-Cart")]
    [UrlSelectorComponent(
        Label = "Shopping Cart Page URL",
        ExplanationText = "Select the shopping cart page from the content tree or enter a URL manually",
        Order = 21)]
    public virtual string ShoppingCartPagePath { get; set; } = "/Store/Shopping-Cart";

    /// <summary>
    /// URL to the Checkout page. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Commerce.CheckoutPagePath", "/Store/Checkout")]
    [UrlSelectorComponent(
        Label = "Checkout Page URL",
        ExplanationText = "Select the checkout page from the content tree or enter a URL manually",
        Order = 22)]
    public virtual string CheckoutPagePath { get; set; } = "/Store/Checkout";

    // NOTE: Account-related URLs (Login, Registration, ForgotPassword, MyAccount)
    // are managed in AccountChannelSettings from the Baseline.Account module.
    // Use IChannelCustomSettingsRepository.GetSettingsModel<AccountChannelSettings>()
    // to access: AccountLoginUrl, AccountRegistrationUrl, AccountForgotPasswordUrl, AccountMyAccountUrl

    /// <summary>
    /// URL to the Order History page. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Commerce.OrderHistoryPagePath", "/Account/OrderHistory")]
    [UrlSelectorComponent(
        Label = "Order History Page URL",
        ExplanationText = "Select the order history page from the content tree or enter a URL manually",
        Order = 27)]
    public virtual string OrderHistoryPagePath { get; set; } = "/Account/OrderHistory";

    /// <summary>
    /// URL to the Order Confirmation page. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Commerce.OrderConfirmationPagePath", "/Store/Order-Confirmation")]
    [UrlSelectorComponent(
        Label = "Order Confirmation Page URL",
        ExplanationText = "Select the order confirmation/thank you page from the content tree or enter a URL manually",
        Order = 28)]
    public virtual string OrderConfirmationPagePath { get; set; } = "/Store/Order-Confirmation";

    #endregion

    #region Checkout Settings

    /// <summary>
    /// Require account for checkout
    /// </summary>
    [XperienceSettingsData("Commerce.RequireAccountForCheckout", false)]
    [CheckBoxComponent(
        Label = "Require Account for Checkout",
        ExplanationText = "If checked, customers must create an account to complete checkout",
        Order = 30)]
    public virtual bool RequireAccountForCheckout { get; set; } = false;

    /// <summary>
    /// Enable guest checkout
    /// </summary>
    [XperienceSettingsData("Commerce.EnableGuestCheckout", true)]
    [CheckBoxComponent(
        Label = "Enable Guest Checkout",
        ExplanationText = "Allow customers to checkout without creating an account",
        Order = 31)]
    public virtual bool EnableGuestCheckout { get; set; } = true;

    /// <summary>
    /// Minimum order amount
    /// </summary>
    [XperienceSettingsData("Commerce.MinimumOrderAmount", 0.0)]
    [DecimalNumberInputComponent(
        Label = "Minimum Order Amount",
        ExplanationText = "Minimum order total required to checkout (0 for no minimum)",
        Order = 32)]
    public virtual decimal MinimumOrderAmount { get; set; } = 0;

    #endregion

    #region Inventory Settings

    /// <summary>
    /// Enable stock management
    /// </summary>
    [XperienceSettingsData("Commerce.EnableStockManagement", true)]
    [CheckBoxComponent(
        Label = "Enable Stock Management",
        ExplanationText = "Track and manage product inventory levels",
        Order = 40)]
    public virtual bool EnableStockManagement { get; set; } = true;

    /// <summary>
    /// Low stock threshold
    /// </summary>
    [XperienceSettingsData("Commerce.LowStockThreshold", 5)]
    [NumberInputComponent(
        Label = "Low Stock Threshold",
        ExplanationText = "Quantity at which a product is considered low stock",
        Order = 41)]
    public virtual int LowStockThreshold { get; set; } = 5;

    /// <summary>
    /// Allow backorders
    /// </summary>
    [XperienceSettingsData("Commerce.AllowBackorders", false)]
    [CheckBoxComponent(
        Label = "Allow Backorders",
        ExplanationText = "Allow customers to order products that are out of stock",
        Order = 42)]
    public virtual bool AllowBackorders { get; set; } = false;

    /// <summary>
    /// Hide out of stock products
    /// </summary>
    [XperienceSettingsData("Commerce.HideOutOfStock", false)]
    [CheckBoxComponent(
        Label = "Hide Out of Stock Products",
        ExplanationText = "Hide products with zero stock from the catalog",
        Order = 43)]
    public virtual bool HideOutOfStock { get; set; } = false;

    #endregion
}

/// <summary>
/// Data provider for default currency dropdown - loads from CurrencyInfo database table.
/// </summary>
public class CurrencyCodeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>();

        try
        {
            // Check if data class exists before querying
            var dataClass = DataClassInfoProvider.GetDataClassInfo(CurrencyInfo.OBJECT_TYPE);
            if (dataClass != null)
            {
                var currencies = Provider<CurrencyInfo>.Instance.Get()
                    .WhereEquals(nameof(CurrencyInfo.CurrencyEnabled), true)
                    .OrderBy(nameof(CurrencyInfo.CurrencyOrder), nameof(CurrencyInfo.CurrencyDisplayName))
                    .GetEnumerableTypedResult();

                foreach (var currency in currencies)
                {
                    items.Add(new DropDownOptionItem
                    {
                        Value = currency.CurrencyCode,
                        Text = $"{currency.CurrencyDisplayName} ({currency.CurrencyCode})"
                    });
                }
            }
        }
        catch
        {
            // Swallow exception if table doesn't exist yet
        }

        // Add fallback if no currencies found
        if (items.Count == 0)
        {
            items.Add(new DropDownOptionItem { Value = "USD", Text = "US Dollar (USD) - No currencies configured" });
        }

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for currency symbol position dropdown.
/// </summary>
public class CurrencyPositionDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "Before", Text = "Before amount ($100)" },
            new() { Value = "After", Text = "After amount (100$)" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for tax address type dropdown.
/// </summary>
public class TaxAddressTypeDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "Shipping", Text = "Shipping Address" },
            new() { Value = "Billing", Text = "Billing Address" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
