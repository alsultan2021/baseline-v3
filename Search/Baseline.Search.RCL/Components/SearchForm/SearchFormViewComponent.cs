using Microsoft.AspNetCore.Mvc;

namespace Baseline.Search.Components;

/// <summary>
/// Renders a search form.
/// </summary>
public class SearchFormViewComponent : ViewComponent
{
    /// <summary>
    /// Renders the search form.
    /// </summary>
    /// <param name="action">Form action URL.</param>
    /// <param name="placeholder">Input placeholder text.</param>
    /// <param name="buttonText">Submit button text.</param>
    /// <param name="showButton">Whether to show the submit button.</param>
    /// <param name="cssClass">CSS class for the form.</param>
    public IViewComponentResult Invoke(
        string action = "/search",
        string placeholder = "Search...",
        string buttonText = "Search",
        bool showButton = true,
        string? cssClass = null)
    {
        var model = new SearchFormViewModel
        {
            Action = action,
            Placeholder = placeholder,
            ButtonText = buttonText,
            ShowButton = showButton,
            CssClass = cssClass,
            CurrentQuery = HttpContext?.Request?.Query["q"].ToString()
        };

        return View(model);
    }
}

/// <summary>
/// View model for search form.
/// </summary>
public class SearchFormViewModel
{
    public string Action { get; set; } = "/search";
    public string Placeholder { get; set; } = "Search...";
    public string ButtonText { get; set; } = "Search";
    public bool ShowButton { get; set; } = true;
    public string? CssClass { get; set; }
    public string? CurrentQuery { get; set; }
}
