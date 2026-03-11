using CMS.Websites;

namespace Baseline.Core;

/// <summary>
/// Interface for content types that have redirect fields.
/// Used with the Base.Redirect reusable schema.
/// </summary>
public interface IBaseRedirect
{
    /// <summary>
    /// Gets or sets the redirection type (e.g., "None", "Internal", "External", "FirstChild").
    /// </summary>
    string PageRedirectionType { get; set; }

    /// <summary>
    /// Gets or sets the internal redirect page reference.
    /// </summary>
    IEnumerable<WebPageRelatedItem> PageInternalRedirectPage { get; set; }

    /// <summary>
    /// Gets or sets the external redirect URL.
    /// </summary>
    string PageExternalRedirectURL { get; set; }

    /// <summary>
    /// Gets or sets the first child page type class name for first-child redirects.
    /// </summary>
    string PageFirstChildClassName { get; set; }

    /// <summary>
    /// Gets or sets whether to use permanent (301) redirects.
    /// </summary>
    bool PageUsePermanentRedirects { get; set; }
}
