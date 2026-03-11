using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.Core.Models;

/// <summary>
/// Data provider for CDN provider dropdown.
/// </summary>
public class CdnProviderDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "", Text = "(Not specified)" },
            new() { Value = "cloudflare", Text = "Cloudflare" },
            new() { Value = "fastly", Text = "Fastly" },
            new() { Value = "akamai", Text = "Akamai" },
            new() { Value = "cloudfront", Text = "AWS CloudFront" },
            new() { Value = "azure-frontdoor", Text = "Azure Front Door" },
            new() { Value = "bunny", Text = "Bunny CDN" },
            new() { Value = "keycdn", Text = "KeyCDN" },
            new() { Value = "custom", Text = "Custom/Other" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for cache tag header dropdown.
/// </summary>
public class CacheTagHeaderDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "Surrogate-Key", Text = "Surrogate-Key (Fastly)" },
            new() { Value = "Cache-Tag", Text = "Cache-Tag (Cloudflare)" },
            new() { Value = "Edge-Cache-Tag", Text = "Edge-Cache-Tag (Akamai)" },
            new() { Value = "X-Cache-Tags", Text = "X-Cache-Tags (Custom)" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for X-Frame-Options dropdown.
/// </summary>
public class XFrameOptionsDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "DENY", Text = "DENY - Prevent all framing" },
            new() { Value = "SAMEORIGIN", Text = "SAMEORIGIN - Allow same origin only" },
            new() { Value = "", Text = "(Disabled)" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for Referrer-Policy dropdown.
/// </summary>
public class ReferrerPolicyDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "strict-origin-when-cross-origin", Text = "strict-origin-when-cross-origin (Recommended)" },
            new() { Value = "no-referrer", Text = "no-referrer" },
            new() { Value = "no-referrer-when-downgrade", Text = "no-referrer-when-downgrade" },
            new() { Value = "origin", Text = "origin" },
            new() { Value = "origin-when-cross-origin", Text = "origin-when-cross-origin" },
            new() { Value = "same-origin", Text = "same-origin" },
            new() { Value = "strict-origin", Text = "strict-origin" },
            new() { Value = "unsafe-url", Text = "unsafe-url" },
            new() { Value = "", Text = "(Disabled)" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for image format dropdown.
/// </summary>
public class ImageFormatDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "webp", Text = "WebP (Best compression)" },
            new() { Value = "avif", Text = "AVIF (Newest, best quality)" },
            new() { Value = "jpg", Text = "JPEG (Universal support)" },
            new() { Value = "png", Text = "PNG (Lossless)" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for sitemap change frequency dropdown.
/// </summary>
public class SitemapChangeFrequencyDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "always", Text = "Always" },
            new() { Value = "hourly", Text = "Hourly" },
            new() { Value = "daily", Text = "Daily" },
            new() { Value = "weekly", Text = "Weekly" },
            new() { Value = "monthly", Text = "Monthly" },
            new() { Value = "yearly", Text = "Yearly" },
            new() { Value = "never", Text = "Never" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}

/// <summary>
/// Data provider for sitemap priority dropdown.
/// </summary>
public class SitemapPriorityDataProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems()
    {
        var items = new List<DropDownOptionItem>
        {
            new() { Value = "1.0", Text = "1.0 (Highest)" },
            new() { Value = "0.9", Text = "0.9" },
            new() { Value = "0.8", Text = "0.8" },
            new() { Value = "0.7", Text = "0.7" },
            new() { Value = "0.6", Text = "0.6" },
            new() { Value = "0.5", Text = "0.5 (Default)" },
            new() { Value = "0.4", Text = "0.4" },
            new() { Value = "0.3", Text = "0.3" },
            new() { Value = "0.2", Text = "0.2" },
            new() { Value = "0.1", Text = "0.1" },
            new() { Value = "0.0", Text = "0.0 (Lowest)" }
        };
        return Task.FromResult<IEnumerable<DropDownOptionItem>>(items);
    }
}
