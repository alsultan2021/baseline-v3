namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Provides functionality to validate product SKU codes against existing content items.
/// Ensures SKU codes are unique across published and draft versions.
/// </summary>
public interface IProductSkuValidator
{
    /// <summary>
    /// Checks if the provided SKU code is already used by another content item.
    /// </summary>
    /// <param name="skuCode">The SKU code to check.</param>
    /// <param name="contentItemId">The ID of the content item to exclude from the check (optional).</param>
    /// <returns>The identifier of the colliding content item, or null if no collision found.</returns>
    Task<int?> GetCollidingContentItem(string skuCode, int? contentItemId = null);
}
