namespace Ecommerce.Models;

/// <summary>
/// OrderHistorySummary
/// </summary>
public class OrderHistorySummary
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Baseline.Ecommerce.Money TotalSpent { get; set; } = Baseline.Ecommerce.Money.Zero();
    public int OrderCount { get; set; }
    public IEnumerable<OrderSummary> RecentOrders { get; set; } = [];

    // Additional properties used by chevalroyal views
    public int TotalOrders { get; set; }
    public DateTime? LastOrderDate { get; set; }
}

/// <summary>
/// OrderSummary
/// </summary>
public class OrderSummary
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Baseline.Ecommerce.Money GrandTotal { get; set; } = Baseline.Ecommerce.Money.Zero();
    public int ItemCount { get; set; }
    public IList<OrderItemSummary> Items { get; set; } = [];

    // Additional properties used by chevalroyal views
    public string? PaymentMethod { get; set; }
    public string? ShippingMethod { get; set; }
}

/// <summary>
/// OrderItemSummary
/// </summary>
public class OrderItemSummary
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSku { get; set; }
    public int Quantity { get; set; }
    public Baseline.Ecommerce.Money UnitPrice { get; set; } = Baseline.Ecommerce.Money.Zero();
    public Baseline.Ecommerce.Money TotalPrice { get; set; } = Baseline.Ecommerce.Money.Zero();
    public string? ImageUrl { get; set; }

    // Additional properties used by chevalroyal views
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
}
