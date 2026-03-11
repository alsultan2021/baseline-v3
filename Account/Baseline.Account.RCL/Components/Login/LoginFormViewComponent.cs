using Microsoft.AspNetCore.Mvc;

namespace Baseline.Account.Components;

/// <summary>
/// Login form view component.
/// </summary>
public class LoginFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(
        string? returnUrl = null,
        bool showRememberMe = true,
        bool showForgotPassword = true,
        bool showRegisterLink = true)
    {
        var model = new LoginFormViewModel
        {
            ReturnUrl = returnUrl,
            ShowRememberMe = showRememberMe,
            ShowForgotPassword = showForgotPassword,
            ShowRegisterLink = showRegisterLink
        };

        return View(model);
    }
}

/// <summary>
/// Login form view model.
/// </summary>
public class LoginFormViewModel
{
    public string? ReturnUrl { get; set; }
    public bool ShowRememberMe { get; set; } = true;
    public bool ShowForgotPassword { get; set; } = true;
    public bool ShowRegisterLink { get; set; } = true;

    // Form data
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool RememberMe { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Login form input model for POST binding.
/// </summary>
public class LoginFormInputModel
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}
