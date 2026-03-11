using System.ComponentModel;
using System.Text;

using CMS.ContentEngine.Internal;
using CMS.DataEngine;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Baseline.AI.Admin.Plugins;

/// <summary>
/// AIRA plugin for discovering and inspecting reusable field schemas and
/// their relationships to content types. Enables AIRA to understand the
/// full content model — not just individual content types but the shared
/// schema building blocks they use.
/// </summary>
[Description("Inspects reusable field schemas: lists schemas, shows fields and settings, " +
             "identifies which content types use each schema, and detects unused schemas.")]
public sealed class ReusableSchemaAiraPlugin(
    IServiceProvider serviceProvider,
    ILogger<ReusableSchemaAiraPlugin> logger) : IAiraPlugin
{
    /// <inheritdoc />
    public string PluginName => "ReusableSchema";

    // ──────────────────────────────────────────────────────────────
    //  List all reusable field schemas
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Lists all reusable field schemas in the system.
    /// </summary>
    [KernelFunction("list_reusable_schemas")]
    [Description("Lists all reusable field schemas (shared field groups used across content types). " +
                 "Shows name, GUID, and how many content types use each schema.")]
    public string ListReusableSchemas()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<IReusableFieldSchemaManager>();
            if (manager is null)
            {
                return "Error: Reusable field schema manager not available.";
            }

            var schemas = manager.GetAll().ToList();
            if (schemas.Count == 0)
            {
                return "No reusable field schemas found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Reusable Field Schemas ({schemas.Count})");
            sb.AppendLine();
            sb.AppendLine("| Name | Display Name | GUID | Used By |");
            sb.AppendLine("|------|-------------|------|---------|");

            foreach (var schema in schemas)
            {
                var usedBy = manager.GetContentTypesWithSchema(schema.Guid);
                int usedByCount = usedBy.Count();

                sb.AppendLine($"| {schema.Name} | {schema.DisplayName} | " +
                    $"{schema.Guid:D} | {usedByCount} type(s) |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReusableSchema: list failed");
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Get schema details (fields)
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets detailed field definitions for a reusable field schema.
    /// </summary>
    [KernelFunction("get_schema_fields")]
    [Description("Gets the field definitions for a reusable field schema. Shows field names, " +
                 "data types, visibility settings, and default values.")]
    public string GetSchemaFields(
        [Description("Schema name (e.g. 'Base.Metadata' or 'ChevalRoyal.SEOFields')")] string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return "Error: Provide a schema name.";
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<IReusableFieldSchemaManager>();
            if (manager is null)
            {
                return "Error: Reusable field schema manager not available.";
            }

            var schemas = manager.GetAll().ToList();
            var schema = schemas.FirstOrDefault(s =>
                s.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase));

            if (schema is null)
            {
                return $"Schema '{schemaName}' not found. Use list_reusable_schemas to see available schemas.";
            }

            var fields = manager.GetSchemaFields(schema.Name).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"## Schema: {schema.DisplayName}");
            sb.AppendLine($"**Name**: {schema.Name} | **GUID**: {schema.Guid:D}");
            sb.AppendLine();

            if (fields.Count == 0)
            {
                sb.AppendLine("No fields defined.");
                return sb.ToString();
            }

            sb.AppendLine($"### Fields ({fields.Count})");
            sb.AppendLine();
            sb.AppendLine("| Field Name | Data Type | Required | Visible | Settings |");
            sb.AppendLine("|------------|-----------|----------|---------|----------|");

            foreach (var field in fields)
            {
                string required = field.AllowEmpty ? "No" : "Yes";
                string visible = field.Visible ? "Yes" : "No";

                // Extract key settings
                var settingsInfo = new List<string>();
                if (field.Settings.Contains("AllowedContentItemTypeIdentifiers")
                    && field.Settings["AllowedContentItemTypeIdentifiers"] is string allowedJson
                    && !string.IsNullOrEmpty(allowedJson))
                {
                    settingsInfo.Add("has content type filter");
                }

                if (field.Settings.Contains("MaxLength")
                    && field.Settings["MaxLength"] is { } maxLen)
                {
                    settingsInfo.Add($"max:{maxLen}");
                }

                string settings = settingsInfo.Count > 0 ? string.Join(", ", settingsInfo) : "—";

                sb.AppendLine($"| {field.Name} | {field.DataType} | {required} | {visible} | {settings} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReusableSchema: get fields failed for {Name}", schemaName);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Schema usage — which content types use it
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows which content types use a specific reusable field schema.
    /// </summary>
    [KernelFunction("get_schema_usage")]
    [Description("Shows all content types that use a specific reusable field schema. " +
                 "Useful to understand the impact of schema changes.")]
    public string GetSchemaUsage(
        [Description("Schema name (e.g. 'Base.Metadata')")] string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return "Error: Provide a schema name.";
        }

        try
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<IReusableFieldSchemaManager>();
            if (manager is null)
            {
                return "Error: Reusable field schema manager not available.";
            }

            var schemas = manager.GetAll().ToList();
            var schema = schemas.FirstOrDefault(s =>
                s.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase));

            if (schema is null)
            {
                return $"Schema '{schemaName}' not found.";
            }

            var contentTypeGuids = manager.GetContentTypesWithSchema(schema.Guid).ToList();

            if (contentTypeGuids.Count == 0)
            {
                return $"Schema '{schema.DisplayName}' is not used by any content types.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Content Types Using: {schema.DisplayName}");
            sb.AppendLine();
            sb.AppendLine("| Content Type | Code Name | Type |");
            sb.AppendLine("|-------------|-----------|------|");

            foreach (var classId in contentTypeGuids)
            {
                var classInfo = DataClassInfoProvider.GetDataClassInfo(classId);
                if (classInfo is null)
                {
                    sb.AppendLine($"| (unknown) | ID: {classId} | — |");
                    continue;
                }

                string classType = classInfo.ClassContentTypeType switch
                {
                    "Website" => "Page",
                    "Reusable" => "Reusable",
                    "Email" => "Email",
                    _ => classInfo.ClassContentTypeType ?? "—"
                };

                sb.AppendLine($"| {classInfo.ClassDisplayName} | {classInfo.ClassName} | {classType} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReusableSchema: usage lookup failed for {Name}", schemaName);
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Find unused schemas
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Finds reusable field schemas that are not used by any content type.
    /// </summary>
    [KernelFunction("find_unused_schemas")]
    [Description("Identifies reusable field schemas not referenced by any content type. " +
                 "These may be candidates for cleanup or indicate incomplete setup.")]
    public string FindUnusedSchemas()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<IReusableFieldSchemaManager>();
            if (manager is null)
            {
                return "Error: Reusable field schema manager not available.";
            }

            var schemas = manager.GetAll().ToList();
            var unused = new List<ReusableFieldSchema>();

            foreach (var schema in schemas)
            {
                var usedBy = manager.GetContentTypesWithSchema(schema.Guid);
                if (!usedBy.Any())
                {
                    unused.Add(schema);
                }
            }

            if (unused.Count == 0)
            {
                return "All reusable field schemas are in use.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Unused Reusable Field Schemas ({unused.Count}/{schemas.Count})");
            sb.AppendLine();

            foreach (var schema in unused)
            {
                var fields = manager.GetSchemaFields(schema.Name).ToList();
                sb.AppendLine($"- **{schema.DisplayName}** ({schema.Name}) — {fields.Count} field(s)");
            }

            sb.AppendLine();
            sb.AppendLine("These schemas are not referenced by any content type and may be candidates for cleanup.");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReusableSchema: unused check failed");
            return $"Error: {ex.Message}";
        }
    }

    // ──────────────────────────────────────────────────────────────
    //  Content model overview — schemas + content types
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Provides a comprehensive content model overview showing all schemas and
    /// their relationships to content types.
    /// </summary>
    [KernelFunction("content_model_overview")]
    [Description("Generates a comprehensive content model overview showing all reusable schemas, " +
                 "their fields, and which content types use them. Useful for understanding the " +
                 "full content architecture.")]
    public string ContentModelOverview()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var manager = scope.ServiceProvider.GetService<IReusableFieldSchemaManager>();
            if (manager is null)
            {
                return "Error: Reusable field schema manager not available.";
            }

            var schemas = manager.GetAll().ToList();

            var sb = new StringBuilder();
            sb.AppendLine("## Content Model Overview");
            sb.AppendLine($"Total reusable schemas: **{schemas.Count}**");
            sb.AppendLine();

            foreach (var schema in schemas)
            {
                var fields = manager.GetSchemaFields(schema.Name).ToList();
                var usedBy = manager.GetContentTypesWithSchema(schema.Guid).ToList();

                sb.AppendLine($"### {schema.DisplayName}");
                sb.AppendLine($"Name: `{schema.Name}` | Fields: {fields.Count} | Used by: {usedBy.Count} type(s)");
                sb.AppendLine();

                if (fields.Count > 0)
                {
                    sb.AppendLine("Fields:");
                    foreach (var field in fields)
                    {
                        string req = field.AllowEmpty ? "" : " *(required)*";
                        sb.AppendLine($"- `{field.Name}` ({field.DataType}){req}");
                    }

                    sb.AppendLine();
                }

                if (usedBy.Count > 0)
                {
                    sb.Append("Content types: ");
                    var names = new List<string>();
                    foreach (var classId in usedBy)
                    {
                        var ci = DataClassInfoProvider.GetDataClassInfo(classId);
                        names.Add(ci?.ClassName ?? $"ID:{classId}");
                    }

                    sb.AppendLine(string.Join(", ", names));
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "ReusableSchema: model overview failed");
            return $"Error: {ex.Message}";
        }
    }
}
