#nullable disable
// Extended user store for 2FA support
using CMS.Membership;
using Kentico.Membership;
using Microsoft.AspNetCore.Identity;

namespace Baseline.Account.Services;

/// <summary>
/// Extended user store that inherits from Kentico's ApplicationUserStore
/// and adds IUserAuthenticatorKeyStore and IUserTwoFactorRecoveryCodeStore for 2FA support.
/// </summary>
/// <typeparam name="TUser">User type that extends ApplicationUserBaseline.</typeparam>
public class TwoFactorUserStore<TUser> : ApplicationUserStore<TUser>,
    IUserAuthenticatorKeyStore<TUser>,
    IUserTwoFactorRecoveryCodeStore<TUser>
    where TUser : ApplicationUserBaseline, new()
{
    public TwoFactorUserStore(
        IMemberInfoProvider memberInfoProvider,
        IMemberExternalLoginInfoProvider memberExternalLoginInfoProvider)
        : base(memberInfoProvider, memberExternalLoginInfoProvider)
    {
    }

    #region IUserAuthenticatorKeyStore

    public Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        user.MemberAuthenticatorKey = key;
        return Task.CompletedTask;
    }

    public Task<string> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        return Task.FromResult(user.MemberAuthenticatorKey);
    }

    #endregion

    #region IUserTwoFactorRecoveryCodeStore

    public Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        // Store codes as semicolon-separated string
        user.MemberRecoveryCodes = string.Join(";", recoveryCodes);
        return Task.CompletedTask;
    }

    public Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(user.MemberRecoveryCodes))
        {
            return Task.FromResult(false);
        }

        var codes = user.MemberRecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Recovery codes are hashed, so we compare directly
        if (codes.Remove(code))
        {
            user.MemberRecoveryCodes = codes.Count > 0 ? string.Join(";", codes) : string.Empty;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(user);

        if (string.IsNullOrEmpty(user.MemberRecoveryCodes))
        {
            return Task.FromResult(0);
        }

        var count = user.MemberRecoveryCodes.Split(';', StringSplitOptions.RemoveEmptyEntries).Length;
        return Task.FromResult(count);
    }

    #endregion
}
