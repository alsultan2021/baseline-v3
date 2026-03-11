using System.Linq;
using CMS.ContentEngine.Internal;
using CMS.FormEngine;
using Kentico.Xperience.Admin.Base.Components;
using Kentico.Xperience.Admin.Base.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Baseline.MediaTools.Admin.Components.VideoAssetPreview;

/// <summary>
/// Replaces the default <see cref="IContentItemAssetPreviewDataRetriever"/> so that
/// content items with a stored video thumbnail show the thumbnail image in the
/// Content Hub grid view instead of the generic play icon.
/// </summary>
public sealed class VideoPreviewDataRetriever : IContentItemAssetPreviewDataRetriever
{
    private readonly VideoThumbnailService _thumbnailService;
    private readonly IContentItemAssetFieldsProvider _fieldsProvider;
    private readonly IContentItemAssetUrlProvider _urlProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VideoPreviewDataRetriever(
        VideoThumbnailService thumbnailService,
        IContentItemAssetFieldsProvider fieldsProvider,
        IContentItemAssetUrlProvider urlProvider,
        IHttpContextAccessor httpContextAccessor)
    {
        _thumbnailService = thumbnailService;
        _fieldsProvider = fieldsProvider;
        _urlProvider = urlProvider;
        _httpContextAccessor = httpContextAccessor;
    }

    public ContentItemAssetPreviewData? Retrieve(
        int contentTypeId,
        Guid contentItemGuid,
        string contentLanguageName,
        bool hasImageAsset,
        bool isInLiveSiteChain)
    {
        if (_thumbnailService.ThumbnailExists(contentItemGuid))
        {
            return new ContentItemAssetPreviewData
            {
                AssetUrl = $"/api/video-thumbnail/{contentItemGuid}",
                IsImage = true,
                IsEligibleForDisplay = isInLiveSiteChain
            };
        }

        FormFieldInfo? field = _fieldsProvider.GetAssetFields(contentTypeId).FirstOrDefault();
        if (field is null)
        {
            return null;
        }

        ContentItemAssetUrl assetUrl = _urlProvider.Get(contentItemGuid, field.Guid, contentLanguageName);
        var pathBase = _httpContextAccessor.HttpContext?.Request.PathBase ?? PathString.Empty;
        var relativePath = $"/admin/api/{assetUrl.RelativePath}";
        var url = UriHelper.BuildRelative(pathBase, relativePath, new QueryString(assetUrl.QueryString));

        return new ContentItemAssetPreviewData
        {
            AssetUrl = url,
            IsImage = hasImageAsset,
            IsEligibleForDisplay = isInLiveSiteChain
        };
    }
}
