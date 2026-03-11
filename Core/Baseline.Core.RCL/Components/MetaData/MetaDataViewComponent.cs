using Microsoft.AspNetCore.Mvc;

namespace Baseline.Core.Components;

/// <summary>
/// Renders SEO meta tags for the current page.
/// </summary>
public class MetaDataViewComponent(IMetaDataService metaDataService) : ViewComponent
{
    /// <summary>
    /// Renders meta tags for the current page.
    /// </summary>
    /// <param name="includeOpenGraph">Include OpenGraph tags.</param>
    /// <param name="includeTwitterCard">Include Twitter Card tags.</param>
    /// <param name="includeAlternates">Include alternate language links.</param>
    public async Task<IViewComponentResult> InvokeAsync(
        bool includeOpenGraph = true,
        bool includeTwitterCard = true,
        bool includeAlternates = true)
    {
        var metadata = await metaDataService.GetPageMetaDataAsync();

        var model = new MetaDataViewModel
        {
            Metadata = metadata,
            IncludeOpenGraph = includeOpenGraph,
            IncludeTwitterCard = includeTwitterCard,
            IncludeAlternates = includeAlternates
        };

        return View(model);
    }
}

/// <summary>
/// View model for meta data rendering.
/// </summary>
public class MetaDataViewModel
{
    public BaselinePageMetaData? Metadata { get; set; }
    public bool IncludeOpenGraph { get; set; } = true;
    public bool IncludeTwitterCard { get; set; } = true;
    public bool IncludeAlternates { get; set; } = true;
}
