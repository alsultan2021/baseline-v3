namespace Baseline.TabbedPages;

/// <summary>
/// Configuration options for Baseline v3 TabbedPages module.
/// </summary>
public class BaselineTabbedPagesOptions
{
    /// <summary>
    /// Content type code name for Tab pages (default: "Generic.Tab").
    /// </summary>
    public string TabContentTypeName { get; set; } = "Generic.Tab";

    /// <summary>
    /// Content type code name for TabParent pages (default: "Generic.TabParent").
    /// </summary>
    public string TabParentContentTypeName { get; set; } = "Generic.TabParent";

    /// <summary>
    /// Cache duration in minutes for tab queries (default: 60).
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Field name mappings for Tab content type columns.
    /// Configure these when your Tab content type uses different field names
    /// than the defaults, or when fields don't exist on the content type.
    /// </summary>
    public TabFieldMappingOptions FieldMapping { get; set; } = new();

    /// <summary>
    /// Tab rendering options.
    /// </summary>
    public TabRenderingOptions Rendering { get; set; } = new();

    /// <summary>
    /// Tab behavior options.
    /// </summary>
    public TabBehaviorOptions Behavior { get; set; } = new();

    /// <summary>
    /// SEO options for tabbed pages.
    /// </summary>
    public TabSeoOptions Seo { get; set; } = new();

    /// <summary>
    /// Accessibility options.
    /// </summary>
    public TabAccessibilityOptions Accessibility { get; set; } = new();
}

/// <summary>
/// Tab rendering configuration.
/// </summary>
public class TabRenderingOptions
{
    /// <summary>
    /// Tab style (e.g., "horizontal", "vertical", "pills", "underline").
    /// </summary>
    public string TabStyle { get; set; } = "horizontal";

    /// <summary>
    /// CSS class for tab container.
    /// </summary>
    public string ContainerClass { get; set; } = "baseline-tabs";

    /// <summary>
    /// CSS class for tab list.
    /// </summary>
    public string TabListClass { get; set; } = "tab-list";

    /// <summary>
    /// CSS class for active tab.
    /// </summary>
    public string ActiveTabClass { get; set; } = "active";

    /// <summary>
    /// CSS class for tab content panels.
    /// </summary>
    public string TabPanelClass { get; set; } = "tab-panel";

    /// <summary>
    /// Enable animation on tab switch.
    /// </summary>
    public bool EnableAnimation { get; set; } = true;

    /// <summary>
    /// Animation type (e.g., "fade", "slide", "none").
    /// </summary>
    public string AnimationType { get; set; } = "fade";

    /// <summary>
    /// Show tab icons if available.
    /// </summary>
    public bool ShowIcons { get; set; } = true;

    /// <summary>
    /// Icon position relative to text.
    /// </summary>
    public IconPosition IconPosition { get; set; } = IconPosition.Left;

    /// <summary>
    /// Use vertical tab layout.
    /// </summary>
    public bool VerticalTabs { get; set; }
}

/// <summary>
/// Icon position.
/// </summary>
public enum IconPosition
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// Tab behavior configuration.
/// </summary>
public class TabBehaviorOptions
{
    /// <summary>
    /// Default tab index (0-based).
    /// </summary>
    public int DefaultTabIndex { get; set; } = 0;

    /// <summary>
    /// Remember selected tab in URL hash.
    /// </summary>
    public bool PersistInUrl { get; set; } = true;

    /// <summary>
    /// Remember selected tab in local storage.
    /// </summary>
    public bool PersistInStorage { get; set; }

    /// <summary>
    /// Enable keyboard navigation.
    /// </summary>
    public bool KeyboardNavigation { get; set; } = true;

    /// <summary>
    /// Auto-activate tab on focus.
    /// </summary>
    public bool ActivateOnFocus { get; set; } = false;

    /// <summary>
    /// Lazy load tab content.
    /// </summary>
    public bool LazyLoadContent { get; set; }

    /// <summary>
    /// Deep link to specific tab.
    /// </summary>
    public bool EnableDeepLinking { get; set; } = true;

