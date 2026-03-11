using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: RegisterFormComponent(
    identifier: Baseline.MediaTools.Admin.Features.ImageEditing.ImageEditorFormComponent.IDENTIFIER,
    componentType: typeof(Baseline.MediaTools.Admin.Features.ImageEditing.ImageEditorFormComponent),
    name: "Image Editor")]

namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Custom form component for image assets with built-in editing (crop, rotate, flip,
/// filters, format conversion) and optional AI image generation via Gemini.
/// </summary>
[ComponentAttribute(typeof(ImageEditorComponentAttribute))]
public class ImageEditorFormComponent
    : FormComponent<ImageEditorClientProperties, ContentItemAssetMetadata>
{
    public const string IDENTIFIER = "Baseline.MediaTools.ImageEditor";

    public override string ClientComponentName =>
        "@baseline/media-tools/ImageEditorFormComponent";

    private static readonly string[] DefaultAllowedExtensions =
        ["png", "jpg", "jpeg", "gif", "webp", "bmp", "svg"];
    private const int DefaultChunkSize = 4 * 1024 * 1024;

    private readonly IUploaderService _uploaderService;
    private readonly IContentItemAssetPathProvider _pathProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContentItemAssetUrlProvider _assetUrlProvider;
    private readonly IInfoProvider<ContentItemInfo> _contentItemInfoProvider;
    private readonly IImageEditingService _imageEditingService;
    private readonly IImageGenerationService _imageGenerationService;

    public ImageEditorFormComponent(
        IUploaderService uploaderService,
        IContentItemAssetPathProvider pathProvider,
        IHttpContextAccessor httpContextAccessor,
        IContentItemAssetUrlProvider assetUrlProvider,
        IInfoProvider<ContentItemInfo> contentItemInfoProvider,
        IImageEditingService imageEditingService,
        IImageGenerationService imageGenerationService)
    {
        _uploaderService = uploaderService;
        _pathProvider = pathProvider;
        _httpContextAccessor = httpContextAccessor;
        _assetUrlProvider = assetUrlProvider;
        _contentItemInfoProvider = contentItemInfoProvider;
        _imageEditingService = imageEditingService;
        _imageGenerationService = imageGenerationService;
    }

    // ── Asset persistence pipeline ─────────────────────────────────────────────

    private string GetTemporaryAssetUrl(Guid fileIdentifier, string fileName)
    {
        var assetPath = _assetUrlProvider.GetForTemporary(fileIdentifier, fileName);
        var pathBase = _httpContextAccessor.HttpContext?.Request.PathBase ?? PathString.Empty;
        var relativePath = $"/admin/api/{assetPath.RelativePath}";
        return UriHelper.BuildRelative(pathBase, relativePath, new QueryString(assetPath.QueryString));
    }

    /// <summary>
    /// Wraps the value in <see cref="ContentItemAssetMetadataWithSource"/> so the
    /// Kentico persistence pipeline moves the uploaded temp file to permanent storage.
    /// </summary>
    public override ContentItemAssetMetadata GetValue()
    {
        var value = base.GetValue();
        if (value is null)
            return value!;

        var tempSource = new ContentItemAssetUploadedFileSource(
            _pathProvider.GetTempFileLocation(value));

        return new ContentItemAssetMetadataWithSource(tempSource, value);
    }

    protected override async Task ConfigureClientProperties(
        ImageEditorClientProperties clientProperties)
    {
        await base.ConfigureClientProperties(clientProperties);

        clientProperties.AllowedExtensions = DefaultAllowedExtensions;
        clientProperties.ChunkSize = DefaultChunkSize;
        clientProperties.AiGenerationAvailable = _imageGenerationService.IsAvailable;

        if (clientProperties.Value is not { } metadata || metadata.Identifier == Guid.Empty)
            return;

        var url = await ResolveAssetUrl(metadata);
        clientProperties.AssetMetadata = BuildClientItem(
            metadata.Identifier, metadata.Name, metadata.Extension, metadata.Size, url);
    }

    private async Task<string> ResolveAssetUrl(ContentItemAssetMetadata metadata)
    {
        var tempPath = _pathProvider.GetTempFileLocation(metadata);
        if (CMS.IO.File.Exists(tempPath))
            return GetTemporaryAssetUrl(metadata.Identifier, metadata.Name);

        if (FormContext is IContentItemFormContextBase context)
        {
            var contentItem = await _contentItemInfoProvider.GetAsync(context.ItemId);
            if (contentItem is not null)
            {
                var assetPath = _assetUrlProvider.Get(
                    contentItem.ContentItemGUID, Guid, context.LanguageName, metadata.Name);
                var pathBase = _httpContextAccessor.HttpContext?.Request.PathBase ?? PathString.Empty;
                var relativePath = $"/admin/api/{assetPath.RelativePath}";
                return UriHelper.BuildRelative(pathBase, relativePath,
                    new QueryString(assetPath.QueryString));
            }
        }
        return string.Empty;
    }

    // ── UploadChunk ────────────────────────────────────────────────────────────

    [FormComponentCommand]
    public async Task<UploadChunkCommandResult> UploadChunk(
        UploadChunkCommandArguments args, CancellationToken ct)
    {
        var identifier = string.IsNullOrEmpty(args.FileIdentifier)
            ? System.Guid.NewGuid().ToString()
            : args.FileIdentifier;

        var directory = _pathProvider.GetTempFileDirectory(System.Guid.Parse(identifier));

        var chunkData = args.ChunkData;
        if (chunkData is null)
        {
            var formFile = _httpContextAccessor.HttpContext?.Request.Form.Files.FirstOrDefault();
            if (formFile is not null)
            {
                using var ms = new MemoryStream((int)formFile.Length);
                await formFile.CopyToAsync(ms, ct);
                chunkData = ms.ToArray();
            }
        }

        var storedFileName = $"{identifier}{System.IO.Path.GetExtension(args.FileName)}";

        await _uploaderService.UploadChunk(new ChunkUploadParameters
        {
            ChunkId = args.ChunkId,
            FileName = storedFileName,
            Data = chunkData,
            Directory = directory,
            FileSize = args.FileSize
        }, ct);

        return new UploadChunkCommandResult
        {
            Identifier = identifier,
            Name = args.FileName,
            Size = args.FileSize
        };
    }

    // ── CompleteUpload ─────────────────────────────────────────────────────────

    [FormComponentCommand]
    public async Task<CompleteUploadCommandResult> CompleteUpload(
        CompleteUploadCommandArguments args, CancellationToken ct)
    {
        var identifier = System.Guid.Parse(args.FileIdentifier);
        var directory = _pathProvider.GetTempFileDirectory(identifier);
        var extension = System.IO.Path.GetExtension(args.FileName);
        var assembledFileName = $"{identifier}{extension}";

        await _uploaderService.CompleteUpload(new CompleteChunkUploadParameters
        {
            FileName = assembledFileName,
            Directory = directory
        }, ct);

        var url = GetTemporaryAssetUrl(identifier, assembledFileName);

        return new CompleteUploadCommandResult
        {
            AssetMetadata = BuildClientItem(
                identifier, args.FileName, extension, args.FileSize, url)
        };
    }

    // ── RemoveUploadedFile ─────────────────────────────────────────────────────

    [FormComponentCommand]
    public Task RemoveUploadedFile(RemoveUploadedFileCommandArguments args, CancellationToken ct)
        => Task.CompletedTask;

    // ── ApplyEdits ─────────────────────────────────────────────────────────────

    [FormComponentCommand]
    public async Task<ApplyEditsCommandResult> ApplyEdits(
        ApplyEditsCommandArguments args, CancellationToken ct)
    {
        try
        {
            var identifier = System.Guid.Parse(args.FileIdentifier);
            var directory = _pathProvider.GetTempFileDirectory(identifier);
            var extension = System.IO.Path.GetExtension(args.FileName);
            var tempFile = System.IO.Path.Combine(directory, $"{identifier}{extension}");

            if (!System.IO.File.Exists(tempFile))
                return new ApplyEditsCommandResult { ErrorMessage = "Source file not found." };

            var editParams = new ImageEditParameters
            {
                Crop = args.Parameters.Crop is { } c
                    ? new CropRegion(c.X, c.Y, c.Width, c.Height)
                    : null,
                RotationDegrees = args.Parameters.RotationDegrees,
                FlipHorizontal = args.Parameters.FlipHorizontal,
                FlipVertical = args.Parameters.FlipVertical,
                Brightness = args.Parameters.Brightness,
                Contrast = args.Parameters.Contrast,
                Saturation = args.Parameters.Saturation,
                FilterPreset = args.Parameters.FilterPreset,
                Resize = args.Parameters.Resize is { } r
                    ? new ResizeDimensions(r.Width, r.Height)
                    : null,
                OutputFormat = args.Parameters.OutputFormat,
                Quality = args.Parameters.Quality
            };

            byte[] processedBytes;
            using (var sourceStream = System.IO.File.OpenRead(tempFile))
            {
                processedBytes = await _imageEditingService.ApplyEdits(sourceStream, editParams, ct);
            }

            var outputExt = editParams.OutputFormat?.ToLowerInvariant() switch
            {
                "jpeg" or "jpg" => ".jpeg",
                "webp" => ".webp",
                _ => ".png"
            };

            var outputFileName = System.IO.Path.ChangeExtension(args.FileName, outputExt);
            var newTempFile = System.IO.Path.Combine(directory, $"{identifier}{outputExt}");

            await System.IO.File.WriteAllBytesAsync(newTempFile, processedBytes, ct);

            if (!string.Equals(tempFile, newTempFile, StringComparison.OrdinalIgnoreCase)
                && System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);

            var url = GetTemporaryAssetUrl(identifier, $"{identifier}{outputExt}");

            return new ApplyEditsCommandResult
            {
                AssetMetadata = BuildClientItem(
                    identifier, outputFileName, outputExt, processedBytes.Length, url)
            };
        }
        catch (Exception ex)
        {
            return new ApplyEditsCommandResult
            {
                ErrorMessage = ex.Message
            };
        }
    }

    // ── GenerateImage (AI) ─────────────────────────────────────────────────────

    [FormComponentCommand]
    public async Task<GenerateImageCommandResult> GenerateImage(
        GenerateImageCommandArguments args, CancellationToken ct)
    {
        try
        {
            if (!_imageGenerationService.IsAvailable)
                return new GenerateImageCommandResult
                {
                    ErrorMessage = "AI image generation is not configured."
                };

            byte[]? refBytes = null;
            if (!string.IsNullOrEmpty(args.ReferenceImageBase64))
                refBytes = Convert.FromBase64String(args.ReferenceImageBase64);

            var request = new ImageGenerationRequest
            {
                Prompt = args.Prompt,
                NegativePrompt = args.NegativePrompt,
                AspectRatio = args.AspectRatio,
                ReferenceImageBytes = refBytes,
                ReferenceImageMimeType = args.ReferenceImageMimeType
            };

            var result = await _imageGenerationService.GenerateAsync(request, ct);

            var identifier = System.Guid.NewGuid();
            var directory = _pathProvider.GetTempFileDirectory(identifier);
            System.IO.Directory.CreateDirectory(directory);

            var fileName = $"generated-{identifier:N}.png";
            var tempFile = System.IO.Path.Combine(directory, $"{identifier}.png");
            await System.IO.File.WriteAllBytesAsync(tempFile, result.ImageBytes, ct);

            var url = GetTemporaryAssetUrl(identifier, $"{identifier}.png");

            return new GenerateImageCommandResult
            {
                AssetMetadata = BuildClientItem(
                    identifier, fileName, ".png", result.ImageBytes.Length, url)
            };
        }
        catch (Exception ex)
        {
            return new GenerateImageCommandResult
            {
                ErrorMessage = ex.Message
            };
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static ImageEditorAssetClientItem BuildClientItem(
        Guid identifier, string name, string extension, long size, string url = "")
        => new()
        {
            Original = new ImageEditorAssetBaseClientItem
            {
                Identifier = identifier,
                Name = name,
                Extension = extension,
                Size = size,
                IsImage = true,
                Url = url,
                LastModified = DateTime.UtcNow
            }
        };
}

/// <summary>
/// Client properties sent to the ImageEditorFormComponent React component.
/// </summary>
public class ImageEditorClientProperties
    : FormComponentClientProperties<ContentItemAssetMetadata>
{
    public ImageEditorAssetClientItem? AssetMetadata { get; set; }
    public IEnumerable<string> AllowedExtensions { get; set; } = [];
    public int ChunkSize { get; set; }
    public bool AiGenerationAvailable { get; set; }
}

/// <summary>
/// Attribute for annotating content type fields to use the image editor.
/// </summary>
public class ImageEditorComponentAttribute : FormComponentAttribute
{
    public string AllowedExtensions { get; set; } = "png;jpg;jpeg;gif;webp;bmp;svg";
}
