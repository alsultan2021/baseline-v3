using Baseline.Ecommerce.Interfaces;

namespace Baseline.Ecommerce.Services;

/// <summary>
/// Default implementation for formatting product names with variant information.
/// </summary>
/// <param name="variantsExtractor">The product variants extractor.</param>
public sealed class ProductNameProvider(IProductVariantsExtractor variantsExtractor) : IProductNameProvider
{
    /// <inheritdoc />
    public string GetProductName<TProduct>(TProduct product, string? variantId = null)
    {
        if (product == null)
        {
            return "Unknown Product";
        }

        int? variantIdInt = null;
        if (variantId != null && int.TryParse(variantId, out int parsed))
        {
            variantIdInt = parsed;
        }

        // Use duck typing to get the product name
        string productName = GetProductFieldName(product) ?? "Unknown Product";

        var variantValues = variantsExtractor.ExtractVariantsValue(product);
        return FormatProductName(productName, variantValues, variantIdInt);
    }

    private static string? GetProductFieldName<TProduct>(TProduct product)
    {
        // Try to get ProductFieldName via reflection or interface
        var prop = typeof(TProduct).GetProperty("ProductFieldName");
        return prop?.GetValue(product) as string;
    }

    private static string FormatProductName(string productName, IDictionary<int, string> variants, int? variantId) =>
        variants.Count > 0 && variantId != null && variants.TryGetValue(variantId.Value, out string? variantValue)
            ? $"{productName} - {variantValue}"
            : productName;
}
