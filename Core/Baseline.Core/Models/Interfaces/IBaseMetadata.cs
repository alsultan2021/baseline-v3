namespace Baseline.Core;

/// <summary>
/// Interface for content types that have page metadata fields.
/// Used with the Base.Metadata reusable schema.
/// </summary>
public interface IBaseMetadata
{
    /// <summary>
    /// Gets or sets the page name (displayed on menus, breadcrumbs, etc.).
    /// </summary>
    string MetaData_PageName { get; set; }

    /// <summary>
    /// Gets or sets the page title. If empty, defaults to PageName.
    /// </summary>
    string MetaData_Title { get; set; }

    /// <summary>
    /// Gets or sets the page description for SEO.
    /// </summary>
    string MetaData_Description { get; set; }

    /// <summary>
    /// Gets or sets the page keywords for SEO.
    /// </summary>
    string MetaData_Keywords { get; set; }

    /// <summary>
    /// Gets or sets whether the page should be excluded from search engine indexing.
    /// </summary>
    bool MetaData_NoIndex { get; set; }

    /// <summary>
    /// Gets or sets the Open Graph image for social sharing.
    /// </summary>
    IEnumerable<IGenericHasImage> MetaData_OGImage { get; set; }
}
