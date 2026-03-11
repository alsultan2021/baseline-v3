using CMS.Membership;
using Kentico.Membership;

namespace Baseline.Account;

/// <summary>
/// Extended application user with baseline member properties.
/// Maps custom member fields like FirstName, MiddleName, LastName, PreferredCurrency.
/// Also includes 2FA fields for two-factor authentication support.
/// Note: Phone and Company are stored on CustomerAddress, not Member.
/// </summary>
public class ApplicationUserBaseline : ApplicationUser
{
    public ApplicationUserBaseline() { }

    public int MemberId { get; set; }

    public string? MemberFirstName { get; set; }

    public string? MemberMiddleName { get; set; }

    public string? MemberLastName { get; set; }

    /// <summary>
    /// User's preferred currency ID (references Baseline_Currency table).
    /// If null, the channel's default currency will be used.
    /// </summary>
    public int? MemberPreferredCurrency { get; set; }

    public Guid? MemberGuid { get; set; }

    /// <summary>
    /// The authenticator key for TOTP-based 2FA.
    /// </summary>
    public string? MemberAuthenticatorKey { get; set; }

    /// <summary>
    /// Semicolon-separated recovery codes for 2FA backup access.
    /// </summary>
    public string? MemberRecoveryCodes { get; set; }

    public override void MapToMemberInfo(MemberInfo target)
    {
        base.MapToMemberInfo(target);

        target.SetValue("MemberFirstName", MemberFirstName);
        target.SetValue("MemberMiddleName", MemberMiddleName);
        target.SetValue("MemberLastName", MemberLastName);
        target.SetValue("MemberPreferredCurrency", MemberPreferredCurrency);
        target.SetValue("MemberAuthenticatorKey", MemberAuthenticatorKey);
        target.SetValue("MemberRecoveryCodes", MemberRecoveryCodes);
    }

    public override void MapFromMemberInfo(MemberInfo source)
    {
        base.MapFromMemberInfo(source);

        MemberId = source.MemberID;
        MemberGuid = source.MemberGuid;

        MemberFirstName = source.GetValue<string?>("MemberFirstName", null);
        MemberMiddleName = source.GetValue<string?>("MemberMiddleName", null);
        MemberLastName = source.GetValue<string?>("MemberLastName", null);
        var currencyValue = source.GetValue<int>("MemberPreferredCurrency", 0);
        MemberPreferredCurrency = currencyValue > 0 ? currencyValue : null;
        MemberAuthenticatorKey = source.GetValue<string?>("MemberAuthenticatorKey", null);
        MemberRecoveryCodes = source.GetValue<string?>("MemberRecoveryCodes", null);
    }
}
