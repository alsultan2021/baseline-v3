using Kentico.Xperience.Admin.Base.FormAnnotations;
using XperienceCommunity.ChannelSettings.Attributes;

namespace Baseline.TabbedPages.Admin.Models;

/// <summary>
/// Channel-specific settings for the TabbedPages module.
/// Managed via admin UI under Channel Settings > Tabbed Pages.
/// </summary>
public class TabbedPagesChannelSettings
{
    #region Content Type Settings

    /// <summary>
    /// Tab content type code name.
    /// </summary>
    [XperienceSettingsData("TabbedPages.TabContentTypeName", "Generic.Tab")]
    [TextInputComponent(
        Label = "Tab Content Type",
        ExplanationText = "Code name of the content type used for tab pages (e.g., Generic.Tab).",
        Order = 1)]
    public virtual string TabContentTypeName { get; set; } = "Generic.Tab";

    /// <summary>
    /// TabParent content type code name.
    /// </summary>
    [XperienceSettingsData("TabbedPages.TabParentContentTypeName", "Generic.TabParent")]
    [TextInputComponent(
        Label = "Tab Parent Content Type",
        ExplanationText = "Code name of the content type used for tab parent pages (e.g., Generic.TabParent).",
        Order = 2)]
    public virtual string TabParentContentTypeName { get; set; } = "Generic.TabParent";

    #endregion

    #region Rendering

    /// <summary>
    /// Tab display style.
    /// </summary>
    [XperienceSettingsData("TabbedPages.TabStyle", "horizontal")]
    [DropDownComponent(
        Label = "Tab Style",
        ExplanationText = "Visual style for tab navigation.",
        Options = "horizontal;Horizontal\npills;Pills\nunderline;Underline\nvertical;Vertical",
        Order = 10)]
    public virtual string TabStyle { get; set; } = "horizontal";

    /// <summary>
    /// Enable tab switch animation.
    /// </summary>
    [XperienceSettingsData("TabbedPages.EnableAnimation", true)]
    [CheckBoxComponent(
        Label = "Enable Animation",
        ExplanationText = "Animate content transitions when switching tabs.",
        Order = 11)]
    public virtual bool EnableAnimation { get; set; } = true;

    /// <summary>
    /// Animation type.
    /// </summary>
    [XperienceSettingsData("TabbedPages.AnimationType", "fade")]
    [DropDownComponent(
        Label = "Animation Type",
        ExplanationText = "Type of animation used during tab transitions.",
        Options = "fade;Fade\nslide;Slide\nnone;None",
        Order = 12)]
    [VisibleIfTrue(nameof(EnableAnimation))]
    public virtual string AnimationType { get; set; } = "fade";

    /// <summary>
    /// Show tab icons if available.
    /// </summary>
    [XperienceSettingsData("TabbedPages.ShowIcons", true)]
    [CheckBoxComponent(
        Label = "Show Tab Icons",
        ExplanationText = "Display icons alongside tab titles when available.",
        Order = 13)]
    public virtual bool ShowIcons { get; set; } = true;

    #endregion

    #region Behavior

    /// <summary>
    /// Persist selected tab in URL hash.
    /// </summary>
    [XperienceSettingsData("TabbedPages.PersistInUrl", true)]
    [CheckBoxComponent(
        Label = "Persist Tab in URL",
        ExplanationText = "Update the URL hash when tabs are selected, enabling deep linking and browser history.",
        Order = 20)]
    public virtual bool PersistInUrl { get; set; } = true;

    /// <summary>
    /// Enable keyboard navigation between tabs.
    /// </summary>
    [XperienceSettingsData("TabbedPages.KeyboardNavigation", true)]
    [CheckBoxComponent(
        Label = "Keyboard Navigation",
        ExplanationText = "Allow users to navigate tabs using arrow keys.",
        Order = 21)]
    public virtual bool KeyboardNavigation { get; set; } = true;

    /// <summary>
    /// Lazy load tab content via AJAX.
    /// </summary>
    [XperienceSettingsData("TabbedPages.LazyLoadContent", false)]
    [CheckBoxComponent(
        Label = "Lazy Load Content",
        ExplanationText = "Load tab content on demand via AJAX instead of rendering all tabs server-side.",
        Order = 22)]
    public virtual bool LazyLoadContent { get; set; }

    #endregion

    #region SEO

    /// <summary>
    /// Render all tab content in HTML for SEO crawlers.
    /// </summary>
    [XperienceSettingsData("TabbedPages.RenderAllContentForSeo", true)]
    [CheckBoxComponent(
        Label = "Render All Content for SEO",
        ExplanationText = "Include all tab content in the initial HTML response so search engines can index it.",
        Order = 30)]
    public virtual bool RenderAllContentForSeo { get; set; } = true;

    /// <summary>
    /// Generate unique URLs per tab.
    /// </summary>
    [XperienceSettingsData("TabbedPages.GenerateTabUrls", true)]
    [CheckBoxComponent(
        Label = "Generate Tab URLs",
        ExplanationText = "Create individual URL slugs for each tab (e.g., /page#tab-slug).",
        Order = 31)]
    public virtual bool GenerateTabUrls { get; set; } = true;

    /// <summary>
    /// Include tabs in sitemap.
    /// </summary>
    [XperienceSettingsData("TabbedPages.IncludeInSitemap", false)]
    [CheckBoxComponent(
        Label = "Include in Sitemap",
        ExplanationText = "Add individual tab URLs to the XML sitemap.",
        Order = 32)]
    public virtual bool IncludeInSitemap { get; set; }

    /// <summary>
    /// Add structured data markup for tabs.
    /// </summary>
    [XperienceSettingsData("TabbedPages.AddStructuredData", true)]
    [CheckBoxComponent(
        Label = "Structured Data",
        ExplanationText = "Add schema.org structured data for tabbed content (ItemList markup).",
        Order = 33)]
    public virtual bool AddStructuredData { get; set; } = true;

    #endregion

    #region Caching

    /// <summary>
    /// Cache duration in minutes for tab queries.
    /// </summary>
    [XperienceSettingsData("TabbedPages.CacheDurationMinutes", 60)]
    [NumberInputComponent(
        Label = "Cache Duration (minutes)",
        ExplanationText = "How long to cache tab query results. Default: 60 minutes.",
        Order = 40)]
    public virtual int CacheDurationMinutes { get; set; } = 60;

    #endregion
}
