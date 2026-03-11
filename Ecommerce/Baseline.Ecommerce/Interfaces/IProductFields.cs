namespace Baseline.Ecommerce;

/// <summary>
/// Interface for content types that have the ProductFields reusable schema.
/// Apply the ProductFields schema to content types representing products.
/// </summary>
/// <remarks>
/// This interface defines the subset of fields from the ProductFields reusable schema
/// that the Baseline.Ecommerce library needs to access. The site's generated
/// Generic.IProductFields interface will have all fields, but this interface
/// is used with duck-typing via reflection for cross-library compatibility.
/// </remarks>
public interface IProductFields
{
    /// <summary>
    /// Gets or sets the product name displayed to customers.
    /// </summary>
    string ProductFieldName { get; set; }

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    string ProductFieldDescription { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    decimal ProductFieldPrice { get; set; }

    /// <summary>
    /// Gets or sets the tax class GUIDs for this product.
    /// </summary>
    IEnumerable<Guid> ProductFieldTaxClasses { get; set; }
}

