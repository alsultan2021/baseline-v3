using Microsoft.AspNetCore.Mvc;

namespace Baseline.Account.Components;

/// <summary>
/// User profile view component.
/// </summary>
public class ProfileViewComponent(IProfileService profileService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        bool showEditForm = true,
        bool showProfilePicture = true,
        bool showDeleteOption = false)
    {
        var profile = await profileService.GetProfileAsync();

        var model = new ProfileViewModel
        {
            Profile = profile,
            ShowEditForm = showEditForm,
            ShowProfilePicture = showProfilePicture,
            ShowDeleteOption = showDeleteOption
        };

        return View(model);
    }
}

/// <summary>
/// Profile view model.
/// </summary>
public class ProfileViewModel
{
    public UserProfile? Profile { get; set; }
    public bool ShowEditForm { get; set; } = true;
    public bool ShowProfilePicture { get; set; } = true;
    public bool ShowDeleteOption { get; set; }

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Profile update input model for POST binding.
/// </summary>
public class ProfileUpdateInputModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Bio { get; set; }
}