    /// <summary>
    /// Scroll to tab container when tab is activated via URL.
    /// </summary>
    public bool ScrollToTabOnLoad { get; set; } = true;
}

/// <summary>
/// Tab SEO configuration.
/// </summary>
public class TabSeoOptions
{
    /// <summary>
    /// Render all tab content in HTML for SEO.
    /// </summary>
    public bool RenderAllContentForSeo { get; set; } = true;

    /// <summary>
    /// Generate unique URLs for each tab.
    /// </summary>
    public bool GenerateTabUrls { get; set; } = true;

    /// <summary>
    /// Tab URL pattern (e.g., "{pagePath}#tab-{tabId}", "{pagePath}/{tabSlug}").
    /// </summary>
    public string TabUrlPattern { get; set; } = "{pagePath}#tab-{tabSlug}";

    /// <summary>
    /// Include tabs in sitemap.
    /// </summary>
    public bool IncludeInSitemap { get; set; }

    /// <summary>
    /// Absolute base URL for sitemap entries (e.g., "https://example.com").
    /// Required when <see cref="IncludeInSitemap"/> is true.
    /// </summary>
    public string? SiteBaseUrl { get; set; }

    /// <summary>
    /// Add structured data for tabs.
    /// </summary>
    public bool AddStructuredData { get; set; } = true;

    /// <summary>
    /// Alias for AddStructuredData (for schema.org markup).
    /// </summary>
    public bool UseSchemaMarkup
    {
        get => AddStructuredData;
        set => AddStructuredData = value;
    }
}

/// <summary>
/// Tab accessibility configuration.
/// </summary>
public class TabAccessibilityOptions
{
    /// <summary>
    /// Use proper ARIA attributes.
    /// </summary>
    public bool UseAriaAttributes { get; set; } = true;

    /// <summary>
    /// Announce tab changes to screen readers.
    /// </summary>
    public bool AnnounceChanges { get; set; } = true;

    /// <summary>
    /// Custom label for tab list.
    /// </summary>
    public string? TabListLabel { get; set; }

    /// <summary>
    /// Focus management on tab switch.
    /// </summary>
    public FocusManagement FocusManagement { get; set; } = FocusManagement.TabPanel;
}

/// <summary>
/// Focus management mode.
/// </summary>
public enum FocusManagement
{
    /// <summary>
    /// Move focus to tab button.
    /// </summary>
    TabButton,

    /// <summary>
    /// Move focus to tab panel.
    /// </summary>
    TabPanel,

    /// <summary>
    /// Don't change focus.
    /// </summary>
    None
}

/// <summary>
/// Field name mappings for Tab content type columns.
/// By default, maps to ContentItemName for title and uses WebPageItemOrder for order.
/// Set a field name to null to skip reading that field (uses defaults instead).
/// </summary>
/// <remarks>
/// The default Generic.Tab content type may not have custom fields like TabName,
/// TabDescription, TabIcon, etc. Configure these mappings when your content type
/// schema includes those fields.
/// </remarks>
public class TabFieldMappingOptions
{
    /// <summary>
    /// Field name for the tab title. Null = use ContentItemName.
    /// </summary>
    public string? TitleFieldName { get; set; }

    /// <summary>
    /// Field name for the "is default tab" flag. Null = skip (first tab is default).
    /// </summary>
    public string? IsDefaultFieldName { get; set; }

    /// <summary>
    /// Field name for the tab description. Null = skip.
    /// </summary>
    public string? DescriptionFieldName { get; set; }

    /// <summary>
    /// Field name for the tab icon CSS class. Null = skip.
    /// </summary>
    public string? IconFieldName { get; set; }

    /// <summary>
    /// Field name for the tab HTML content. Null = skip.
    /// </summary>
    public string? ContentFieldName { get; set; }

    /// <summary>
    /// Field name for the "uses Page Builder" flag. Null = skip (assume false).
    /// </summary>
    public string? UsesPageBuilderFieldName { get; set; }
}
