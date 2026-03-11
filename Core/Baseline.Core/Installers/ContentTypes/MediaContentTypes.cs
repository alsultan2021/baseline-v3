namespace Baseline.Core.Installers.ContentTypes;

/// <summary>
/// Predefined content type definitions for common media types.
/// Use these as examples or directly in your site installer.
/// </summary>
public static class MediaContentTypes
{
    /// <summary>
    /// GUIDs for the Generic.Image content type.
    /// </summary>
    public static class Image
    {
        public const string ClassName = "Generic.Image";
        public const string DisplayName = "Media File - Image";

        public static readonly Guid ClassGuid = Guid.Parse("20071D4B-400C-4D91-945D-D05A200A67B2");
        public static readonly Guid ContentItemDataIdGuid = Guid.Parse("42383b40-69aa-4b31-a98e-0bba8d2d1e83");
        public static readonly Guid CommonDataIdGuid = Guid.Parse("96dbd301-7dd9-4125-aed0-2a860a012bc2");
        public static readonly Guid DataGuidGuid = Guid.Parse("4c341d79-baea-4edd-9ff9-652e6ff72e3c");
        public static readonly Guid TitleGuid = Guid.Parse("2755f9e4-361c-43c6-9e7f-db2977aeb2f0");
        public static readonly Guid AssetGuid = Guid.Parse("a28a7e27-4593-40cf-8266-2ae4101723f5");

        /// <summary>
        /// Creates the Generic.Image content type builder.
        /// </summary>
        /// <param name="allowedExtensions">Allowed image file extensions (default: jpg;jpeg;png;gif;webp;svg)</param>
        public static ContentTypeBuilder CreateBuilder(string allowedExtensions = "jpg;jpeg;png;gif;webp;svg")
        {
            return new ContentTypeBuilder(ClassName, DisplayName, ClassGuid)
                .WithIcon("xp-picture")
                .WithShortName("GenericImage")
                .AsReusable()
                .WithUrl()
                .WithHasImageSchema()
                .WithTextField("ImageTitle", TitleGuid, "Title", required: true, size: 100)
                .WithAssetField("ImageAsset", AssetGuid, "Image File",
                    allowedExtensions: allowedExtensions,
                    required: true,
                    requiredErrorMessage: "Must add an image file");
        }
    }

    /// <summary>
    /// GUIDs for the Generic.Audio content type.
    /// </summary>
    public static class Audio
    {
        public const string ClassName = "Generic.Audio";
        public const string DisplayName = "Media File - Audio";

        public static readonly Guid ClassGuid = Guid.Parse("A9E1EE4F-9ECB-47B0-9ED9-7E1C6BAB9523");
        public static readonly Guid ContentItemDataIdGuid = Guid.Parse("8dc1c5d8-34ea-40c8-a94d-f2d26bedaaf8");
        public static readonly Guid TitleGuid = Guid.Parse("d4a3e5f6-7b8c-9d0e-1f2a-3b4c5d6e7f80");
        public static readonly Guid DescriptionGuid = Guid.Parse("e5b4f6a7-8c9d-0e1f-2a3b-4c5d6e7f8091");
        public static readonly Guid AssetGuid = Guid.Parse("f6c5a7b8-9d0e-1f2a-3b4c-5d6e7f809102");
        public static readonly Guid TranscriptGuid = Guid.Parse("07d6b8c9-0e1f-2a3b-4c5d-6e7f80910213");

        /// <summary>
        /// Creates the Generic.Audio content type builder.
        /// </summary>
        /// <param name="allowedExtensions">Allowed audio file extensions (default: mp3;wav;ogg;m4a)</param>
        public static ContentTypeBuilder CreateBuilder(string allowedExtensions = "mp3;wav;ogg;m4a")
        {
            return new ContentTypeBuilder(ClassName, DisplayName, ClassGuid)
                .WithIcon("xp-bubble")
                .WithShortName("GenericAudio")
                .AsReusable()
                .WithUrl()
                .WithTextField("AudioTitle", TitleGuid, "Title", required: true, size: 100)
                .WithLongTextField("AudioDescription", DescriptionGuid, "Description",
                    componentName: "Kentico.Administration.TextInput")
                .WithAssetField("AudioFile", AssetGuid, "Audio File",
                    allowedExtensions: allowedExtensions,
                    required: true,
                    requiredErrorMessage: "Must add an audio file")
                .WithLongTextField("AudioTranscript", TranscriptGuid, "Transcript",
                    componentName: "Kentico.Administration.TextArea",
                    minRows: 3,
                    maxRows: 5);
        }
    }

    /// <summary>
    /// GUIDs for the Generic.Video content type.
    /// </summary>
    public static class Video
    {
        public const string ClassName = "Generic.Video";
        public const string DisplayName = "Media File - Video";

        public static readonly Guid ClassGuid = Guid.Parse("A10DD00E-6CA2-4A15-9E34-3FACDBC73B8E");
        public static readonly Guid ContentItemDataIdGuid = Guid.Parse("ef8a9b0c-1d2e-3f4a-5b6c-7d8e9f0a1b2c");
        public static readonly Guid TitleGuid = Guid.Parse("084941bb-aa75-4d40-969e-c7f7d0fa6b68");
        public static readonly Guid DescriptionGuid = Guid.Parse("ec672c1a-92cd-492c-8a37-e65ae32982c3");
        public static readonly Guid AssetGuid = Guid.Parse("05b5348d-16e4-439d-b620-75c40b4d7eb5");
        public static readonly Guid TranscriptGuid = Guid.Parse("f7c0c3d3-a3e3-4af4-a6b6-af298d6f90ec");

        /// <summary>
        /// Creates the Generic.Video content type builder.
        /// </summary>
        /// <param name="allowedExtensions">Allowed video file extensions (default: mp4;webm;mov;avi)</param>
        public static ContentTypeBuilder CreateBuilder(string allowedExtensions = "mp4;webm;mov;avi")
        {
            return new ContentTypeBuilder(ClassName, DisplayName, ClassGuid)
                .WithIcon("xp-play")
                .WithShortName("GenericVideo")
                .AsReusable()
                .WithUrl()
                .WithTextField("VideoTitle", TitleGuid, "Title", required: true, size: 100)
                .WithLongTextField("VideoDescription", DescriptionGuid, "Description",
                    componentName: "Kentico.Administration.TextInput")
                .WithAssetField("VideoFile", AssetGuid, "Video File",
                    allowedExtensions: allowedExtensions,
                    required: true,
                    requiredErrorMessage: "Must add a video file")
                .WithLongTextField("VideoTranscript", TranscriptGuid, "Transcript",
                    componentName: "Kentico.Administration.TextArea",
                    minRows: 3,
                    maxRows: 5);
        }
    }
}
