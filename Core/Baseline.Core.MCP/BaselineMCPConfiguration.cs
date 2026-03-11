using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;

namespace Baseline.Core.MCP;

/// <summary>
/// Configuration options for the Baseline MCP server integration with Xperience by Kentico.
/// Note: Widget/content modeling handled by official Kentico MCP (kentico-cm-mcp, xperience-management-api)
/// </summary>
public class BaselineMCPConfiguration
{
    /// <summary>
    /// Gets or sets the base URL path for the MCP server endpoint.
    /// </summary>
    public string BasePath { get; set; } = "/baseline-mcp";

    /// <summary>
    /// Whether to use HTTPS for URL generation.
    /// </summary>
    public bool UseHttps { get; set; } = false;

    /// <summary>
    /// Additional assemblies to scan for MCP tools and prompts.
    /// </summary>
    public List<Assembly> ScannedAssemblies { get; set; } = [];

    /// <summary>
    /// JSON serializer options for responses.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; set; } = new() { WriteIndented = true };

    /// <summary>
    /// Content types to expose through content retrieval tools.
    /// </summary>
    [Required]
    public List<ContentTypeConfiguration> ContentTypes { get; set; } = [];

    /// <summary>
    /// Maximum items per request (prevents large result sets).
    /// </summary>
    public int MaxItemsPerRequest { get; set; } = 100;

    /// <summary>
    /// Filter content by current site context.
    /// </summary>
    public bool UseSiteContext { get; set; } = true;

    /// <summary>
    /// Cache duration in seconds (0 = disabled).
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Include system fields in responses.
    /// </summary>
    public bool IncludeSystemFields { get; set; } = false;

    /// <summary>
    /// Enable debug info (disable in production).
    /// </summary>
    public bool EnableDebugInfo { get; set; } = false;

    /// <summary>
    /// Enforce read-only SQL queries (blocks INSERT, UPDATE, DELETE, DROP, etc.).
    /// Default: true. Only set to false if you explicitly need write operations.
    /// </summary>
    public bool EnforceReadOnlyQueries { get; set; } = true;

    /// <summary>
    /// Allow write SQL operations (INSERT, UPDATE, DELETE) via dedicated tools.
    /// Default: false. DDL (DROP, ALTER, CREATE) always blocked regardless.
    /// </summary>
    public bool AllowWriteQueries { get; set; } = false;

    internal static void ValidateConfiguration(BaselineMCPConfiguration config)
    {
        config.ContentTypes ??= [];

        var invalid = config.ContentTypes.Where(ct => string.IsNullOrWhiteSpace(ct.CodeName));
        if (invalid.Any())
            throw new ValidationException("Content type code name cannot be empty.");

        if (string.IsNullOrWhiteSpace(config.BasePath))
            throw new ValidationException("Base path cannot be empty.");

        if (!config.BasePath.StartsWith('/'))
            throw new ValidationException("Base path must start with '/'.");

        if (config.MaxItemsPerRequest <= 0)
            throw new ValidationException("MaxItemsPerRequest must be > 0.");
    }
}

/// <summary>
/// Configuration for a content type exposed through MCP.
/// </summary>
public class ContentTypeConfiguration
{
    /// <summary>
    /// Content type code name.
    /// </summary>
    [Required]
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Display name (defaults to CodeName).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Fields to include (empty = all except system fields).
    /// </summary>
    public ICollection<string> IncludedFields { get; set; } = [];

    /// <summary>
    /// Fields to exclude (takes precedence).
    /// </summary>
    public ICollection<string> ExcludedFields { get; set; } = [];

    /// <summary>
    /// Field name mappings (original -> MCP name).
    /// </summary>
    public Dictionary<string, string> FieldMappings { get; set; } = [];

    /// <summary>
    /// Include related content items.
    /// </summary>
    public bool IncludeRelatedContent { get; set; } = false;

    /// <summary>
    /// Max depth for related content.
    /// </summary>
    public int MaxRelatedContentDepth { get; set; } = 1;
}
