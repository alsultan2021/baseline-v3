using System.ComponentModel;
using System.Text.Json;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// MCP tools for e-commerce discovery and introspection.
/// Read-only tools using SQL queries (works regardless of commerce feature state).
/// </summary>
[McpServerToolType]
public static class EcommerceTool
{
    /// <summary>
    /// Gets all order statuses configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetOrderStatuses),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Order Statuses"),
    Description("Gets all order statuses configured in Xperience Commerce (if available)")]
    public static async Task<string> GetOrderStatuses(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                StatusID, StatusGUID, StatusName, StatusDisplayName,
                StatusEnabled, StatusMarkOrderAsPaid, StatusMarkOrderAsCompleted,
                StatusSendNotification, StatusOrder
            FROM Commerce_OrderStatus
            ORDER BY StatusOrder
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var statuses = new List<object>();
            while (await reader.ReadAsync())
            {
                statuses.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Enabled = reader.GetBoolean(4),
                    MarksAsPaid = reader.GetBoolean(5),
                    MarksAsCompleted = reader.GetBoolean(6),
                    SendsNotification = reader.GetBoolean(7)
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = statuses.Count,
                Statuses = statuses
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Commerce tables not found. Ensure UseCommerce() is enabled.",
                Message = "The Commerce_OrderStatus table does not exist."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets all shipping options configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetShippingOptions),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Shipping Options"),
    Description("Gets all shipping options configured in Xperience Commerce (if available)")]
    public static async Task<string> GetShippingOptions(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                ShippingOptionID, ShippingOptionGUID, ShippingOptionName,
                ShippingOptionDisplayName, ShippingOptionEnabled
            FROM COM_ShippingOption
            ORDER BY ShippingOptionName
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<object>();
            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Enabled = reader.GetBoolean(4)
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = items.Count,
                ShippingOptions = items
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Commerce tables not found. Ensure UseCommerce() is enabled.",
                Message = "The COM_ShippingOption table does not exist."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets all payment methods configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetPaymentMethods),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Payment Methods"),
    Description("Gets all payment methods configured in Xperience Commerce (if available)")]
    public static async Task<string> GetPaymentMethods(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                PaymentOptionID, PaymentOptionGUID, PaymentOptionName,
                PaymentOptionDisplayName, PaymentOptionEnabled
            FROM COM_PaymentOption
            ORDER BY PaymentOptionName
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<object>();
            while (await reader.ReadAsync())
            {
                items.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Enabled = reader.GetBoolean(4)
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = items.Count,
                PaymentMethods = items
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Commerce tables not found. Ensure UseCommerce() is enabled.",
                Message = "The COM_PaymentOption table does not exist."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets all currencies configured in the system.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetCurrencies),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Currencies"),
    Description("Gets all currencies configured in Xperience Commerce (if available)")]
    public static async Task<string> GetCurrencies(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        const string sql = """
            SELECT 
                CurrencyID, CurrencyGUID, CurrencyName, CurrencyDisplayName,
                CurrencyCode, CurrencyFormatString, CurrencyRoundTo,
                CurrencyEnabled, CurrencyIsMain
            FROM COM_Currency
            ORDER BY CurrencyIsMain DESC, CurrencyCode
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var items = new List<object>();
            string? mainCurrency = null;

            while (await reader.ReadAsync())
            {
                var isMain = reader.GetBoolean(8);
                var code = reader.GetString(4);
                if (isMain) mainCurrency = code;

                items.Add(new
                {
                    Id = reader.GetInt32(0),
                    Guid = reader.GetGuid(1),
                    Name = reader.GetString(2),
                    DisplayName = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Code = code,
                    FormatString = reader.IsDBNull(5) ? null : reader.GetString(5),
                    RoundTo = reader.GetInt32(6),
                    Enabled = reader.GetBoolean(7),
                    IsMain = isMain
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalCount = items.Count,
                MainCurrency = mainCurrency,
                Currencies = items
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Commerce tables not found. Ensure UseCommerce() is enabled.",
                Message = "The COM_Currency table does not exist."
            }, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Gets order statistics summary.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetOrderStatistics),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get Order Statistics"),
    Description("Gets order count statistics by status (if commerce is enabled)")]
    public static async Task<string> GetOrderStatistics(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("Only include orders from last N days (optional)")] int? lastDays = null)
    {
        var connString = configuration.GetConnectionString("CMSConnectionString");
        if (string.IsNullOrEmpty(connString))
        {
            return JsonSerializer.Serialize(new { Error = "No connection string found" }, options.Value.SerializerOptions);
        }

        var sql = """
            SELECT 
                s.StatusID, s.StatusName, s.StatusDisplayName,
                COUNT(o.OrderID) as OrderCount
            FROM Commerce_OrderStatus s
            LEFT JOIN Commerce_Order o ON o.OrderOrderStatusID = s.StatusID
            """;

        if (lastDays.HasValue)
        {
            sql += $" AND o.OrderDate >= DATEADD(day, -{lastDays.Value}, GETUTCDATE())";
        }

        sql += """
            
            GROUP BY s.StatusID, s.StatusName, s.StatusDisplayName, s.StatusOrder
            ORDER BY s.StatusOrder
            """;

        try
        {
            await using var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var stats = new List<object>();
            var total = 0;

            while (await reader.ReadAsync())
            {
                var count = reader.GetInt32(3);
                total += count;
                stats.Add(new
                {
                    StatusId = reader.GetInt32(0),
                    StatusName = reader.GetString(1),
                    StatusDisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Count = count
                });
            }

            return JsonSerializer.Serialize(new
            {
                TotalOrders = total,
                Period = lastDays.HasValue ? $"Last {lastDays} days" : "All time",
                ByStatus = stats
            }, options.Value.SerializerOptions);
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name"))
        {
            return JsonSerializer.Serialize(new
            {
                Error = "Commerce tables not found. Ensure UseCommerce() is enabled.",
                Message = "The Commerce_Order or Commerce_OrderStatus tables do not exist."
            }, options.Value.SerializerOptions);
        }
    }
}
