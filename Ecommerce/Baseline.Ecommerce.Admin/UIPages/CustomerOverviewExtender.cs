using CMS.Commerce;
using CMS.DataEngine;
using Baseline.Ecommerce.Models;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.DigitalCommerce.UIPages;

[assembly: PageExtender(typeof(Baseline.Ecommerce.Admin.UIPages.CustomerOverviewExtender))]

namespace Baseline.Ecommerce.Admin.UIPages;

/// <summary>
/// Extends the Digital Commerce Customer Overview page to show wallet balance summary.
/// </summary>
public class CustomerOverviewExtender : PageExtender<CustomerOverview>
{
    private readonly IInfoProvider<WalletInfo> _walletProvider;
    private readonly IInfoProvider<CustomerInfo> _customerProvider;

    public CustomerOverviewExtender(
        IInfoProvider<WalletInfo> walletProvider,
        IInfoProvider<CustomerInfo> customerProvider)
    {
        _walletProvider = walletProvider;
        _customerProvider = customerProvider;
    }

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        // Initialize callouts collection if null
        Page.PageConfiguration.Callouts ??= [];

        // Check if wallet data class exists
        var walletDataClassExists = DataClassInfoProvider.GetDataClassInfo("Baseline.Wallet") != null;
        if (!walletDataClassExists)
        {
            return; // Wallets not installed, skip balance display
        }

        // Get the customer ID from the page's ObjectId parameter
        var customerId = Page.ObjectId;
        if (customerId <= 0)
        {
            return;
        }

        // Get customer to find their member ID
        var customer = await _customerProvider.GetAsync(customerId);
        if (customer == null || customer.CustomerMemberID <= 0)
        {
            return; // No customer or no member association
        }

        var memberId = customer.CustomerMemberID;

        // Get all wallets for this member
        var wallets = await _walletProvider.Get()
            .WhereEquals(nameof(WalletInfo.WalletMemberID), memberId)
            .GetEnumerableTypedResultAsync();

        var walletList = wallets.ToList();

        if (walletList.Count == 0)
        {
            return; // No wallets for this customer
        }

        // Calculate balance summary
        var totalBalance = walletList
            .Where(w => w.WalletEnabled && !w.WalletFrozen)
            .Sum(w => w.WalletBalance);
        var totalHeld = walletList.Sum(w => w.WalletHeldBalance);
        var availableBalance = totalBalance - totalHeld;

        // Build balance summary message
        var balanceSummary = $"<strong>Available Balance:</strong> {availableBalance:C2}";
        if (totalHeld > 0)
        {
            balanceSummary += $" &nbsp;|&nbsp; <strong>Held:</strong> {totalHeld:C2}";
        }
        balanceSummary += $" &nbsp;|&nbsp; <strong>Total:</strong> {totalBalance:C2}";

        // Add wallet type breakdown if multiple wallets exist
        if (walletList.Count > 1)
        {
            var breakdown = string.Join(", ", walletList
                .Where(w => w.WalletEnabled)
                .Select(w => $"{w.WalletType}: {w.WalletBalance:C2}"));
            balanceSummary += $"<br/><small>{breakdown}</small>";
        }

        // Add balance indicator (positive = customer has credit, negative = owes money)
        string headline;
        CalloutType calloutType;

        if (availableBalance > 0)
        {
            headline = "💰 Customer Wallet Balance";
            calloutType = CalloutType.QuickTip;
        }
        else if (availableBalance < 0)
        {
            headline = "⚠️ Customer Owes Balance";
            calloutType = CalloutType.FriendlyWarning;
        }
        else
        {
            headline = "Customer Wallet Balance";
            calloutType = CalloutType.QuickTip;
        }

        Page.PageConfiguration.Callouts.Add(new CalloutConfiguration
        {
            Headline = headline,
            Content = balanceSummary,
            ContentAsHtml = true,
            Type = calloutType,
            Placement = CalloutPlacement.OnDesk
        });
    }
}
