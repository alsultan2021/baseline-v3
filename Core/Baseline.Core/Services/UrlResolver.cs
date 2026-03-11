using Microsoft.AspNetCore.Http;

namespace Baseline.Core;

/// <summary>
/// V3 implementation of URL resolution service.
/// </summary>
public class UrlResolver(IHttpContextAccessor httpContextAccessor) : IUrlResolver
{
    public string GetAbsoluteUrl(string relativeUrl)
    {
        if (string.IsNullOrWhiteSpace(relativeUrl))
        {
            relativeUrl = string.Empty;
        }

        if (relativeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || relativeUrl.StartsWith("//"))
        {
            return relativeUrl;
        }

        return BuildAbsoluteUri(ResolveUrl(relativeUrl));
    }

    public string ResolveUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        if (url.StartsWith("~/"))
        {
            url = url.Replace("~/", "/");
        }

        return url;
    }

    private string BuildAbsoluteUri(string relativeUrl)
    {
        relativeUrl = !string.IsNullOrWhiteSpace(relativeUrl) ? relativeUrl : "/";

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return relativeUrl;
        }

        var request = httpContext.Request;
        var hostValue = request.Host.Value ?? "localhost";
        var hostParts = hostValue.Split(':');
        var urlParts = relativeUrl.Split('?');
        var path = urlParts.Length > 0 ? urlParts[0] : "/";
        var query = urlParts.Length > 1 ? urlParts[1] : "";

        if (hostParts.Length > 1 && !string.IsNullOrEmpty(hostParts[1]) && int.TryParse(hostParts[1], out var port))
        {
            return new UriBuilder
            {
                Scheme = request.Scheme,
                Host = hostParts[0],
                Port = port,
                Path = path,
                Query = query
            }.Uri.AbsoluteUri;
        }

        return new UriBuilder
        {
            Scheme = request.Scheme,
            Host = hostParts[0],
            Path = path,
            Query = query
        }.Uri.AbsoluteUri;
    }
}
