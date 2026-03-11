using Baseline.Ecommerce.Models;

using CSharpFunctionalExtensions;

namespace Baseline.Ecommerce.Interfaces;

/// <summary>
/// Interface for resolving fulfillment types from content type names.
/// Sites implement this to map their specific content types to fulfillment types.
/// </summary>
public interface IFulfillmentTypeResolver
{
    /// <summary>
    /// Resolves the fulfillment type code name from a content type name.
    /// </summary>
    /// <param name="contentTypeName">The content type name (e.g., "Generic.Product", "MVC.EventTicket").</param>
    /// <returns>The fulfillment type code name, or null if not handled by this resolver.</returns>
    string? ResolveFulfillmentTypeCodeName(string contentTypeName);
}

/// <summary>
/// Service for retrieving and working with fulfillment types.
/// Replaces the hardcoded ProductType enum with database-driven configuration.
/// </summary>
public interface IFulfillmentTypeService
{
    /// <summary>
    /// Gets all enabled fulfillment types.
    /// </summary>
    Task<IEnumerable<FulfillmentTypeInfo>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a fulfillment type by its code name.
    /// </summary>
    Task<Maybe<FulfillmentTypeInfo>> GetByCodeNameAsync(string codeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the fulfillment type for a content type.
    /// First checks registered resolvers, then falls back to convention-based matching.
    /// </summary>
    Task<FulfillmentTypeInfo> GetForContentTypeAsync(string contentTypeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if the shopping cart requires shipping based on its contents.
    /// </summary>
    Task<bool> CartRequiresShippingAsync(IEnumerable<string> contentTypeNames, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary fulfillment type display name for a cart based on its contents.
    /// </summary>
    Task<string> GetCartFulfillmentDisplayNameAsync(IEnumerable<string> contentTypeNames, CancellationToken cancellationToken = default);
}
