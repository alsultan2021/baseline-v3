using Microsoft.AspNetCore.Mvc;

namespace Baseline.Navigation;

/// <summary>
/// Controller for sitemap endpoints.
/// Explicit [Route] ensures endpoints register even without convention routing.
/// </summary>
[Route("")]
[ApiExplorerSettings(IgnoreApi = true)]
public class SitemapController : ControllerBase
{
    /// <summary>
    /// Returns the main sitemap.xml.
    /// </summary>
    [HttpGet("/sitemap.xml", Order = -1000)]
    [Produces("application/xml")]
    public async Task<IActionResult> Sitemap([FromServices] ISitemapService? service)
    {
        if (service is null)
        {
            return NotFound();
        }

        var content = await service.GenerateSitemapAsync();
        return Content(content, "application/xml");
    }

    /// <summary>
    /// Returns the sitemap index for large sites.
    /// </summary>
    [HttpGet("/sitemap-index.xml", Order = -1000)]
    [Produces("application/xml")]
    public async Task<IActionResult> SitemapIndex([FromServices] ISitemapService? service)
    {
        if (service is null)
        {
            return NotFound();
        }

        var content = await service.GenerateSitemapIndexAsync();
        return Content(content, "application/xml");
    }

    /// <summary>
    /// Returns a specific sitemap section.
    /// </summary>
    [HttpGet("/sitemap-{section}.xml", Order = -1000)]
    [Produces("application/xml")]
    public async Task<IActionResult> SitemapSection(
        [FromServices] ISitemapService? service,
        string section,
        [FromQuery] int page = 1)
    {
        if (service is null)
        {
            return NotFound();
        }

        var content = await service.GenerateSitemapSectionAsync(section, page);
        return Content(content, "application/xml");
    }
}
