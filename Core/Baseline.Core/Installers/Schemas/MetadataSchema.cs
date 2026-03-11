using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.FormEngine;

namespace Baseline.Core.Installers.Schemas;

/// <summary>
/// Defines the Base.Metadata reusable schema for page metadata (SEO, titles, descriptions).
/// </summary>
public static class MetadataSchema
{
    public const string SchemaName = "Base.Metadata";
    public const string SchemaCaption = "Page Metadata";
    public const string SchemaDescription = "Contains Meta Data for your page.";

    public static readonly Guid SchemaGuid = Guid.Parse("cbd68689-f238-4a7e-9437-ba5182822c70");

    // Field GUIDs
    public static readonly Guid PageNameFieldGuid = Guid.Parse("70dfb72a-d2a2-49df-84dc-61c0068163f9");
    public static readonly Guid TitleFieldGuid = Guid.Parse("1e716d33-9f3c-42b4-aa99-330f44976058");
    public static readonly Guid DescriptionFieldGuid = Guid.Parse("3788a858-7695-4837-9d1e-164d39eb0d06");
    public static readonly Guid KeywordsFieldGuid = Guid.Parse("e71ff512-61a8-4e85-9892-2d7ecba7f0ff");
    public static readonly Guid NoIndexFieldGuid = Guid.Parse("dfd21f8a-7c4d-4420-bedc-40c3163f151f");

    /// <summary>
    /// Creates or updates the Base.Metadata reusable schema and its fields.
    /// </summary>
    public static void Install()
    {
        var contentItemCommonData = DataClassInfoProvider.GetClasses()
            .WhereEquals(nameof(DataClassInfo.ClassName), ContentItemCommonDataInfo.OBJECT_TYPE)
            .FirstOrDefault() ?? throw new Exception("No Content Item Common Data Class Found!");

        var form = FormHelper.GetFormInfo(ContentItemCommonDataInfo.OBJECT_TYPE, false);

        // Create schema if it doesn't exist
        EnsureSchemaExists(form);

        // Add or update fields using fluent builder
        var builder = new FormFieldBuilder(form, SchemaGuid);

        builder
            .TextField(
                name: nameof(IBaseMetadata.MetaData_PageName),
                fieldGuid: PageNameFieldGuid,
                caption: "Page Name",
                description: "What gets displayed on Menus, Breadcrumbs, etc.",
                size: 200)
            .LongTextField(
                name: nameof(IBaseMetadata.MetaData_Title),
                fieldGuid: TitleFieldGuid,
                caption: "Page Title",
                description: "If empty, will default to the Page Name",
                componentName: "Kentico.Administration.TextInput")
            .LongTextField(
                name: nameof(IBaseMetadata.MetaData_Description),
                fieldGuid: DescriptionFieldGuid,
                caption: "Description")
            .LongTextField(
                name: nameof(IBaseMetadata.MetaData_Keywords),
                fieldGuid: KeywordsFieldGuid,
                caption: "Keywords",
                componentName: "Kentico.Administration.TextInput")
            .BooleanField(
                name: nameof(IBaseMetadata.MetaData_NoIndex),
                fieldGuid: NoIndexFieldGuid,
                caption: "No Index",
                description: "Indicates that this page should not be indexed by search engines.");

        // Save changes
        contentItemCommonData.ClassFormDefinition = form.GetXmlDefinition();
        if (contentItemCommonData.HasChanged)
        {
            DataClassInfoProvider.SetDataClassInfo(contentItemCommonData);
        }
    }

    private static void EnsureSchemaExists(FormInfo form)
    {
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
