using Microsoft.AspNetCore.Mvc;

namespace Baseline.Core;

/// <summary>
/// Controller for SEO-related endpoints (robots.txt, llms.txt, security.txt).
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class SeoEndpointsController : ControllerBase
{
    /// <summary>
    /// Returns the robots.txt content.
    /// </summary>
    [HttpGet("/robots.txt")]
    [Produces("text/plain")]
    public async Task<IActionResult> RobotsTxt([FromServices] IRobotsTxtService? service)
    {
        if (service is null)
        {
            return NotFound();
        }

        var content = await service.GenerateAsync();
        return Content(content, "text/plain");
    }

    /// <summary>
    /// Returns the llms.txt content for AI crawlers.
    /// </summary>
    [HttpGet("/llms.txt")]
    [Produces("text/plain; charset=utf-8")]
    public async Task<IActionResult> LlmsTxt([FromServices] ILlmsTxtService? service)
    {
        if (service is null)
        {
            return NotFound();
        }

        var content = await service.GenerateAsync();
        return Content(content, "text/plain; charset=utf-8");
    }

    /// <summary>
    /// Returns the security.txt content.
    /// </summary>
    [HttpGet("/.well-known/security.txt")]
    [HttpGet("/security.txt")]
    [Produces("text/plain")]
    public async Task<IActionResult> SecurityTxt([FromServices] ISecurityTxtService? service)
    {
        if (service is null)
        {
            return NotFound();
        }

        var content = await service.GenerateAsync();

        if (string.IsNullOrEmpty(content))
        {
            return NotFound();
        }

        return Content(content, "text/plain");
    }
}
