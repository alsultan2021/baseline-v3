using Baseline.MediaTools.Admin.Components.VideoAssetPreview;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Baseline.MediaTools.Admin.Components.VideoAssetPreview;

/// <summary>
/// Maps the GET /api/video-thumbnail/{contentItemGuid} endpoint that serves
/// auto-generated video thumbnails as JPEG images for Content Hub list preview.
/// </summary>
public static class VideoThumbnailEndpoints
{
    public static IEndpointRouteBuilder MapVideoThumbnailEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/video-thumbnail/{contentItemGuid:guid}", async (Guid contentItemGuid, VideoThumbnailService service) =>
        {
            var data = service.GetThumbnail(contentItemGuid);
            if (data is null)
            {
                return Results.NotFound();
            }

            return Results.File(data, "image/jpeg", enableRangeProcessing: true);
        });

        return endpoints;
    }
}
