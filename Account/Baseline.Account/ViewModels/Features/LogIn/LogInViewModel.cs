using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace Account.Features.Account.LogIn;

/// <summary>
/// LogIn view model
/// </summary>
public class LogInViewModel
{
    public string? UserName { get; set; }
    [JsonIgnore]
    public string? Password { get; set; }
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    // Additional properties used by chevalroyal views
    public bool StayLogedIn { get; set; }
    public bool AlreadyLogedIn { get; set; }
    public string? MyAccountUrl { get; set; }
    public string? ForgotPassword { get; set; }
    public string? RegistrationUrl { get; set; }
    public string? ResendConfirmationToken { get; set; }

    /// <summary>
    /// Serializable sign-in result state for TempData persistence.
    /// Use <see cref="ResultOfSignIn"/> for runtime access.
    /// </summary>
    public SerializableSignInResult? SignInResultState { get; set; }

    /// <summary>
    /// Gets or sets the sign-in result. 
    /// Reading this property will reconstruct from <see cref="SignInResultState"/> if available.
    /// Setting this property will also update <see cref="SignInResultState"/>.
    /// </summary>
    [JsonIgnore]
    public SignInResult? ResultOfSignIn
    {
        get => SignInResultState?.ToSignInResult();
        set => SignInResultState = value != null ? SerializableSignInResult.FromSignInResult(value) : null;
    }
}
