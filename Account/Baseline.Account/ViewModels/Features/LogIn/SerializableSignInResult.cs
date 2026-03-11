using Microsoft.AspNetCore.Identity;

namespace Account.Features.Account.LogIn;

/// <summary>
/// JSON-serializable representation of <see cref="SignInResult"/>.
/// </summary>
public class SerializableSignInResult
{
    public bool Succeeded { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsNotAllowed { get; set; }
    public bool RequiresTwoFactor { get; set; }

    /// <summary>
    /// Creates a serializable result from an Identity SignInResult.
    /// </summary>
    public static SerializableSignInResult FromSignInResult(SignInResult result)
    {
        return new SerializableSignInResult
        {
            Succeeded = result.Succeeded,
            IsLockedOut = result.IsLockedOut,
            IsNotAllowed = result.IsNotAllowed,
            RequiresTwoFactor = result.RequiresTwoFactor
        };
    }

    /// <summary>
    /// Converts back to an Identity SignInResult.
    /// Note: Returns a new instance with matching property values.
    /// </summary>
    public SignInResult ToSignInResult()
    {
        if (Succeeded) return SignInResult.Success;
        if (IsLockedOut) return SignInResult.LockedOut;
        if (IsNotAllowed) return SignInResult.NotAllowed;
        if (RequiresTwoFactor) return SignInResult.TwoFactorRequired;
        return SignInResult.Failed;
    }
}
