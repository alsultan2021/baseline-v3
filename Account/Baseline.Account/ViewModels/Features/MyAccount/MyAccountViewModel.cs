namespace Account.Features.Account.MyAccount;

/// <summary>
/// MyAccount view model
/// </summary>
public class MyAccountViewModel
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
}
