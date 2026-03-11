using Microsoft.AspNetCore.Mvc;

namespace Baseline.Core.RCL.Features.HttpErrors;

/// <summary>
/// Controller for handling HTTP error pages.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HttpErrorsController : Controller
{
    /// <summary>
    /// Handles errors by code.
    /// </summary>
    /// <param name="code">HTTP status code.</param>
    [Route("error/{code:int}")]
    public IActionResult Error(int code) => code switch
    {
        400 => BadRequest400(),
        401 => Unauthorized401(),
        403 => AccessDenied(),
        404 => Error404(),
        500 => Error500(),
        503 => ServiceUnavailable503(),
        _ => GenericError(code)
    };

    /// <summary>
    /// 400 Bad Request error page.
    /// </summary>
    [Route("error/400")]
    public IActionResult BadRequest400()
    {
        Response.StatusCode = 400;
        return View("~/Features/HttpErrors/Error400.cshtml");
    }

    /// <summary>
    /// 401 Unauthorized error page.
    /// </summary>
    [Route("error/401")]
    public IActionResult Unauthorized401()
    {
        Response.StatusCode = 401;
        return View("~/Features/HttpErrors/Error401.cshtml");
    }

    /// <summary>
    /// 403 Access Denied error page.
    /// </summary>
    [Route("error/403")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View("~/Features/HttpErrors/AccessDenied.cshtml");
    }

    /// <summary>
    /// 404 Not Found error page.
    /// </summary>
    [Route("error/404")]
    public IActionResult Error404()
    {
        Response.StatusCode = 404;
        return View("~/Features/HttpErrors/Error404.cshtml");
    }

    /// <summary>
    /// 500 Internal Server Error page.
    /// </summary>
    [Route("error/500")]
    public IActionResult Error500()
    {
        Response.StatusCode = 500;
        return View("~/Features/HttpErrors/Error500.cshtml");
    }

    /// <summary>
    /// 503 Service Unavailable error page.
    /// </summary>
    [Route("error/503")]
    public IActionResult ServiceUnavailable503()
    {
        Response.StatusCode = 503;
        return View("~/Features/HttpErrors/Error503.cshtml");
    }

    /// <summary>
    /// Generic error page.
    /// </summary>
    private IActionResult GenericError(int code)
    {
        Response.StatusCode = code;
        ViewData["ErrorCode"] = code;
        return View("~/Features/HttpErrors/GenericError.cshtml");
    }

    /// <summary>
    /// Gets the URL for the access denied page.
    /// </summary>
    public static string GetAccessDeniedUrl() => "/error/403";

    /// <summary>
    /// Gets the URL for the not found page.
    /// </summary>
    public static string GetNotFoundUrl() => "/error/404";
}
