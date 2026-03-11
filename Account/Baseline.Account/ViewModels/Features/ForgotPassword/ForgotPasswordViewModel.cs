namespace Account.Features.Account.ForgotPassword;

/// <summary>
/// ForgotPassword view model
/// </summary>
public class ForgotPasswordViewModel
{
    public string? Email { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }

    // Additional properties used by chevalroyal views
    public bool? Succeeded { get; set; }
    public string? Error { get; set; }
    public string? EmailAddress { get; set; }
}
