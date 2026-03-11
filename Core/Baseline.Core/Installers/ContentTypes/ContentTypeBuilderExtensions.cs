using Baseline.Core.Installers.Schemas;

namespace Baseline.Core.Installers.ContentTypes;

/// <summary>
/// Extension methods for ContentTypeBuilder to add common Baseline configurations.
/// </summary>
public static class ContentTypeBuilderExtensions
{
    /// <summary>
    /// Adds the Base.Metadata schema to this content type.
    /// </summary>
    public static ContentTypeBuilder WithMetadataSchema(this ContentTypeBuilder builder)
    {
        return builder.WithSchema(MetadataSchema.SchemaGuid);
    }

    /// <summary>
    /// Adds the Base.Redirect schema to this content type.
    /// </summary>
    public static ContentTypeBuilder WithRedirectSchema(this ContentTypeBuilder builder)
    {
        return builder.WithSchema(RedirectSchema.SchemaGuid);
    }

    /// <summary>
    /// Adds the Generic.HasImage schema to this content type.
    /// </summary>
    public static ContentTypeBuilder WithHasImageSchema(this ContentTypeBuilder builder)
    {
        return builder.WithSchema(HasImageSchema.SchemaGuid);
    }

    /// <summary>
    /// Adds all Baseline page schemas (Metadata + Redirect) to this content type.
    /// Commonly used for website page types.
    /// </summary>
    public static ContentTypeBuilder WithBaselinePageSchemas(this ContentTypeBuilder builder)
    {
        return builder
            .WithMetadataSchema()
            .WithRedirectSchema();
    }

    /// <summary>
    /// Configures this as a standard Baseline website page with URL and all page schemas.
    /// </summary>
    public static ContentTypeBuilder AsBaselineWebsitePage(this ContentTypeBuilder builder)
    {
        return builder
            .AsWebsite()
            .WithUrl()
            .WithBaselinePageSchemas();
    }

    /// <summary>
    /// Configures this as a standard Baseline media content type (reusable, with HasImage schema).
    /// </summary>
    public static ContentTypeBuilder AsBaselineMedia(this ContentTypeBuilder builder)
    {
        return builder
            .AsReusable()
            .WithUrl()
            .WithHasImageSchema();
    }

    /// <summary>
    /// Adds a standard Title field commonly used across content types.
    /// </summary>
    public static ContentTypeBuilder WithTitleField(
        this ContentTypeBuilder builder,
        string fieldName,
        Guid fieldGuid,
        bool required = true)
    {
        return builder.WithTextField(
            name: fieldName,
            fieldGuid: fieldGuid,
            caption: "Title",
            required: required,
            size: 200);
    }

    /// <summary>
    /// Adds a standard Description field commonly used across content types.
    /// </summary>
    public static ContentTypeBuilder WithDescriptionField(
        this ContentTypeBuilder builder,
        string fieldName,
        Guid fieldGuid,
        bool useLongText = true)
    {
        if (useLongText)
        {
            return builder.WithLongTextField(
                name: fieldName,
                fieldGuid: fieldGuid,
                caption: "Description",
                componentName: "Kentico.Administration.TextInput");
        }

        return builder.WithTextField(
            name: fieldName,
            fieldGuid: fieldGuid,
            caption: "Description",
            size: 500);
    }

    /// <summary>
    /// Adds a standard image asset field with common image extensions.
    /// </summary>
    public static ContentTypeBuilder WithImageAssetField(
        this ContentTypeBuilder builder,
        string fieldName,
        Guid fieldGuid,
        string? allowedExtensions = null,
        bool required = false)
    {
        return builder.WithAssetField(
            name: fieldName,
            fieldGuid: fieldGuid,
            caption: "Image",
            allowedExtensions: allowedExtensions ?? "jpg;jpeg;png;gif;webp;svg",
            required: required,
            requiredErrorMessage: required ? "An image is required" : null);
    }

    /// <summary>
    /// Adds a standard file asset field.
    /// </summary>
    public static ContentTypeBuilder WithFileAssetField(
        this ContentTypeBuilder builder,
        string fieldName,
        Guid fieldGuid,
        string? allowedExtensions = null,
        bool required = false)
    {
        return builder.WithAssetField(
            name: fieldName,
            fieldGuid: fieldGuid,
            caption: "File",
            allowedExtensions: allowedExtensions,
            required: required,
            requiredErrorMessage: required ? "A file is required" : null);
    }
}
