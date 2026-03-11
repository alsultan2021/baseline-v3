namespace Baseline.Ecommerce;

/// <summary>
/// Product information.
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Sku { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public Money Price { get; set; } = new();
    public Money? SalePrice { get; set; }
    public bool IsOnSale => SalePrice is not null && SalePrice.Amount < Price.Amount;
    public string? ImageUrl { get; set; }
    public IList<string> Images { get; set; } = [];
    public IList<Guid> CategoryIds { get; set; } = [];
    public ProductAvailability Availability { get; set; } = new();
}

/// <summary>
/// Product availability information.
/// </summary>
public class ProductAvailability
{
    public bool InStock { get; set; }
    public int? StockQuantity { get; set; }
    public bool AllowBackorder { get; set; }
    public string? AvailabilityText { get; set; }
    public DateTimeOffset? ExpectedRestockDate { get; set; }
}

/// <summary>
/// Product price information.
/// </summary>
public class ProductPrice
{
    public Money Price { get; set; } = new();
    public Money? SalePrice { get; set; }
    public Money? TaxAmount { get; set; }
    public string? DiscountDescription { get; set; }
}

/// <summary>
/// Product search request.
/// </summary>
public record ProductSearchRequest
{
    public string? Query { get; init; }
    public Guid? CategoryId { get; init; }
    public Money? MinPrice { get; init; }
    public Money? MaxPrice { get; init; }
    public bool? InStockOnly { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Paged result for lists.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
