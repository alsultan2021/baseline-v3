using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Websites.FormAnnotations;
using XperienceCommunity.ChannelSettings.Attributes;

namespace Baseline.Navigation.Models;

/// <summary>
/// Channel settings for Navigation module.
/// Configure navigation-related settings per channel.
/// </summary>
public class NavigationChannelSettings
{
    #region Page Paths

    /// <summary>
    /// URL to the Home page. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Navigation.HomePagePath", "/Home")]
    [UrlSelectorComponent(
        Label = "Home Page URL",
        ExplanationText = "Select the home page from the content tree or enter a URL manually",
        Order = 0)]
    public virtual string HomePagePath { get; set; } = "/Home";

    /// <summary>
    /// URL to the main site navigation menu. Select from content tree or enter manually.
    /// </summary>
    [XperienceSettingsData("Navigation.SiteNavigationMenuPath", "/Navigation_menu")]
    [UrlSelectorComponent(
        Label = "Site Navigation Menu URL",
        ExplanationText = "Select the navigation menu content folder from the content tree or enter a path manually",
        Order = 1)]
    public virtual string SiteNavigationMenuPath { get; set; } = "/Navigation_menu";

    #endregion

    #region Menu Settings

    /// <summary>
    /// Maximum depth for navigation menu hierarchy.
    /// </summary>
    [XperienceSettingsData("Navigation.MaxMenuDepth", 3)]
    [NumberInputComponent(
        Label = "Maximum Menu Depth",
        ExplanationText = "Maximum depth of the navigation menu hierarchy (1-5)",
        Order = 10)]
    public virtual int MaxMenuDepth { get; set; } = 3;

    /// <summary>
    /// Whether to show hidden pages in navigation.
    /// </summary>
    [XperienceSettingsData("Navigation.ShowHiddenPages", false)]
    [CheckBoxComponent(
        Label = "Show Hidden Pages",
        ExplanationText = "If checked, pages marked as hidden will still appear in navigation",
        Order = 11)]
    public virtual bool ShowHiddenPages { get; set; } = false;

    #endregion

    #region Caching Settings

    /// <summary>
    /// Whether to cache navigation items.
    /// </summary>
    [XperienceSettingsData("Navigation.EnableCaching", true)]
    [CheckBoxComponent(
        Label = "Enable Navigation Caching",
        ExplanationText = "Cache navigation items for better performance",
        Order = 20)]
    public virtual bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Navigation cache duration in minutes.
    /// </summary>
    [XperienceSettingsData("Navigation.CacheDurationMinutes", 60)]
    [NumberInputComponent(
        Label = "Cache Duration (Minutes)",
        ExplanationText = "How long to cache navigation items (in minutes)",
        Order = 21)]
    public virtual int CacheDurationMinutes { get; set; } = 60;

    #endregion

    #region SEO & Breadcrumbs

    /// <summary>
    /// Whether to include breadcrumb structured data.
    /// </summary>
    [XperienceSettingsData("Navigation.IncludeBreadcrumbStructuredData", true)]
    [CheckBoxComponent(
        Label = "Include Breadcrumb Structured Data",
        ExplanationText = "Add JSON-LD structured data for breadcrumbs (SEO)",
        Order = 30)]
    public virtual bool IncludeBreadcrumbStructuredData { get; set; } = true;

    /// <summary>
    /// Home page display name in breadcrumbs.
    /// </summary>
    [XperienceSettingsData("Navigation.HomePageBreadcrumbText", "Home")]
    [TextInputComponent(
        Label = "Home Page Breadcrumb Text",
        ExplanationText = "Text to display for the home page in breadcrumbs",
        WatermarkText = "Home",
        Order = 31)]
    public virtual string HomePageBreadcrumbText { get; set; } = "Home";

    #endregion

    #region Sitemap

    /// <summary>
    /// Whether to generate XML sitemap.
    /// </summary>
    [XperienceSettingsData("Navigation.EnableSitemap", true)]
    [CheckBoxComponent(
        Label = "Enable XML Sitemap",
        ExplanationText = "Generate XML sitemap for search engines",
        Order = 40)]
    public virtual bool EnableSitemap { get; set; } = true;

    /// <summary>
    /// Sitemap change frequency.
    /// </summary>
    [XperienceSettingsData("Navigation.SitemapChangeFrequency", "weekly")]
    [DropDownComponent(
        Label = "Sitemap Change Frequency",
        ExplanationText = "Default change frequency for sitemap entries",
        DataProviderType = typeof(SitemapChangeFrequencyDataProvider),
        Order = 41)]
    public virtual string SitemapChangeFrequency { get; set; } = "weekly";

    #endregion
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
