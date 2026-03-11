using Microsoft.AspNetCore.Mvc;

namespace Baseline.Account.Components;

/// <summary>
/// Registration form view component.
/// </summary>
#pragma warning disable CS9113 // Parameter is unread - reserved for future password validation
public class RegistrationFormViewComponent(IPasswordService passwordService) : ViewComponent
#pragma warning restore CS9113
{
    public IViewComponentResult Invoke(
        bool showLoginLink = true,
        bool requireEmailConfirmation = true)
    {
        var model = new RegistrationFormViewModel
        {
            ShowLoginLink = showLoginLink,
            RequireEmailConfirmation = requireEmailConfirmation
        };

        return View(model);
    }
}

/// <summary>
/// Registration form view model.
/// </summary>
public class RegistrationFormViewModel
{
    public bool ShowLoginLink { get; set; } = true;
    public bool RequireEmailConfirmation { get; set; } = true;

    // Form data
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public bool AcceptTerms { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = [];
}

/// <summary>
/// Registration form input model for POST binding.
/// </summary>
public class RegistrationFormInputModel
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
    public bool AcceptTerms { get; set; }
}
