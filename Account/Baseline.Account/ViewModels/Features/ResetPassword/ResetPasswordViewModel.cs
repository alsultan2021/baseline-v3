namespace Account.Features.Account.ResetPassword;

/// <summary>
/// ResetPassword view model
/// </summary>
public class ResetPasswordViewModel
{
    public string? Token { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; }

    // Additional properties used by chevalroyal views
    public string? CurrentPassword { get; set; }
    public string? PasswordConfirm { get; set; }
}
