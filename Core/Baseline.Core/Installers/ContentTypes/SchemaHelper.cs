using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.FormEngine;

namespace Baseline.Core.Installers.ContentTypes;

/// <summary>
/// Helper class for working with reusable schema references in content types.
/// </summary>
public static class SchemaHelper
{
    /// <summary>
    /// Gets a reusable schema from the content item common data form by name.
    /// </summary>
    /// <param name="schemaName">The schema name (e.g., "Base.Metadata")</param>
    /// <returns>The schema info or null if not found</returns>
    public static FormSchemaInfo? GetSchema(string schemaName)
    {
        var form = FormHelper.GetFormInfo(ContentItemCommonDataInfo.OBJECT_TYPE, false);
        return form.GetFormSchema(schemaName);
    }

    /// <summary>
    /// Gets a reusable schema GUID by name.
    /// </summary>
    /// <param name="schemaName">The schema name (e.g., "Base.Metadata")</param>
    /// <returns>The schema GUID or null if not found</returns>
    public static Guid? GetSchemaGuid(string schemaName)
    {
        return GetSchema(schemaName)?.Guid;
    }

    /// <summary>
    /// Checks if a schema exists by name.
    /// </summary>
    /// <param name="schemaName">The schema name</param>
    /// <returns>True if schema exists</returns>
    public static bool SchemaExists(string schemaName)
    {
        return GetSchema(schemaName) != null;
    }

    /// <summary>
    /// Gets multiple schema GUIDs, returning only those that exist.
    /// </summary>
    /// <param name="schemaNames">The schema names to look up</param>
    /// <returns>Dictionary of schema name to GUID for existing schemas</returns>
    public static Dictionary<string, Guid> GetSchemaGuids(params string[] schemaNames)
    {
        var result = new Dictionary<string, Guid>();
        var form = FormHelper.GetFormInfo(ContentItemCommonDataInfo.OBJECT_TYPE, false);

        foreach (var name in schemaNames)
        {
            var schema = form.GetFormSchema(name);
            if (schema != null)
            {
                result[name] = schema.Guid;
            }
        }

        return result;
    }

    /// <summary>
    /// Validates that all required schemas exist, throwing if any are missing.
    /// </summary>
    /// <param name="schemaNames">The required schema names</param>
    /// <exception cref="InvalidOperationException">Thrown if any schemas are missing</exception>
    public static void RequireSchemas(params string[] schemaNames)
    {
        var form = FormHelper.GetFormInfo(ContentItemCommonDataInfo.OBJECT_TYPE, false);
        var missing = schemaNames.Where(name => form.GetFormSchema(name) == null).ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Required schemas are missing: {string.Join(", ", missing)}. " +
                "Ensure BaselineModuleInstaller.Install() has been called first.");
        }
    }

    /// <summary>
    /// Gets the Baseline core schema GUIDs (Metadata, Redirect, HasImage).
    /// </summary>
    /// <returns>Tuple with the three core schema GUIDs</returns>
    public static (Guid? Metadata, Guid? Redirect, Guid? HasImage) GetBaselineSchemas()
    {
        var schemas = GetSchemaGuids("Base.Metadata", "Base.Redirect", "Generic.HasImage");

        return (
            schemas.GetValueOrDefault("Base.Metadata"),
            schemas.GetValueOrDefault("Base.Redirect"),
            schemas.GetValueOrDefault("Generic.HasImage")
        );
    }

    /// <summary>
    /// Gets the required Baseline page schemas (Metadata and Redirect) with error logging.
    /// Returns null if any required schema is missing.
    /// </summary>
    /// <param name="eventLogService">Event log service for error logging</param>
    /// <param name="contentTypeName">Name of the content type being created (for error messages)</param>
    /// <returns>Tuple with MetadataGuid and RedirectGuid, or null if missing</returns>
    public static (Guid MetadataGuid, Guid RedirectGuid)? GetBaselineSchemas(
        IEventLogService eventLogService,
        string contentTypeName)
    {
        var metadataGuid = GetSchemaGuid("Base.Metadata");
        var redirectGuid = GetSchemaGuid("Base.Redirect");

        if (!metadataGuid.HasValue || !redirectGuid.HasValue)
        {
            var missing = new List<string>();
            if (!metadataGuid.HasValue) missing.Add("Base.Metadata");
            if (!redirectGuid.HasValue) missing.Add("Base.Redirect");

            eventLogService.LogError(
                "StartingSiteInstaller",
                "MissingSchemas",
                eventDescription: $"Cannot create {contentTypeName} - missing schemas: {string.Join(", ", missing)}");
            return null;
        }

        return (metadataGuid.Value, redirectGuid.Value);
    }
}
