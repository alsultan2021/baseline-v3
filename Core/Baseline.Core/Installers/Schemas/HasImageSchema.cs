using CMS.ContentEngine.Internal;
using CMS.FormEngine;

namespace Baseline.Core.Installers.Schemas;

/// <summary>
/// Defines the Generic.HasImage reusable schema for content types that can provide OG images.
/// </summary>
public static class HasImageSchema
{
    public const string SchemaName = "Generic.HasImage";
    public const string SchemaCaption = "Has Image";
    public const string SchemaDescription = @"Place this Reusable Schema on the content type you wish to be able to select from for the Base.Metadata Reusable Schema's OG Image.

Should configure in the Core Baseline ContentItemAssetOptionsConfiguration.";

    public static readonly Guid SchemaGuid = Guid.Parse("03dda2f6-b776-48b2-92d7-c110682febe0");

    /// <summary>
    /// Creates the Generic.HasImage reusable schema if it doesn't exist.
    /// This schema has no fields - it's used as a marker interface for content types.
    /// </summary>
    public static void Install()
    {
        var form = FormHelper.GetFormInfo(ContentItemCommonDataInfo.OBJECT_TYPE, false);

        // Create schema if it doesn't exist
        var schema = form.GetFormSchema(SchemaName);
        if (schema == null)
        {
            form.ItemsList.Add(new FormSchemaInfo()
            {
                Name = SchemaName,
                Description = SchemaDescription,
                Caption = SchemaCaption,
                Guid = SchemaGuid
            });
        }
    }
}
