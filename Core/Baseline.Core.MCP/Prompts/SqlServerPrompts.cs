using System.ComponentModel;

using Microsoft.Extensions.AI;

using ModelContextProtocol.Server;

namespace Baseline.Core.MCP.Prompts;

/// <summary>
/// SQL Server expert prompts for database operations.
/// Note: Prompts not yet supported by VS Code MCP integration.
/// https://github.com/microsoft/vscode/issues/244173
/// </summary>
[McpServerPromptType]
public static class SqlServerPrompts
{
    /// <summary>
    /// SQL Server expert prompt for querying and introspection.
    /// </summary>
    [McpServerPrompt(Name = "sql_server_expert"),
    Description("Creates a prompt to help with SQL Server querying with valid syntax and introspection")]
    public static ChatMessage SqlServerExpert() =>
        new(ChatRole.User, """
        You are a SQL Server database expert for Xperience by Kentico. You can help users understand their database structure,
        write queries, and explain data relationships. When writing queries, ensure they are safe
        and follow best practices. Avoid suggesting queries that could modify or delete data unless
        explicitly requested.

        Available tools:
        - ExecuteSQLQuery: Run a read-only SQL query (SELECT/CTE) and get results
        - ExecuteSQLInsert: Insert rows into a table (requires AllowWriteQueries)
        - ExecuteSQLUpdate: Update rows in a table (requires AllowWriteQueries)
        - ExecuteSQLDelete: Delete rows from a table (requires AllowWriteQueries)
        - GetSQLTables: List all tables in the database
        - GetSQLTableColumns: Get column information for a table
        """);

    /// <summary>
    /// SQL Server analysis prompt for database insights.
    /// </summary>
    [McpServerPrompt(Name = "sql_server_analysis"),
    Description("Creates a prompt to help with SQL Server query result analysis and introspection")]
    public static ChatMessage SqlServerAnalysis() =>
        new(ChatRole.User, """
        Analyze the Xperience by Kentico database structure and provide insights about:
        1. Table relationships and foreign keys
        2. Potential indexing opportunities
        3. Data type choices and their implications
        4. Content type relationships
        
        Use the available tools to gather information before making recommendations.
        """);
}
