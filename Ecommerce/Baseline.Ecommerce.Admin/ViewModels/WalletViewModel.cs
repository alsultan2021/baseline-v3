using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Membership;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Ecommerce.Admin.ViewModels;

/// <summary>
/// View model for Wallet create/edit forms.
/// </summary>
public class WalletViewModel
{
    /// <summary>
    /// Wallet ID (primary key).
    /// </summary>
    public int WalletID { get; set; }

    /// <summary>
    /// Wallet GUID.
    /// </summary>
    public Guid WalletGuid { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Member ID who owns this wallet (stored as string for dropdown compatibility).
    /// </summary>
    [DropDownComponent(
        Label = "Member",
        ExplanationText = "The member who owns this wallet",
        DataProviderType = typeof(MemberDropdownProvider),
        Order = 1)]
    [RequiredValidationRule(ErrorMessage = "Member is required")]
    public string WalletMemberID { get; set; } = string.Empty;

    /// <summary>
    /// Currency ID for this wallet's balance (stored as string for dropdown compatibility).
    /// </summary>
    [DropDownComponent(
        Label = "Currency",
        ExplanationText = "The currency for this wallet's balance",
        DataProviderType = typeof(CurrencyDropdownProvider),
        Order = 2)]
    [RequiredValidationRule(ErrorMessage = "Currency is required")]
    public string WalletCurrencyID { get; set; } = string.Empty;

    /// <summary>
    /// Wallet type: StoreCredit, LoyaltyPoints, PrepaidFunds, GiftCard.
    /// </summary>
    [DropDownComponent(
        Label = "Wallet Type",
        ExplanationText = "Type of wallet (StoreCredit, LoyaltyPoints, PrepaidFunds, GiftCard)",
        DataProviderType = typeof(WalletTypeDropdownProvider),
        Order = 3)]
    [RequiredValidationRule(ErrorMessage = "Wallet type is required")]
    public string WalletType { get; set; } = "StoreCredit";

    /// <summary>
    /// Current total balance in the wallet.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Balance",
        ExplanationText = "Current total balance in the wallet",
        Order = 4)]
    public decimal WalletBalance { get; set; }

    /// <summary>
    /// Balance on hold (reserved for pending transactions).
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Held Balance",
        ExplanationText = "Balance currently on hold for pending transactions",
        Order = 5)]
    public decimal WalletHeldBalance { get; set; }

    /// <summary>
    /// Optional credit limit for credit-type wallets.
    /// </summary>
    [DecimalNumberInputComponent(
        Label = "Credit Limit",
        ExplanationText = "Optional credit limit (leave empty for no limit)",
        Order = 6)]
    public decimal? WalletCreditLimit { get; set; }

    /// <summary>
    /// Whether the wallet is enabled for transactions.
    /// </summary>
    [CheckBoxComponent(
        Label = "Enabled",
        ExplanationText = "If unchecked, this wallet cannot be used for transactions",
        Order = 7)]
    public bool WalletEnabled { get; set; } = true;

    /// <summary>
    /// Whether the wallet is frozen (fraud prevention, disputes).
    /// </summary>
    [CheckBoxComponent(
        Label = "Frozen",
        ExplanationText = "Frozen wallets cannot perform any transactions",
        Order = 8)]
    public bool WalletFrozen { get; set; }

    /// <summary>
    /// Reason for freezing the wallet (if frozen).
    /// </summary>
    [TextAreaComponent(
        Label = "Freeze Reason",
        ExplanationText = "Reason for freezing the wallet",
        Order = 9)]
    [VisibleIfTrue(nameof(WalletFrozen))]
    public string? WalletFreezeReason { get; set; }

    /// <summary>
    /// Optional expiration date for time-limited balances.
    /// </summary>
    [DateInputComponent(
        Label = "Expiration Date",
        ExplanationText = "Optional expiration date for the wallet balance",
        Order = 10)]
    public DateTime? WalletExpiresAt { get; set; }
}

/// <summary>
/// Provides wallet type options for dropdown from "WalletType" taxonomy.
/// Falls back to hardcoded values if taxonomy doesn't exist.
/// </summary>
public class WalletTypeDropdownProvider(
    IInfoProvider<TaxonomyInfo> taxonomyInfoProvider,
    IInfoProvider<TagInfo> tagInfoProvider) : IDropDownOptionsProvider
{
    /// <summary>
    /// The taxonomy code name for wallet types.
    /// </summary>
    public const string WalletTypeTaxonomyName = "WalletType";

    private readonly IInfoProvider<TaxonomyInfo> _taxonomyInfoProvider = taxonomyInfoProvider;
    private readonly IInfoProvider<TagInfo> _tagInfoProvider = tagInfoProvider;

    public async Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        // Try to get wallet types from taxonomy
        var taxonomies = await _taxonomyInfoProvider.Get()
            .WhereEquals(nameof(TaxonomyInfo.TaxonomyName), WalletTypeTaxonomyName)
            .GetEnumerableTypedResultAsync();

        var taxonomy = taxonomies.FirstOrDefault();

        if (taxonomy is not null)
        {
            var tags = await _tagInfoProvider.Get()
                .WhereEquals(nameof(TagInfo.TagTaxonomyID), taxonomy.TaxonomyID)
                .OrderBy(nameof(TagInfo.TagOrder))
                .GetEnumerableTypedResultAsync();

            var items = tags.Select(t => new DropDownOptionItem
            {
                Text = t.TagTitle,
                Value = t.TagName
            }).ToList();

            if (items.Count != 0)
            {
                return items;
            }
        }

        // Fallback to hardcoded values if taxonomy doesn't exist or has no tags
        return
        [
            new() { Text = "Store Credit", Value = "StoreCredit" },
            new() { Text = "Loyalty Points", Value = "LoyaltyPoints" },
            new() { Text = "Prepaid Funds", Value = "PrepaidFunds" },
            new() { Text = "Gift Card", Value = "GiftCard" }
        ];
    }
}

/// <summary>
/// Provides member options for dropdown selection.
/// </summary>
public class MemberDropdownProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Text = "-- Select member --", Value = string.Empty }
        };

        var members = Provider<MemberInfo>.Instance.Get()
            .OrderBy(nameof(MemberInfo.MemberEmail))
            .TopN(100) // Limit for performance
            .ToList();

        items.AddRange(members.Select(m => new DropDownOptionItem
        {
            Text = $"{m.MemberEmail} ({m.MemberName})",
            Value = m.MemberID.ToString()
        }));

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Provides currency options for dropdown selection.
/// </summary>
public class CurrencyDropdownProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Text = "-- Select currency --", Value = string.Empty }
        };

        var dataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.Currency") != null;

        if (!dataClassExists)
        {
            items.Add(new DropDownOptionItem { Text = "Currency not configured", Value = string.Empty });
            return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
        }

        var currencies = Provider<CurrencyInfo>.Instance.Get()
            .WhereEquals(nameof(CurrencyInfo.CurrencyEnabled), true)
            .OrderBy(nameof(CurrencyInfo.CurrencyOrder))
            .ToList();

        items.AddRange(currencies.Select(c => new DropDownOptionItem
        {
            Text = $"{c.CurrencyCode} - {c.CurrencyDisplayName}",
            Value = c.CurrencyID.ToString()
        }));

        if (items.Count == 1)
        {
            items.Add(new DropDownOptionItem { Text = "No currencies available", Value = string.Empty });
        }

        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
