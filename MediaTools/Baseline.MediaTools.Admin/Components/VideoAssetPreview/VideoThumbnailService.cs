using Microsoft.AspNetCore.Hosting;

namespace Baseline.MediaTools.Admin.Components.VideoAssetPreview;

/// <summary>
/// Stores and retrieves auto-generated video thumbnail images on disk.
/// Thumbnails are saved as JPEG files under App_Data/videothumbnails/{contentItemGuid}.jpg.
/// </summary>
public sealed class VideoThumbnailService
{
    private readonly string _basePath;

    public VideoThumbnailService(IWebHostEnvironment env)
    {
        _basePath = System.IO.Path.Combine(env.ContentRootPath, "App_Data", "videothumbnails");
        System.IO.Directory.CreateDirectory(_basePath);
    }

    public string GetThumbnailPath(Guid contentItemGuid)
        => System.IO.Path.Combine(_basePath, $"{contentItemGuid}.jpg");

    public bool ThumbnailExists(Guid contentItemGuid)
        => System.IO.File.Exists(GetThumbnailPath(contentItemGuid));

    public void SaveThumbnail(Guid contentItemGuid, byte[] jpegData)
    {
        var path = GetThumbnailPath(contentItemGuid);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllBytes(path, jpegData);
    }

    public byte[]? GetThumbnail(Guid contentItemGuid)
    {
        var path = GetThumbnailPath(contentItemGuid);
        return System.IO.File.Exists(path) ? System.IO.File.ReadAllBytes(path) : null;
    }
}
