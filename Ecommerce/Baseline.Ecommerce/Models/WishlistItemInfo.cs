using System.Data;
using System.Runtime.Serialization;
using CMS;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.WishlistItemInfo), Baseline.Ecommerce.Models.WishlistItemInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Wishlist item info object. Stores a member's saved product reference.
/// </summary>
public class WishlistItemInfo : AbstractInfo<WishlistItemInfo, IInfoProvider<WishlistItemInfo>>, IInfoWithId
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.wishlistitem";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<WishlistItemInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.WishlistItem",
        idColumn: nameof(WishlistItemID),
        timeStampColumn: nameof(WishlistItemCreatedWhen),
        guidColumn: null,
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = false
    };

    #region Properties

    /// <summary>
    /// Wishlist item ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int WishlistItemID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(WishlistItemID)), 0);
        set => SetValue(nameof(WishlistItemID), value);
    }

    /// <summary>
    /// Member ID who owns this wishlist item.
    /// </summary>
    [DatabaseField]
    public virtual int WishlistItemMemberID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(WishlistItemMemberID)), 0);
        set => SetValue(nameof(WishlistItemMemberID), value);
    }

    /// <summary>
    /// Content item ID of the product or event.
    /// </summary>
    [DatabaseField]
    public virtual int WishlistItemProductID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(WishlistItemProductID)), 0);
        set => SetValue(nameof(WishlistItemProductID), value);
    }

    /// <summary>
    /// Item type discriminator: "Product" or "Event".
    /// </summary>
    [DatabaseField]
    public virtual string WishlistItemType
    {
        get => ValidationHelper.GetString(GetValue(nameof(WishlistItemType)), "Product");
        set => SetValue(nameof(WishlistItemType), value);
    }

    /// <summary>
    /// When the item was added to the wishlist.
    /// </summary>
    [DatabaseField]
    public virtual DateTime WishlistItemCreatedWhen
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(WishlistItemCreatedWhen)), DateTime.MinValue);
        set => SetValue(nameof(WishlistItemCreatedWhen), value);
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates an empty WishlistItemInfo.
    /// </summary>
    public WishlistItemInfo() : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a WishlistItemInfo from a DataRow.
    /// </summary>
    public WishlistItemInfo(DataRow dr) : base(TYPEINFO, dr)
    {
    }

    #endregion
}
