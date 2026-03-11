using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.FormEngine;

namespace Baseline.Core.Installers.Schemas;

/// <summary>
/// Defines the Base.Redirect reusable schema for page redirection logic.
/// </summary>
public static class RedirectSchema
{
    public const string SchemaName = "Base.Redirect";
    public const string SchemaCaption = "Redirect";
    public const string SchemaDescription = "Allows for redirection logic.";

    public static readonly Guid SchemaGuid = Guid.Parse("95e3dcfc-b550-4000-b7bb-02cdd81b8161");

    // Field GUIDs
    public static readonly Guid RedirectionTypeFieldGuid = Guid.Parse("d72506d4-f799-40e0-8d17-62bd3ae49c00");
    public static readonly Guid InternalRedirectPageFieldGuid = Guid.Parse("f47ba3f8-1625-4897-aabf-a701a639c23b");
    public static readonly Guid ExternalRedirectUrlFieldGuid = Guid.Parse("3adfa20c-b5a3-4c4a-838e-54192743e35e");
    public static readonly Guid FirstChildClassNameFieldGuid = Guid.Parse("79a85749-bedf-4f85-8f95-b024762fa6e3");
    public static readonly Guid UsePermanentRedirectsFieldGuid = Guid.Parse("a808c854-d2ce-4fd0-9d32-d9a185f73d5f");

    /// <summary>
    /// Creates or updates the Base.Redirect reusable schema and its fields.
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
            .DropdownField(
                name: nameof(IBaseRedirect.PageRedirectionType),
                fieldGuid: RedirectionTypeFieldGuid,
                caption: "Redirection Type",
                size: 200)
            .WebPageSelectorField(
                name: nameof(IBaseRedirect.PageInternalRedirectPage),
                fieldGuid: InternalRedirectPageFieldGuid,
                caption: "Internal URL")
            .TextField(
                name: nameof(IBaseRedirect.PageExternalRedirectURL),
                fieldGuid: ExternalRedirectUrlFieldGuid,
                caption: "External URL",
                size: 512)
            .TextField(
                name: nameof(IBaseRedirect.PageFirstChildClassName),
                fieldGuid: FirstChildClassNameFieldGuid,
                caption: "First Child Page Type",
                size: 200)
            .BooleanField(
                name: nameof(IBaseRedirect.PageUsePermanentRedirects),
                fieldGuid: UsePermanentRedirectsFieldGuid,
                caption: "Use Permanent (301) Redirects?");

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
