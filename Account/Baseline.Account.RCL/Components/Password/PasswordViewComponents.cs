using Microsoft.AspNetCore.Mvc;

namespace Baseline.Account.Components;

/// <summary>
/// Forgot password form view component.
/// </summary>
public class ForgotPasswordFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(bool showLoginLink = true)
    {
        var model = new ForgotPasswordFormViewModel
        {
            ShowLoginLink = showLoginLink
        };

        return View(model);
    }
}

public class ForgotPasswordFormViewModel
{
    public bool ShowLoginLink { get; set; } = true;
    public string? Email { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Reset password form view component.
/// </summary>
public class ResetPasswordFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string token)
    {
        var model = new ResetPasswordFormViewModel
        {
            Token = token
        };

        return View(model);
    }
}

public class ResetPasswordFormViewModel
{
    public string? Token { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = [];
}

/// <summary>
/// Change password form view component.
/// </summary>
public class ChangePasswordFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View(new ChangePasswordFormViewModel());
    }
}

public class ChangePasswordFormViewModel
{
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string> ValidationErrors { get; set; } = [];
}
