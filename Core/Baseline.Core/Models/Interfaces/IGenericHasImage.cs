using CMS.ContentEngine;

namespace Baseline.Core;

/// <summary>
/// Interface for content types that have an image asset.
/// Used with the Generic.HasImage reusable schema.
/// </summary>
public interface IGenericHasImage
{
    /// <summary>
    /// Gets or sets the image asset reference.
    /// </summary>
    ContentItemAsset HasImageImage { get; set; }
}
