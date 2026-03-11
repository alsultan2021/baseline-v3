namespace Account.Features.Account.LogIn;

/// <summary>
/// Two-factor authentication view model
/// </summary>
public class TwoFormAuthenticationViewModel
{
    public string? Code { get; set; }
    public bool RememberDevice { get; set; }
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    // Additional properties used by chevalroyal views
    public string? LoginUrl { get; set; }

    // Additional properties for v3 controller compatibility (stored in TempData)
    public string? UserName { get; set; }
    public bool StayLoggedIn { get; set; }
}
