using Baseline.MediaTools.Admin.Components.VideoAssetPreview;
using Kentico.Xperience.Admin.Base.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// DI registration for the video thumbnail preview system.
/// Call <see cref="AddVideoThumbnailPreview"/> in Program.cs AFTER AddKentico().
/// </summary>
public static class VideoThumbnailServiceExtensions
{
    public static IServiceCollection AddVideoThumbnailPreview(this IServiceCollection services)
    {
        services.AddSingleton<VideoThumbnailService>();
        services.AddSingleton<IContentItemAssetPreviewDataRetriever, VideoPreviewDataRetriever>();

        return services;
    }
}
