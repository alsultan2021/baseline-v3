namespace Account.Features.Account.Registration;

/// <summary>
/// Registration view model
/// </summary>
public class RegistrationViewModel
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool AcceptTerms { get; set; }
    public string? ErrorMessage { get; set; }

    // Additional properties used by chevalroyal views
    public bool? RegistrationSuccessful { get; set; }
    public string? RegistrationFailureMessage { get; set; }
    public RegistrationUserModel User { get; set; } = new();
    public string? PasswordConfirm { get; set; }
    public bool AgreeToTerms { get; set; }
}
