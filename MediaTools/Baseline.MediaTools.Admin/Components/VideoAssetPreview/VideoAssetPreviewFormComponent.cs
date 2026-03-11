using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Helpers;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

[assembly: RegisterFormComponent(
    identifier: Baseline.MediaTools.Admin.Components.VideoAssetPreview.VideoAssetPreviewFormComponent.IDENTIFIER,
    componentType: typeof(Baseline.MediaTools.Admin.Components.VideoAssetPreview.VideoAssetPreviewFormComponent),
    name: "Video Asset Preview")]

namespace Baseline.MediaTools.Admin.Components.VideoAssetPreview;

/// <summary>
/// Custom form component for video assets. Chunked upload via UploadChunk/CompleteUpload/RemoveUploadedFile
/// commands with HTML5 video player preview on the client.
/// </summary>
[ComponentAttribute(typeof(VideoAssetPreviewComponentAttribute))]
public class VideoAssetPreviewFormComponent : FormComponent<VideoAssetPreviewClientProperties, ContentItemAssetMetadata>
{
    public const string IDENTIFIER = "Baseline.MediaTools.VideoAssetPreview";

    public override string ClientComponentName => "@baseline/media-tools/VideoAssetPreview";

    private static readonly string[] DefaultAllowedExtensions = ["mp4", "webm", "mov", "avi", "mkv"];
    private const int DefaultChunkSize = 4 * 1024 * 1024;

    private readonly IUploaderService _uploaderService;
    private readonly IContentItemAssetPathProvider _pathProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContentItemAssetUrlProvider _assetUrlProvider;
    private readonly IInfoProvider<ContentItemInfo> _contentItemInfoProvider;
    private readonly VideoThumbnailService _thumbnailService;

    public VideoAssetPreviewFormComponent(
        IUploaderService uploaderService,
        IContentItemAssetPathProvider pathProvider,
        IHttpContextAccessor httpContextAccessor,
        IContentItemAssetUrlProvider assetUrlProvider,
        IInfoProvider<ContentItemInfo> contentItemInfoProvider,
        VideoThumbnailService thumbnailService)
    {
        _uploaderService = uploaderService;
        _pathProvider = pathProvider;
        _httpContextAccessor = httpContextAccessor;
        _assetUrlProvider = assetUrlProvider;
        _contentItemInfoProvider = contentItemInfoProvider;
        _thumbnailService = thumbnailService;
    }

    private string GetTemporaryAssetUrl(Guid fileIdentifier, string fileName)
    {
        var assetPath = _assetUrlProvider.GetForTemporary(fileIdentifier, fileName);
        var pathBase = _httpContextAccessor.HttpContext?.Request.PathBase ?? PathString.Empty;
        var relativePath = $"/admin/api/{assetPath.RelativePath}";
        return UriHelper.BuildRelative(pathBase, relativePath, new QueryString(assetPath.QueryString));
    }

    public override ContentItemAssetMetadata GetValue()
    {
        var value = base.GetValue();
        if (value is null)
        {
            return value!;
        }

        var tempSource = new ContentItemAssetUploadedFileSource(
            _pathProvider.GetTempFileLocation(value));

        return new ContentItemAssetMetadataWithSource(tempSource, value);
    }

    protected override async Task ConfigureClientProperties(VideoAssetPreviewClientProperties clientProperties)
    {
        await base.ConfigureClientProperties(clientProperties);

        clientProperties.AllowedExtensions = DefaultAllowedExtensions;
        clientProperties.ChunkSize = DefaultChunkSize;

        if (clientProperties.Value is not { } metadata || metadata.Identifier == Guid.Empty)
        {
            return;
        }

        var url = await ResolveAssetUrl(metadata);
        clientProperties.AssetMetadata = BuildClientItem(
            metadata.Identifier, metadata.Name, metadata.Extension, metadata.Size, url);
    }

    private async Task<string> ResolveAssetUrl(ContentItemAssetMetadata metadata)
    {
        var tempPath = _pathProvider.GetTempFileLocation(metadata);
        if (CMS.IO.File.Exists(tempPath))
        {
            return GetTemporaryAssetUrl(metadata.Identifier, metadata.Name);
        }

        if (FormContext is IContentItemFormContextBase context)
        {
            var contentItem = await _contentItemInfoProvider.GetAsync(context.ItemId);
            if (contentItem is not null)
            {
                var assetPath = _assetUrlProvider.Get(
                    contentItem.ContentItemGUID, Guid, context.LanguageName, metadata.Name);
                var pathBase = _httpContextAccessor.HttpContext?.Request.PathBase ?? PathString.Empty;
                var relativePath = $"/admin/api/{assetPath.RelativePath}";
                return UriHelper.BuildRelative(pathBase, relativePath, new QueryString(assetPath.QueryString));
            }
        }

        return string.Empty;
    }

