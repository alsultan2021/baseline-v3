using Baseline.Core.Installers.Schemas;

namespace Baseline.Core.Installers.ContentTypes;

/// <summary>
/// Predefined content type definitions for common page types.
/// Use these as examples or directly in your site installer.
/// </summary>
public static class PageContentTypes
{
    /// <summary>
    /// GUIDs for the Generic.BasicPage content type.
    /// </summary>
    public static class BasicPage
    {
        public const string ClassName = "Generic.BasicPage";
        public const string DisplayName = "Basic Page";

        public static readonly Guid ClassGuid = Guid.Parse("9EF89D5B-C45B-4617-B16B-1BD2046F8A5E");
        public static readonly Guid ContentItemDataIdGuid = Guid.Parse("b964fdbd-b5d8-4fe0-8a41-7959bfc011dd");
        public static readonly Guid CommonDataIdGuid = Guid.Parse("2b7034d0-ad21-4de0-b021-e8a94d265e9e");
        public static readonly Guid DataGuidGuid = Guid.Parse("c0a1b2c3-d4e5-f6a7-b8c9-d0e1f2a3b4c5");

        /// <summary>
        /// Creates the Generic.BasicPage content type builder.
        /// Includes Base.Metadata and Base.Redirect schemas.
        /// </summary>
        /// <param name="includeMemberPermissions">Whether to include member permissions schema (requires XperienceCommunity.MemberRoles)</param>
        public static ContentTypeBuilder CreateBuilder(bool includeMemberPermissions = false)
        {
            var builder = new ContentTypeBuilder(ClassName, DisplayName, ClassGuid)
                .WithIcon("xp-doc-inverted")
                .WithShortName("GenericBasicPage")
                .AsWebsite()
                .WithUrl()
                .WithSchema(MetadataSchema.SchemaGuid)
                .WithSchema(RedirectSchema.SchemaGuid);

            // MemberPermissions schema GUID if using XperienceCommunity.MemberRoles
            if (includeMemberPermissions)
            {
                var memberPermSchema = SchemaHelper.GetSchemaGuid("XperienceCommunity.MemberPermissionConfiguration");
                if (memberPermSchema.HasValue)
                {
                    builder.WithSchema(memberPermSchema.Value);
                }
            }

            return builder;
        }
    }

    /// <summary>
    /// GUIDs for the Generic.HomePage content type.
    /// </summary>
    public static class HomePage
    {
        public const string ClassName = "Generic.HomePage";
        public const string DisplayName = "Home Page";

        public static readonly Guid ClassGuid = Guid.Parse("4D4C9A06-4D5E-43BE-A5CB-C4CB4678AB10");
        public static readonly Guid ContentItemDataIdGuid = Guid.Parse("a1b2c3d4-e5f6-a7b8-c9d0-e1f2a3b4c5d6");
        public static readonly Guid CommonDataIdGuid = Guid.Parse("b2c3d4e5-f6a7-b8c9-d0e1-f2a3b4c5d6e7");

        /// <summary>
        /// Creates the Generic.HomePage content type builder.
        /// Includes Base.Metadata and Base.Redirect schemas.
        /// </summary>
        public static ContentTypeBuilder CreateBuilder()
        {
            return new ContentTypeBuilder(ClassName, DisplayName, ClassGuid)
                .WithIcon("xp-home")
                .WithShortName("GenericHomePage")
                .AsWebsite()
                .WithUrl()
                .WithSchema(MetadataSchema.SchemaGuid)
                .WithSchema(RedirectSchema.SchemaGuid);
        }
    }

    /// <summary>
    /// GUIDs for a generic Article/Blog Post content type.
    /// </summary>
    public static class Article
    {
        public const string ClassName = "Generic.Article";
        public const string DisplayName = "Article";

        public static readonly Guid ClassGuid = Guid.Parse("C3D4E5F6-A7B8-C9D0-E1F2-A3B4C5D6E7F8");
        public static readonly Guid TitleGuid = Guid.Parse("d4e5f6a7-b8c9-d0e1-f2a3-b4c5d6e7f8a9");
        public static readonly Guid SummaryGuid = Guid.Parse("e5f6a7b8-c9d0-e1f2-a3b4-c5d6e7f8a9b0");
        public static readonly Guid ContentGuid = Guid.Parse("f6a7b8c9-d0e1-f2a3-b4c5-d6e7f8a9b0c1");
        public static readonly Guid PublishDateGuid = Guid.Parse("a7b8c9d0-e1f2-a3b4-c5d6-e7f8a9b0c1d2");
        public static readonly Guid FeaturedImageGuid = Guid.Parse("b8c9d0e1-f2a3-b4c5-d6e7-f8a9b0c1d2e3");

        /// <summary>
        /// Creates the Generic.Article content type builder.
        /// Includes common article fields and page schemas.
        /// </summary>
        public static ContentTypeBuilder CreateBuilder()
        {
            return new ContentTypeBuilder(ClassName, DisplayName, ClassGuid)
                .WithIcon("xp-newspaper")
                .WithShortName("GenericArticle")
                .AsWebsite()
                .WithUrl()
                .WithSchema(MetadataSchema.SchemaGuid)
                .WithSchema(RedirectSchema.SchemaGuid)
                .WithTextField("ArticleTitle", TitleGuid, "Title", required: true, size: 200)
                .WithLongTextField("ArticleSummary", SummaryGuid, "Summary",
                    description: "A short summary for listings and SEO")
                .WithRichTextField("ArticleContent", ContentGuid, "Content")
                .WithContentItemsField("ArticleFeaturedImage", FeaturedImageGuid, "Featured Image",
                    allowedContentTypes: "Generic.Image",
                    maximumItems: 1);
        }
    }
}
