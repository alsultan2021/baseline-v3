using Baseline.Core;
using Baseline.Navigation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;

namespace Baseline.Navigation.RCL.TagHelpers;

/// <summary>
/// Tag helper that adds navigation data attributes to elements for JS-based active state handling.
/// </summary>
/// <example>
/// <code>
/// &lt;li bl-nav-item bl-nav-path="/about"&gt;
///     &lt;a href="/about"&gt;About&lt;/a&gt;
/// &lt;/li&gt;
/// </code>
/// </example>
[HtmlTargetElement("li", Attributes = NavItemAttributeName)]
[HtmlTargetElement("a", Attributes = NavItemAttributeName)]
[HtmlTargetElement("div", Attributes = NavItemAttributeName)]
[HtmlTargetElement("nav", Attributes = NavItemAttributeName)]
public sealed class NavigationItemTagHelper : TagHelper
{
    private const string NavItemAttributeName = "bl-nav-item";
    private const string NavPathAttributeName = "bl-nav-path";
    private const string NavHrefAttributeName = "bl-nav-href";
    private const string NavExactAttributeName = "bl-nav-exact";

    /// <summary>
    /// Enable navigation item data attributes.
    /// </summary>
    [HtmlAttributeName(NavItemAttributeName)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The navigation path for matching.
    /// </summary>
    [HtmlAttributeName(NavPathAttributeName)]
    public string? Path { get; set; }

    /// <summary>
    /// The href for matching (alternative to path).
    /// </summary>
    [HtmlAttributeName(NavHrefAttributeName)]
    public string? Href { get; set; }

    /// <summary>
    /// Whether to require exact path match.
    /// </summary>
    [HtmlAttributeName(NavExactAttributeName)]
    public bool ExactMatch { get; set; }

    /// <inheritdoc/>
    public override int Order => -10;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Path))
        {
            output.Attributes.Add("data-nav-path", Path.ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(Href))
        {
            output.Attributes.Add("data-nav-href", Href.ToLowerInvariant());
        }

        if (ExactMatch)
        {
            output.Attributes.Add("data-nav-exact", "true");
        }
    }
}

/// <summary>
/// Tag helper that adds CSS classes from a navigation item model.
/// </summary>
/// <example>
/// <code>
/// &lt;li bl-nav-class class="nav-item" bl-nav-model="@item"&gt;...&lt;/li&gt;
/// </code>
/// </example>
[HtmlTargetElement("li", Attributes = NavClassAttributeName)]
[HtmlTargetElement("a", Attributes = NavClassAttributeName)]
[HtmlTargetElement("div", Attributes = NavClassAttributeName)]
[HtmlTargetElement("article", Attributes = NavClassAttributeName)]
public sealed class NavigationClassTagHelper : TagHelper
{
    private const string NavClassAttributeName = "bl-nav-class";
    private const string NavModelAttributeName = "bl-nav-model";

    /// <summary>
    /// Enable navigation class handling.
    /// </summary>
    [HtmlAttributeName(NavClassAttributeName)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The navigation item model containing CSS class info.
    /// </summary>
    [HtmlAttributeName(NavModelAttributeName)]
    public INavigationItemModel? Model { get; set; }

    /// <summary>
    /// Additional CSS classes to add.
    /// </summary>
    [HtmlAttributeName("bl-nav-extra-class")]
    public string? ExtraClasses { get; set; }

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled)
        {
            return;
        }

        // Add classes from model
        if (!string.IsNullOrWhiteSpace(Model?.CssClass))
        {
            foreach (var cssClass in Model.CssClass.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                output.AddClass(cssClass, HtmlEncoder.Default);
            }
        }

        // Add extra classes
        if (!string.IsNullOrWhiteSpace(ExtraClasses))
        {
            foreach (var cssClass in ExtraClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                output.AddClass(cssClass, HtmlEncoder.Default);
            }
        }
    }
}

/// <summary>
/// Tag helper that populates anchor tag attributes from a navigation item model.
/// </summary>
/// <example>
/// <code>
/// &lt;a bl-nav-link bl-nav-model="@item"&gt;@item.Title&lt;/a&gt;
/// </code>
/// </example>
[HtmlTargetElement("a", Attributes = NavLinkAttributeName)]
public sealed class NavigationLinkTagHelper : TagHelper
{
    private const string NavLinkAttributeName = "bl-nav-link";
    private const string NavModelAttributeName = "bl-nav-model";