    [FormComponentCommand]
    public async Task<VideoUploadChunkCommandResult> UploadChunk(VideoUploadChunkCommandArguments args, CancellationToken ct)
    {
        var identifier = string.IsNullOrEmpty(args.FileIdentifier)
            ? Guid.NewGuid().ToString()
            : args.FileIdentifier;

        var directory = _pathProvider.GetTempFileDirectory(Guid.Parse(identifier));

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

        return new VideoUploadChunkCommandResult
        {
            Identifier = identifier,
            Name = args.FileName,
            Size = args.FileSize
        };
    }

    [FormComponentCommand]
    public async Task<VideoCompleteUploadCommandResult> CompleteUpload(VideoCompleteUploadCommandArguments args, CancellationToken ct)
    {
        var identifier = Guid.Parse(args.FileIdentifier);
        var directory = _pathProvider.GetTempFileDirectory(identifier);
        var extension = System.IO.Path.GetExtension(args.FileName);
        var assembledFileName = $"{identifier}{extension}";

        await _uploaderService.CompleteUpload(new CompleteChunkUploadParameters
        {
            FileName = assembledFileName,
            Directory = directory
        }, ct);

        var url = GetTemporaryAssetUrl(identifier, assembledFileName);

        return new VideoCompleteUploadCommandResult
        {
            AssetMetadata = BuildClientItem(identifier, args.FileName, extension, args.FileSize, url)
        };
    }

    [FormComponentCommand]
    public async Task<SaveThumbnailResult> SaveThumbnail(SaveThumbnailArgs args, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(args.ThumbnailBase64))
            return new SaveThumbnailResult { Success = false };

        if (FormContext is not IContentItemFormContextBase context || context.ItemId <= 0)
            return new SaveThumbnailResult { Success = false };

        var contentItem = await _contentItemInfoProvider.GetAsync(context.ItemId, ct);
        if (contentItem is null)
            return new SaveThumbnailResult { Success = false };

        var data = Convert.FromBase64String(args.ThumbnailBase64);
        _thumbnailService.SaveThumbnail(contentItem.ContentItemGUID, data);

        return new SaveThumbnailResult { Success = true };
    }

    [FormComponentCommand]
    public Task RemoveUploadedFile(VideoRemoveUploadedFileCommandArguments args, CancellationToken ct)
        => Task.CompletedTask;

    private static ContentItemAssetMetadataClientItem BuildClientItem(
        Guid identifier, string name, string extension, long size, string url = "")
        => new()
        {
            Original = new ContentItemAssetMetadataBaseClientItem
            {
                Identifier = identifier,
                Name = name,
                Extension = extension,
                Size = size,
                IsImage = false,
                IsOptimizableImage = false,
                Url = url,
                LastModified = DateTime.UtcNow
            }
        };
}

public class VideoAssetPreviewClientProperties : FormComponentClientProperties<ContentItemAssetMetadata>
{
    public ContentItemAssetMetadataClientItem? AssetMetadata { get; set; }
    public IEnumerable<string> AllowedExtensions { get; set; } = [];
    public int ChunkSize { get; set; }
}

public class SaveThumbnailArgs
{
    public string ThumbnailBase64 { get; set; } = string.Empty;
}

public class SaveThumbnailResult
{
    public bool Success { get; set; }
}

public class VideoAssetPreviewComponentAttribute : FormComponentAttribute
{
    public string AllowedExtensions { get; set; } = "mp4;webm;mov;avi;mkv";
}

// ─── Video-specific chunked upload DTOs (avoid conflict w/ ImageEditing DTOs) ──

public class VideoUploadChunkCommandArguments
{
    public int ChunkId { get; set; }
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string? FileIdentifier { get; set; }
    public byte[]? ChunkData { get; set; }
}

public class VideoUploadChunkCommandResult
{
    public string? Identifier { get; set; }
    public string? Name { get; set; }
    public long Size { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VideoCompleteUploadCommandArguments
{
    public string FileName { get; set; } = "";
    public string FileIdentifier { get; set; } = "";
    public long FileSize { get; set; }
}

public class VideoCompleteUploadCommandResult
{
    public ContentItemAssetMetadataClientItem? AssetMetadata { get; set; }
}

public class VideoRemoveUploadedFileCommandArguments
{
    public ContentItemAssetMetadataClientItem? AssetMetadata { get; set; }
}
