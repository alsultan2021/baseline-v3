using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.TabbedPages;

/// <summary>
/// API controller for lazy-loading tab content via AJAX.
/// Active only when <see cref="TabBehaviorOptions.LazyLoadContent"/> is enabled.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/baseline/tabs")]
public class TabContentApiController(
    ITabbedPageService tabbedPageService,
    IOptions<BaselineTabbedPagesOptions> options,
    ILogger<TabContentApiController> logger) : ControllerBase
{
    private readonly BaselineTabbedPagesOptions _options = options.Value;

    /// <summary>
    /// Gets tab content by tab ID. Returns HTML fragment for AJAX insertion.
    /// </summary>
    /// <param name="pageId">Parent page ID.</param>
    /// <param name="tabSlug">Tab URL slug.</param>
    [HttpGet("{pageId:int}/{tabSlug}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetTabContent(int pageId, string tabSlug)
    {
        if (!_options.Behavior.LazyLoadContent)
        {
            return NotFound(new { error = "Lazy loading is not enabled." });
        }

        if (pageId <= 0 || string.IsNullOrWhiteSpace(tabSlug))
        {
            return BadRequest(new { error = "Invalid page ID or tab slug." });
        }

        logger.LogDebug("Lazy loading tab content: page {PageId}, slug {TabSlug}", pageId, tabSlug);

        var tab = await tabbedPageService.GetTabBySlugAsync(pageId, tabSlug);
        if (tab is null)
        {
            return NotFound(new { error = "Tab not found." });
        }

        var content = await tabbedPageService.GetTabContentAsync(tab.Id);
        if (content is null)
        {
            return Ok(new TabContentResponse
            {
                TabId = tab.Id,
                Slug = tab.Slug,
                Title = tab.Title,
                Html = string.Empty
            });
        }

        return Ok(new TabContentResponse
        {
            TabId = tab.Id,
            Slug = tab.Slug,
            Title = tab.Title,
            Html = content.Html,
            UsesPageBuilder = content.UsesPageBuilder,
            LastModified = content.LastModified
        });
    }

    /// <summary>
    /// Gets all tab metadata (without content) for a page.
    /// Useful for client-side tab list initialization.
    /// </summary>
    /// <param name="pageId">Parent page ID.</param>
    [HttpGet("{pageId:int}")]
    [Produces("application/json")]
    public async Task<IActionResult> GetTabs(int pageId)
    {
        if (!_options.Behavior.LazyLoadContent)
        {
            return NotFound(new { error = "Lazy loading is not enabled." });
        }

        if (pageId <= 0)
        {
            return BadRequest(new { error = "Invalid page ID." });
        }

        var tabs = await tabbedPageService.GetTabsAsync(pageId);

        var result = tabs.Select(t => new TabMetadataResponse
        {
            TabId = t.Id,
            Slug = t.Slug,
            Title = t.Title,
            Icon = t.Icon,
            IsDefault = t.IsDefault,
            Order = t.Order
        });

        return Ok(result);
    }
}

/// <summary>
/// Response DTO for lazy-loaded tab content.
/// </summary>
public class TabContentResponse
{
    /// <summary>Tab ID.</summary>
    public int TabId { get; set; }

    /// <summary>Tab slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Tab title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>HTML content fragment.</summary>
    public string Html { get; set; } = string.Empty;

    /// <summary>Whether this tab uses Page Builder rendering.</summary>
    public bool UsesPageBuilder { get; set; }

    /// <summary>Last modified timestamp.</summary>
    public DateTimeOffset LastModified { get; set; }
}

/// <summary>
/// Response DTO for tab metadata (no content).
/// </summary>
public class TabMetadataResponse
{
    /// <summary>Tab ID.</summary>
    public int TabId { get; set; }

    /// <summary>Tab slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Tab title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Tab icon CSS class.</summary>
    public string? Icon { get; set; }

    /// <summary>Whether this is the default tab.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Tab order.</summary>
    public int Order { get; set; }
}