    /// <summary>
    /// Enable navigation link handling.
    /// </summary>
    [HtmlAttributeName(NavLinkAttributeName)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The navigation item model.
    /// </summary>
    [HtmlAttributeName(NavModelAttributeName)]
    public INavigationItemModel? Model { get; set; }

    /// <summary>
    /// Whether to open external links in new tab.
    /// </summary>
    [HtmlAttributeName("bl-nav-external-newtab")]
    public bool ExternalNewTab { get; set; } = true;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled || Model is null)
        {
            return;
        }

        // Set href
        if (!string.IsNullOrWhiteSpace(Model.Href) && !output.Attributes.ContainsName("href"))
        {
            output.Attributes.SetAttribute("href", Model.Href);
        }

        // Set title
        if (!string.IsNullOrWhiteSpace(Model.Title) && !output.Attributes.ContainsName("title"))
        {
            output.Attributes.SetAttribute("title", Model.Title);
        }

        // Handle target
        if (!string.IsNullOrWhiteSpace(Model.Target) && !output.Attributes.ContainsName("target"))
        {
            output.Attributes.SetAttribute("target", Model.Target);
        }
        else if (ExternalNewTab && Model.IsExternal && !output.Attributes.ContainsName("target"))
        {
            output.Attributes.SetAttribute("target", "_blank");
            output.Attributes.SetAttribute("rel", "noopener noreferrer");
        }

        // Handle aria-current for active items
        if (Model.IsActive)
        {
            output.Attributes.SetAttribute("aria-current", "page");
        }
    }
}

/// <summary>
/// Tag helper for rendering breadcrumb navigation with proper accessibility.
/// </summary>
/// <example>
/// <code>
/// &lt;nav bl-breadcrumbs aria-label="Breadcrumb"&gt;
///     &lt;ol&gt;...&lt;/ol&gt;
/// &lt;/nav&gt;
/// </code>
/// </example>
[HtmlTargetElement("nav", Attributes = BreadcrumbsAttributeName)]
public sealed class BreadcrumbsTagHelper : TagHelper
{
    private const string BreadcrumbsAttributeName = "bl-breadcrumbs";

    /// <summary>
    /// Enable breadcrumb enhancements.
    /// </summary>
    [HtmlAttributeName(BreadcrumbsAttributeName)]
    public bool Enabled { get; set; } = true;

    /// <inheritdoc/>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled)
        {
            return;
        }

        // Ensure proper accessibility attributes
        if (!output.Attributes.ContainsName("aria-label"))
        {
            output.Attributes.SetAttribute("aria-label", "Breadcrumb");
        }

        // Add structured data attribute for JS-based JSON-LD generation
        output.Attributes.SetAttribute("data-structured-data", "breadcrumb");
    }
}

/// <summary>
/// Tag helper that outputs a JavaScript snippet to mark the current page as active in navigation.
/// This allows navigation to be fully cached while still indicating the current page.
/// </summary>
/// <example>
/// <code>
/// &lt;cache expires-after="CacheMinuteTypes.VeryLong.ToTimeSpan()"&gt;
///     &lt;vc:main-navigation x-css-class="navbar-nav" /&gt;
/// &lt;/cache&gt;
/// &lt;!-- Selector outside of cache --&gt;
/// &lt;bl:navigation-page-selector x-parent-class="navbar-nav"&gt;&lt;/bl:navigation-page-selector&gt;
/// </code>
/// </example>
/// <remarks>
/// The tag helper injects JavaScript that:
/// 1. Finds an &lt;li&gt; element with an &lt;a&gt; matching the current page path under the parent class
/// 2. Adds the 'active' CSS class to that &lt;li&gt;
/// 3. Sets aria-current="page" on the &lt;a&gt; for accessibility
/// </remarks>
[HtmlTargetElement("bl:navigation-page-selector")]
public sealed class NavigationPageSelectorTagHelper : TagHelper
{
    private readonly IPageContextRepository _pageContextRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private const string ParentClassAttributeName = "x-parent-class";
    private const string CurrentPagePathAttributeName = "x-current-page-path";
    private const string ActiveClassAttributeName = "x-active-class";
    private const string ExactMatchAttributeName = "x-exact-match";

