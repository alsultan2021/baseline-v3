using CMS.DataEngine;
using Baseline.Ecommerce.Automation;
using Baseline.Ecommerce.Models;
using Microsoft.Extensions.Logging;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Wishlist item DTO for API responses.
/// </summary>
public record WishlistItemDto(int ProductId, string ProductName, decimal Price, string? ImageUrl, DateTime AddedAt);

/// <summary>
/// Supported wishlist item types.
/// </summary>
public static class WishlistItemTypes
{
    public const string Product = "Product";
    public const string Event = "Event";
}

/// <summary>
/// Service interface for wishlist operations.
/// </summary>
public interface IWishlistService
{
    /// <summary>
    /// Gets all wishlist items for a member.
    /// </summary>
    Task<IReadOnlyList<WishlistItemInfo>> GetWishlistAsync(int memberId);

    /// <summary>
    /// Gets wishlist items for a member filtered by item type.
    /// </summary>
    Task<IReadOnlyList<WishlistItemInfo>> GetWishlistAsync(int memberId, string itemType);

    /// <summary>
    /// Adds a product to the member's wishlist.
    /// </summary>
    /// <returns>True if added, false if already exists.</returns>
    Task<bool> AddToWishlistAsync(int memberId, int productId);

    /// <summary>
    /// Adds an item to the member's wishlist with a specified type.
    /// </summary>
    /// <returns>True if added, false if already exists.</returns>
    Task<bool> AddToWishlistAsync(int memberId, int itemId, string itemType);

    /// <summary>
    /// Removes a product from the member's wishlist.
    /// </summary>
    /// <returns>True if removed, false if not found.</returns>
    Task<bool> RemoveFromWishlistAsync(int memberId, int productId);

    /// <summary>
    /// Removes an item from the member's wishlist by type.
    /// </summary>
    Task<bool> RemoveFromWishlistAsync(int memberId, int itemId, string itemType);

    /// <summary>
    /// Checks if a product is in the member's wishlist.
    /// </summary>
    Task<bool> IsInWishlistAsync(int memberId, int productId);

    /// <summary>
    /// Checks if an item is in the member's wishlist by type.
    /// </summary>
    Task<bool> IsInWishlistAsync(int memberId, int itemId, string itemType);

    /// <summary>
    /// Gets the wishlist item count for a member.
    /// </summary>
    Task<int> GetWishlistCountAsync(int memberId);
}

/// <summary>
/// Default implementation of IWishlistService using WishlistItemInfo.
/// </summary>
public class WishlistService(
    IInfoProvider<WishlistItemInfo> wishlistProvider,
    IAutomationEventInterceptor automationEvents,
    ILogger<WishlistService> logger) : IWishlistService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<WishlistItemInfo>> GetWishlistAsync(int memberId)
    {
        var items = await wishlistProvider.Get()
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemMemberID), memberId)
            .OrderByDescending(nameof(WishlistItemInfo.WishlistItemCreatedWhen))
            .GetEnumerableTypedResultAsync();

        return items.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WishlistItemInfo>> GetWishlistAsync(int memberId, string itemType)
    {
        var items = await wishlistProvider.Get()
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemMemberID), memberId)
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemType), itemType)
            .OrderByDescending(nameof(WishlistItemInfo.WishlistItemCreatedWhen))
            .GetEnumerableTypedResultAsync();

        return items.ToList();
    }

    /// <inheritdoc />
    public async Task<bool> AddToWishlistAsync(int memberId, int productId) =>
        await AddToWishlistAsync(memberId, productId, WishlistItemTypes.Product);

    /// <inheritdoc />
    public async Task<bool> AddToWishlistAsync(int memberId, int itemId, string itemType)
    {
        if (await IsInWishlistAsync(memberId, itemId, itemType))
        {
            logger.LogDebug("{ItemType} {ItemId} already in wishlist for member {MemberId}", itemType, itemId, memberId);
            return false;
        }

        var item = new WishlistItemInfo
        {
            WishlistItemMemberID = memberId,
            WishlistItemProductID = itemId,
            WishlistItemType = itemType,
            WishlistItemCreatedWhen = DateTime.UtcNow
        };

        wishlistProvider.Set(item);
        logger.LogInformation("Added {ItemType} {ItemId} to wishlist for member {MemberId}", itemType, itemId, memberId);

        // Fire automation trigger for wishlist update (best-effort)
        if (itemType == WishlistItemTypes.Product)
        {
            await automationEvents.OnWishlistUpdatedAsync(memberId, itemId, added: true);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RemoveFromWishlistAsync(int memberId, int productId) =>
        await RemoveFromWishlistAsync(memberId, productId, WishlistItemTypes.Product);

    /// <inheritdoc />
    public async Task<bool> RemoveFromWishlistAsync(int memberId, int itemId, string itemType)
    {
        var item = (await wishlistProvider.Get()
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemMemberID), memberId)
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemProductID), itemId)
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemType), itemType)
            .GetEnumerableTypedResultAsync())
            .FirstOrDefault();

        if (item == null)
        {
            return false;
        }

        wishlistProvider.Delete(item);
        logger.LogInformation("Removed {ItemType} {ItemId} from wishlist for member {MemberId}", itemType, itemId, memberId);

        // Fire automation trigger for wishlist update (best-effort)
        if (itemType == WishlistItemTypes.Product)
        {
            await automationEvents.OnWishlistUpdatedAsync(memberId, itemId, added: false);
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> IsInWishlistAsync(int memberId, int productId) =>
        await IsInWishlistAsync(memberId, productId, WishlistItemTypes.Product);

    /// <inheritdoc />
    public async Task<bool> IsInWishlistAsync(int memberId, int itemId, string itemType)
    {
        var count = (await wishlistProvider.Get()
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemMemberID), memberId)
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemProductID), itemId)
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemType), itemType)
            .GetEnumerableTypedResultAsync())
            .Count();

        return count > 0;
    }

    /// <inheritdoc />
    public async Task<int> GetWishlistCountAsync(int memberId)
    {
        return (await wishlistProvider.Get()
            .WhereEquals(nameof(WishlistItemInfo.WishlistItemMemberID), memberId)
            .GetEnumerableTypedResultAsync())
            .Count();
    }
}
