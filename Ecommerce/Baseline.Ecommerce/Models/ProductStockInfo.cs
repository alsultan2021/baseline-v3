using System.Data;
using System.Runtime.Serialization;
using System.Text.Json;
using CMS;
using CMS.ContentEngine;
using CMS.DataEngine;
using CMS.Helpers;

[assembly: RegisterObjectType(typeof(Baseline.Ecommerce.Models.ProductStockInfo), Baseline.Ecommerce.Models.ProductStockInfo.OBJECT_TYPE)]

namespace Baseline.Ecommerce.Models;

/// <summary>
/// Product stock info object for managing product inventory.
/// Tracks available and reserved quantities for products.
/// </summary>
public class ProductStockInfo : AbstractInfo<ProductStockInfo, IInfoProvider<ProductStockInfo>>, IInfoWithId, IInfoWithGuid
{
    /// <summary>
    /// Object type name.
    /// </summary>
    public const string OBJECT_TYPE = "baseline.productstock";

    /// <summary>
    /// Type info.
    /// </summary>
    public static readonly ObjectTypeInfo TYPEINFO = new(
        providerType: typeof(IInfoProvider<ProductStockInfo>),
        objectType: OBJECT_TYPE,
        objectClassName: "Baseline.ProductStock",
        idColumn: nameof(ProductStockID),
        timeStampColumn: nameof(ProductStockLastModified),
        guidColumn: nameof(ProductStockGuid),
        codeNameColumn: null,
        displayNameColumn: null,
        binaryColumn: null,
        parentIDColumn: null,
        parentObjectType: null)
    {
        TouchCacheDependencies = true,
        SupportsCloning = false,
        LogEvents = true
        // Note: Product reference uses ContentItems data type which doesn't require ObjectDependency
    };

    /// <summary>
    /// Product stock ID (primary key).
    /// </summary>
    [DatabaseField]
    public virtual int ProductStockID
    {
        get => ValidationHelper.GetInteger(GetValue(nameof(ProductStockID)), 0);
        set => SetValue(nameof(ProductStockID), value);
    }

    /// <summary>
    /// Product stock GUID.
    /// </summary>
    [DatabaseField]
    public virtual Guid ProductStockGuid
    {
        get => ValidationHelper.GetGuid(GetValue(nameof(ProductStockGuid)), Guid.Empty);
        set => SetValue(nameof(ProductStockGuid), value);
    }

    /// <summary>
    /// Content item reference linking to the product.
    /// Uses ContentItemReference for proper Admin UI content item selector support.
    /// </summary>
    [DatabaseField]
    public virtual IEnumerable<ContentItemReference> ProductStockProduct
    {
        get
        {
            var value = GetValue(nameof(ProductStockProduct));
            if (value is null or DBNull || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return [];
            }
            try
            {
                return JsonSerializer.Deserialize<IEnumerable<ContentItemReference>>(value.ToString()!) ?? [];
            }
            catch
            {
                return [];
            }
        }
        set => SetValue(nameof(ProductStockProduct), value is null ? null : JsonSerializer.Serialize(value));
    }

    /// <summary>
    /// Gets the first product's content item GUID, if any.
    /// </summary>
    public Guid? GetProductGuid() => ProductStockProduct.FirstOrDefault()?.Identifier;

    /// <summary>
    /// Sets the product reference using a GUID.
    /// </summary>
    /// <param name="productGuid">The product content item GUID.</param>
    public void SetProductGuid(Guid productGuid)
    {
        ProductStockProduct = productGuid == Guid.Empty
            ? []
            : [new ContentItemReference { Identifier = productGuid }];
    }

    /// <summary>
    /// Available quantity for purchase.
    /// </summary>
    [DatabaseField]
    public virtual decimal ProductStockAvailableQuantity
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(ProductStockAvailableQuantity)), 0m);
        set => SetValue(nameof(ProductStockAvailableQuantity), value);
    }

    /// <summary>
    /// Reserved quantity for pending orders.
    /// </summary>
    [DatabaseField]
    public virtual decimal ProductStockReservedQuantity
    {
        get => ValidationHelper.GetDecimal(GetValue(nameof(ProductStockReservedQuantity)), 0m);
        set => SetValue(nameof(ProductStockReservedQuantity), value);
    }

    /// <summary>
    /// Minimum threshold for low stock warning.
    /// </summary>
    [DatabaseField]
    public virtual decimal? ProductStockMinimumThreshold
    {
        get
        {
            var val = GetValue(nameof(ProductStockMinimumThreshold));
            return val is null or DBNull ? null : ValidationHelper.GetDecimal(val, 0m);
        }
        set => SetValue(nameof(ProductStockMinimumThreshold), value, value.HasValue);
    }

    /// <summary>
    /// Whether backorders are allowed when out of stock.
    /// </summary>
    [DatabaseField]
    public virtual bool ProductStockAllowBackorders
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(ProductStockAllowBackorders)), false);
        set => SetValue(nameof(ProductStockAllowBackorders), value);
    }

    /// <summary>
    /// Whether stock tracking is enabled for this product.
    /// </summary>
    [DatabaseField]
    public virtual bool ProductStockTrackingEnabled
    {
        get => ValidationHelper.GetBoolean(GetValue(nameof(ProductStockTrackingEnabled)), true);
        set => SetValue(nameof(ProductStockTrackingEnabled), value);
    }

    /// <summary>
    /// Last modified timestamp.
    /// </summary>
    [DatabaseField]
    public virtual DateTime ProductStockLastModified
    {
        get => ValidationHelper.GetDateTime(GetValue(nameof(ProductStockLastModified)), DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(ProductStockLastModified), value);
    }

    /// <summary>
    /// Deletes the object using appropriate provider.
    /// </summary>
    protected override void DeleteObject() =>
        Provider.Delete(this);

    /// <summary>
    /// Updates the object using appropriate provider.
    /// </summary>
    protected override void SetObject() =>
        Provider.Set(this);

    /// <summary>
    /// Constructor for deserialization.
    /// </summary>
    protected ProductStockInfo(SerializationInfo info, StreamingContext context)
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates an empty instance.
    /// </summary>
    public ProductStockInfo()
        : base(TYPEINFO)
    {
    }

    /// <summary>
    /// Creates a new instance from a DataRow.
    /// </summary>
    public ProductStockInfo(DataRow dr)
        : base(TYPEINFO, dr)
    {
    }

    /// <summary>
    /// Calculates the effective available quantity (available minus reserved).
    /// </summary>
    public decimal GetEffectiveAvailableQuantity() =>
        Math.Max(0, ProductStockAvailableQuantity - ProductStockReservedQuantity);

    /// <summary>
    /// Gets the stock status based on quantities and thresholds.
    /// </summary>
    public StockStatus GetStockStatus()
    {
        var effectiveQuantity = GetEffectiveAvailableQuantity();

        if (effectiveQuantity <= 0)
        {
            return ProductStockAllowBackorders ? StockStatus.Backorder : StockStatus.OutOfStock;
        }

        if (ProductStockMinimumThreshold.HasValue && effectiveQuantity <= ProductStockMinimumThreshold.Value)
        {
            return StockStatus.LowStock;
        }

        return StockStatus.InStock;
    }
}