    /// <summary>
    /// Creates a new <see cref="NavigationPageSelectorTagHelper"/>.
    /// </summary>
    public NavigationPageSelectorTagHelper(
        IPageContextRepository pageContextRepository,
        IHttpContextAccessor httpContextAccessor)
    {
        _pageContextRepository = pageContextRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// The CSS class of the parent element containing navigation items.
    /// </summary>
    [HtmlAttributeName(ParentClassAttributeName)]
    public string ParentClass { get; set; } = string.Empty;

    /// <summary>
    /// Optional explicit path to mark as current. If not provided, uses the current request path.
    /// </summary>
    [HtmlAttributeName(CurrentPagePathAttributeName)]
    public string? CurrentPagePath { get; set; }

    /// <summary>
    /// The CSS class to add to active items. Defaults to "active".
    /// </summary>
    [HtmlAttributeName(ActiveClassAttributeName)]
    public string ActiveClass { get; set; } = "active";

    /// <summary>
    /// Whether to require an exact path match. Defaults to false (prefix matching).
    /// </summary>
    [HtmlAttributeName(ExactMatchAttributeName)]
    public bool ExactMatch { get; set; }

    /// <inheritdoc/>
    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        // Suppress the custom element - we'll render a script instead
        output.TagName = null;

        if (string.IsNullOrWhiteSpace(ParentClass))
        {
            return;
        }

        // Determine the current page path
        var currentPath = CurrentPagePath;
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            // Try to get from page context first
            var pageIdentityResult = await _pageContextRepository.GetCurrentPageAsync();
            if (pageIdentityResult.HasValue)
            {
                currentPath = pageIdentityResult.Value.RelativeUrl;
            }
            else
            {
                // Fallback to request path
                currentPath = _httpContextAccessor.HttpContext?.Request.Path.Value ?? "/";
            }
        }

        // Normalize the path for comparison
        currentPath = NormalizePath(currentPath);

        // Generate the JavaScript snippet
        var script = GenerateScript(ParentClass, currentPath, ActiveClass, ExactMatch);

        output.Content.SetHtmlContent(script);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        // Ensure leading slash
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        // Remove trailing slash (except for root)
        if (path.Length > 1 && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        return path.ToLowerInvariant();
    }

    private static string GenerateScript(string parentClass, string currentPath, string activeClass, bool exactMatch)
    {
        var escapedParentClass = JavaScriptEncoder.Default.Encode(parentClass);
        var escapedCurrentPath = JavaScriptEncoder.Default.Encode(currentPath);
        var escapedActiveClass = JavaScriptEncoder.Default.Encode(activeClass);
        var matchType = exactMatch ? "exact" : "prefix";

        return $@"<script>
(function(){{
    var serverPath = '{escapedCurrentPath}';
    var parentClass = '{escapedParentClass}';
    var activeClass = '{escapedActiveClass}';
    var matchType = '{matchType}';

    // Use browser path if server path looks like CMS preview (~) or is empty
    var currentPath = (serverPath && serverPath.indexOf('~') === -1) ? serverPath : window.location.pathname;

    function normalizePath(p){{
        if(!p) return '/';
        p = p.toLowerCase();
        if(!p.startsWith('/')) p = '/' + p;
        if(p.length > 1 && p.endsWith('/')) p = p.slice(0, -1);
        return p;
    }}

    currentPath = normalizePath(currentPath);

    function matches(href, path){{
        href = normalizePath(href);
        if(matchType === 'exact'){{
            return href === path;
        }}
        // Prefix match: path starts with href, and either is the same or followed by /
        return href === path || path.startsWith(href + '/');
    }}

    var parent = document.querySelector('.' + parentClass);
    if(!parent) return;

    var items = parent.querySelectorAll('li');
    items.forEach(function(li){{
        var links = li.querySelectorAll('a[href]');
        links.forEach(function(a){{
            var href = a.getAttribute('href');
            if(matches(href, currentPath)){{
                li.classList.add(activeClass);
                a.setAttribute('aria-current', 'page');
            }}
        }});
    }});
}})();
</script>";
    }
}
