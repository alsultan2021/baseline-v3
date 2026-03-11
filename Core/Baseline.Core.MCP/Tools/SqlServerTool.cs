using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Tools;

/// <summary>
/// SQL Server database tools for executing queries and retrieving metadata.
/// </summary>
[McpServerToolType]
public static partial class SqlServerTool
{
    private static readonly string[] BlockedKeywords =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "TRUNCATE", "ALTER", "CREATE",
        "EXEC", "EXECUTE", "GRANT", "REVOKE", "DENY", "BACKUP", "RESTORE",
        "MERGE", "BULK", "OPENROWSET", "OPENDATASOURCE", "XP_", "SP_"
    ];

    /// <summary>
    /// Validates that a query is read-only (SELECT/CTE only).
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    private static string? ValidateReadOnlyQuery(string query)
    {
        var normalized = query.Trim();

        // Remove comments
        normalized = SingleLineCommentRegex().Replace(normalized, " ");
        normalized = MultiLineCommentRegex().Replace(normalized, " ");
        normalized = normalized.ToUpperInvariant();

        // Must start with SELECT or WITH (CTE)
        if (!normalized.StartsWith("SELECT") && !normalized.StartsWith("WITH"))
        {
            return "Only SELECT or WITH (CTE) queries are allowed. Query must start with SELECT or WITH.";
        }

        // Check for blocked keywords
        foreach (var keyword in BlockedKeywords)
        {
            // Use word boundary check to avoid false positives
            if (Regex.IsMatch(normalized, $@"\b{keyword}\b"))
            {
                return $"Query contains blocked keyword: {keyword}. Only read-only queries are allowed.";
            }
        }

        return null;
    }

    [GeneratedRegex(@"--.*$", RegexOptions.Multiline)]
    private static partial Regex SingleLineCommentRegex();

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex MultiLineCommentRegex();

    /// <summary>
    /// Executes a read-only SQL query against the Xperience database.
    /// Only SELECT and CTE (WITH) queries are allowed.
    /// </summary>
    [McpServerTool(
        Name = nameof(ExecuteSQLQuery),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Execute SQL query (read-only)"),
    Description("Executes a read-only SQL query (SELECT/CTE only) against the Xperience by Kentico database")]
    public static async Task<string> ExecuteSQLQuery(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The SQL query to execute (SELECT or WITH only). Use GetSQLTables and GetSQLTableColumns first to discover valid table/column names.")] string query,
        CancellationToken cancellationToken)
    {
        // Enforce read-only by default
        if (options.Value.EnforceReadOnlyQueries)
        {
            string? validationError = ValidateReadOnlyQuery(query);
            if (validationError is not null)
            {
                var error = new
                {
                    error = true,
                    message = validationError,
                    hint = "Only SELECT and WITH (CTE) queries are allowed. Use GetSQLTables and GetSQLTableColumns to discover the schema.",
                    query
                };
                return JsonSerializer.Serialize(error, options.Value.SerializerOptions);
            }
        }

        string? connectionString = configuration.GetConnectionString("CMSConnectionString");

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var results = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                results.Add(row);
            }

            return JsonSerializer.Serialize(results, options.Value.SerializerOptions);
        }
        catch (SqlException ex)
        {
            var error = new
            {
                error = true,
                message = ex.Message,
                hint = "Use GetSQLTables to list valid tables, and GetSQLTableColumns to discover column names before querying.",
                query
            };
            return JsonSerializer.Serialize(error, options.Value.SerializerOptions);
        }
    }

    /// <summary>
    /// Lists all database tables.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetSQLTables),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get SQL tables"),
    Description("Lists all tables in the Xperience by Kentico database")]
    public static async Task<string> GetSQLTables(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        string query = """
            SELECT TABLE_SCHEMA, TABLE_NAME 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'
            """;

        return await ExecuteSQLQuery(options, configuration, query, cancellationToken);
    }

    /// <summary>
    /// Lists columns for a specific table.
    /// </summary>
    [McpServerTool(
        Name = nameof(GetSQLTableColumns),
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = true,
        Title = "Get SQL table columns"),
    Description("Lists all columns for a specified table in the Xperience by Kentico database")]
    public static async Task<string> GetSQLTableColumns(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The table name")] string tableName,
        CancellationToken cancellationToken)
    {
        string query = $"""
            SELECT 
                TABLE_SCHEMA,
                TABLE_NAME,
                COLUMN_NAME,
                ORDINAL_POSITION,
                COLUMN_DEFAULT,
                IS_NULLABLE,
                DATA_TYPE,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = '{tableName}'
            ORDER BY ORDINAL_POSITION
            """;

        return await ExecuteSQLQuery(options, configuration, query, cancellationToken);
    }

    #region Write Operations (INSERT, UPDATE, DELETE, ALTER, CREATE)

    private static readonly string[] AllowedWriteKeywords = ["INSERT", "UPDATE", "DELETE", "ALTER", "CREATE"];

    private static readonly string[] DangerousKeywords =
    [
        "DROP", "TRUNCATE", "GRANT", "REVOKE", "DENY",
        "BACKUP", "RESTORE", "BULK", "OPENROWSET", "OPENDATASOURCE", "XP_", "SP_"
    ];

    /// <summary>
    /// Validates that a write query is INSERT, UPDATE, DELETE, ALTER, or CREATE only — blocks admin commands.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    private static string? ValidateWriteQuery(string query)
    {
        var normalized = query.Trim();
        normalized = SingleLineCommentRegex().Replace(normalized, " ");
        normalized = MultiLineCommentRegex().Replace(normalized, " ");
        normalized = normalized.ToUpperInvariant();

        bool startsWithAllowed = AllowedWriteKeywords.Any(k => normalized.StartsWith(k));
        if (!startsWithAllowed)
        {
            return "Only INSERT, UPDATE, DELETE, ALTER, or CREATE statements are allowed. " +
                $"Query starts with: {normalized[..Math.Min(20, normalized.Length)]}...";
        }

        foreach (var keyword in DangerousKeywords)
        {
            if (Regex.IsMatch(normalized, $@"\b{keyword}\b"))
            {
                return $"Query contains blocked keyword: {keyword}. DDL and admin commands are not allowed.";
            }
        }

        return null;
    }

    /// <summary>
    /// Executes a SQL INSERT statement against the Xperience database.
    /// Requires AllowWriteQueries = true in MCP configuration.
    /// </summary>
    [McpServerTool(
        Name = nameof(ExecuteSQLInsert),
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Execute SQL INSERT"),
    Description("Executes a SQL INSERT statement against the Xperience by Kentico database. Requires write operations to be enabled in configuration.")]
    public static async Task<string> ExecuteSQLInsert(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The SQL INSERT statement to execute")] string query,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AllowWriteQueries)
            return "Write operations are disabled. Set AllowWriteQueries = true in BaselineMCPConfiguration to enable.";

        string? writeError = ValidateWriteQuery(query);
        if (writeError is not null)
            return writeError;

        var normalized = query.TrimStart().ToUpperInvariant();
        if (!normalized.StartsWith("INSERT"))
            return "Only INSERT statements are allowed with this tool.";

        return await ExecuteNonQueryAsync(configuration, query, cancellationToken);
    }

    /// <summary>
    /// Executes a SQL UPDATE statement against the Xperience database.
    /// Requires AllowWriteQueries = true in MCP configuration.
    /// </summary>
    [McpServerTool(
        Name = nameof(ExecuteSQLUpdate),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Execute SQL UPDATE"),
    Description("Executes a SQL UPDATE statement against the Xperience by Kentico database. Requires write operations to be enabled in configuration.")]
    public static async Task<string> ExecuteSQLUpdate(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The SQL UPDATE statement to execute")] string query,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AllowWriteQueries)
            return "Write operations are disabled. Set AllowWriteQueries = true in BaselineMCPConfiguration to enable.";

        string? writeError = ValidateWriteQuery(query);
        if (writeError is not null)
            return writeError;

        var normalized = query.TrimStart().ToUpperInvariant();
        if (!normalized.StartsWith("UPDATE"))
            return "Only UPDATE statements are allowed with this tool.";

        return await ExecuteNonQueryAsync(configuration, query, cancellationToken);
    }

    /// <summary>
    /// Executes a SQL DELETE statement against the Xperience database.
    /// Requires AllowWriteQueries = true in MCP configuration.
    /// </summary>
    [McpServerTool(
        Name = nameof(ExecuteSQLDelete),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Execute SQL DELETE"),
    Description("Executes a SQL DELETE statement against the Xperience by Kentico database. Requires write operations to be enabled in configuration.")]
    public static async Task<string> ExecuteSQLDelete(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The SQL DELETE statement to execute")] string query,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AllowWriteQueries)
            return "Write operations are disabled. Set AllowWriteQueries = true in BaselineMCPConfiguration to enable.";

        string? writeError = ValidateWriteQuery(query);
        if (writeError is not null)
            return writeError;

        var normalized = query.TrimStart().ToUpperInvariant();
        if (!normalized.StartsWith("DELETE"))
            return "Only DELETE statements are allowed with this tool.";

        return await ExecuteNonQueryAsync(configuration, query, cancellationToken);
    }

    /// <summary>
    /// Executes a SQL ALTER TABLE statement against the Xperience database.
    /// Requires AllowWriteQueries = true in MCP configuration.
    /// </summary>
    [McpServerTool(
        Name = nameof(ExecuteSQLAlter),
        Destructive = true,
        Idempotent = true,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Execute SQL ALTER TABLE"),
    Description("Executes a SQL ALTER TABLE statement against the Xperience by Kentico database. Requires write operations to be enabled in configuration.")]
    public static async Task<string> ExecuteSQLAlter(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The SQL ALTER TABLE statement to execute")] string query,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AllowWriteQueries)
            return "Write operations are disabled. Set AllowWriteQueries = true in BaselineMCPConfiguration to enable.";

        string? writeError = ValidateWriteQuery(query);
        if (writeError is not null)
            return writeError;

        var normalized = query.TrimStart().ToUpperInvariant();
        if (!normalized.StartsWith("ALTER"))
            return "Only ALTER statements are allowed with this tool.";

        return await ExecuteNonQueryAsync(configuration, query, cancellationToken);
    }

    /// <summary>
    /// Executes a SQL CREATE TABLE statement against the Xperience database.
    /// Requires AllowWriteQueries = true in MCP configuration.
    /// </summary>
    [McpServerTool(
        Name = nameof(ExecuteSQLCreate),
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        ReadOnly = false,
        Title = "Execute SQL CREATE TABLE"),
    Description("Executes a SQL CREATE TABLE statement against the Xperience by Kentico database. Requires write operations to be enabled in configuration.")]
    public static async Task<string> ExecuteSQLCreate(
        IOptions<BaselineMCPConfiguration> options,
        IConfiguration configuration,
        [Description("The SQL CREATE TABLE statement to execute")] string query,
        CancellationToken cancellationToken)
    {
        if (!options.Value.AllowWriteQueries)
            return "Write operations are disabled. Set AllowWriteQueries = true in BaselineMCPConfiguration to enable.";

        string? writeError = ValidateWriteQuery(query);
        if (writeError is not null)
            return writeError;

        var normalized = query.TrimStart().ToUpperInvariant();
        if (!normalized.StartsWith("CREATE"))
            return "Only CREATE statements are allowed with this tool.";

        return await ExecuteNonQueryAsync(configuration, query, cancellationToken);
    }

    /// <summary>
    /// Shared helper for executing non-query (INSERT/UPDATE/DELETE/ALTER/CREATE) statements.
    /// Returns affected row count as JSON.
    /// </summary>
    private static async Task<string> ExecuteNonQueryAsync(
        IConfiguration configuration,
        string query,
        CancellationToken cancellationToken)
    {
        string? connectionString = configuration.GetConnectionString("CMSConnectionString");

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            int rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            return JsonSerializer.Serialize(new { rowsAffected, statement = query.TrimStart()[..Math.Min(50, query.TrimStart().Length)] + "..." });
        }
        catch (SqlException ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = true,
                message = ex.Message,
                hint = "Use GetSQLTables and GetSQLTableColumns to verify table/column names before executing.",
                query
            });
        }
    }

    #endregion
}
