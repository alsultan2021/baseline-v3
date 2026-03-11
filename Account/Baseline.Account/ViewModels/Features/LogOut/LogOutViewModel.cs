namespace Account.Features.Account.LogOut;

/// <summary>
/// LogOut view model
/// </summary>
public class LogOutViewModel
{
    public string? Message { get; set; }
    public string? RedirectUrl { get; set; }

    // Additional properties used by chevalroyal views
    public bool IsSignedIn { get; set; }
}
