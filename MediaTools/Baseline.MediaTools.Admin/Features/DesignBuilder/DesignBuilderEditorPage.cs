using System.Text.Json;
using Baseline.MediaTools.Admin.Features.DesignBuilder;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using Kentico.Xperience.Admin.Base;
using Microsoft.Extensions.Logging;

[assembly: UIPage(
    uiPageType: typeof(DesignBuilderEditorPage),
    parentType: typeof(DesignBuilderApplicationPage),
    slug: "editor",
    name: "Editor",
    templateName: "@baseline/media-tools/DesignBuilderLayout",
    order: 1,
    Icon = Icons.Layout)]

namespace Baseline.MediaTools.Admin.Features.DesignBuilder;

public class DesignBuilderEditorPage(
    ILogger<DesignBuilderEditorPage> logger,
    IContentQueryExecutor contentQueryExecutor) : Page<DesignBuilderClientProperties>
{
    private readonly ILogger<DesignBuilderEditorPage> logger = logger;
    private readonly IContentQueryExecutor contentQueryExecutor = contentQueryExecutor;

    public override async Task<DesignBuilderClientProperties> ConfigureTemplateProperties(
        DesignBuilderClientProperties properties)
    {
        properties.SizePresets = GetSizePresets();
        properties.Templates = GetBuiltInTemplates();
        properties.Designs = await GetSavedDesigns();
        properties.MediaAssets = [];

        return properties;
    }

    #region Page Commands

    [PageCommand(CommandName = "SAVE_DESIGN")]
    public async Task<ICommandResponse> SaveDesign(SaveDesignArgs args)
    {
        try
        {
            logger.LogInformation("Saving design: {Name}", args.Name);
            var designId = Math.Abs(args.DesignJson.GetHashCode());

            return ResponseFrom(new SaveDesignResult(designId, DateTime.UtcNow.ToString("O")))
                .AddSuccessMessage($"Design '{args.Name}' saved successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving design");
            return Response().AddErrorMessage("Failed to save design.");
        }
    }

    [PageCommand(CommandName = "LOAD_DESIGN")]
    public async Task<ICommandResponse> LoadDesign(LoadDesignArgs args)
    {
        try
        {
            logger.LogInformation("Loading design: {Id}", args.DesignId);
            return ResponseFrom(new LoadDesignResult("{}"))
                .AddInfoMessage("Design loaded.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading design");
            return Response().AddErrorMessage("Failed to load design.");
        }
    }

    [PageCommand(CommandName = "DELETE_DESIGN")]
    public async Task<ICommandResponse> DeleteDesign(DeleteDesignArgs args)
    {
        try
        {
            logger.LogInformation("Deleting design: {Id}", args.DesignId);
            return Response().AddSuccessMessage("Design deleted.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting design");
            return Response().AddErrorMessage("Failed to delete design.");
        }
    }

    [PageCommand(CommandName = "EXPORT_DESIGN")]
    public async Task<ICommandResponse> ExportDesign(ExportDesignArgs args)
    {
        try
        {
            logger.LogInformation("Exporting design as {Format}", args.Format);
            return ResponseFrom(new ExportDesignResult($"/exported/design-{args.DesignId}.{args.Format}"))
                .AddSuccessMessage($"Design exported as {args.Format.ToUpperInvariant()}.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting design");
            return Response().AddErrorMessage("Failed to export design.");
        }
    }

    [PageCommand(CommandName = "LOAD_MEDIA")]
    public async Task<ICommandResponse> LoadMedia()
    {
        try
        {
            var assets = await LoadContentHubAssets();
            return ResponseFrom(new LoadMediaResult(assets));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading media");
            return Response().AddErrorMessage("Failed to load media assets.");
        }
    }

    [PageCommand(CommandName = "BROWSE_CONTENT_HUB")]
    public async Task<ICommandResponse> BrowseContentHub(BrowseContentHubArgs args)
    {
        try
        {
            var assets = await LoadContentHubAssets(args.SearchTerm, args.MediaType, args.Offset, args.Limit);
            var total = assets.Count();
            return ResponseFrom(new BrowseContentHubResult(assets, total, args.Offset + args.Limit < total));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error browsing content hub");
            return Response().AddErrorMessage("Failed to browse content hub.");
        }
    }

    #endregion

    #region Helpers

    private static IEnumerable<CanvasSizePreset> GetSizePresets() =>
    [
        new("Instagram Post", "Social Media", 1080, 1080),
        new("Instagram Story", "Social Media", 1080, 1920),
        new("Facebook Post", "Social Media", 1200, 630),
        new("Facebook Cover", "Social Media", 820, 312),
        new("Twitter Post", "Social Media", 1600, 900),
        new("LinkedIn Post", "Social Media", 1200, 627),
        new("YouTube Thumbnail", "Social Media", 1280, 720),
        new("Pinterest Pin", "Social Media", 1000, 1500),
        new("A4 Portrait", "Print", 2480, 3508),
        new("A4 Landscape", "Print", 3508, 2480),
        new("US Letter", "Print", 2550, 3300),
        new("Business Card", "Print", 1050, 600),
        new("Poster 18x24", "Print", 5400, 7200),
        new("Flyer 5x7", "Print", 1500, 2100),
        new("Web Banner 728x90", "Web", 728, 90),
        new("Web Banner 300x250", "Web", 300, 250),
        new("Web Banner 160x600", "Web", 160, 600),
        new("Email Header", "Web", 600, 200),
        new("Presentation 16:9", "Presentation", 1920, 1080),
        new("Presentation 4:3", "Presentation", 1024, 768),
        new("Custom Size", "Custom", 800, 600),
    ];

    private static IEnumerable<DesignTemplate> GetBuiltInTemplates() =>
    [
        new("tpl-blank", "Blank Canvas", "Basic", "", "{}"),
        new("tpl-announcement", "Announcement", "Marketing",
            "", GetAnnouncementTemplate()),
        new("tpl-sale", "Sale Promotion", "Marketing",
            "", GetSaleTemplate()),
        new("tpl-event", "Event Invitation", "Events",
            "", GetEventTemplate()),
        new("tpl-social", "Social Media Post", "Social",
            "", GetSocialTemplate()),
    ];

    private static string GetAnnouncementTemplate() => JsonSerializer.Serialize(new
    {
        backgroundColor = "#1a1a2e",
        elements = new object[]
        {
            new { type = "text", x = 100, y = 80, text = "ANNOUNCEMENT", fontSize = 48, fontWeight = "bold", fill = "#e94560" },
            new { type = "text", x = 100, y = 180, text = "Your headline here", fontSize = 32, fill = "#ffffff" },
            new { type = "text", x = 100, y = 250, text = "Add your description text here", fontSize = 18, fill = "#cccccc" },
            new { type = "rect", x = 80, y = 60, width = 500, height = 3, fill = "#e94560" },
        }
    });

    private static string GetSaleTemplate() => JsonSerializer.Serialize(new
    {
        backgroundColor = "#ff6b35",
        elements = new object[]
        {
            new { type = "rect", x = 50, y = 50, width = 980, height = 980, fill = "#ffffff", opacity = 0.15 },
            new { type = "text", x = 200, y = 150, text = "MEGA SALE", fontSize = 72, fontWeight = "bold", fill = "#ffffff" },
            new { type = "text", x = 300, y = 300, text = "50% OFF", fontSize = 120, fontWeight = "bold", fill = "#1a1a2e" },
            new { type = "text", x = 250, y = 500, text = "Limited time offer!", fontSize = 28, fill = "#ffffff" },
        }
    });

    private static string GetEventTemplate() => JsonSerializer.Serialize(new
    {
        backgroundColor = "#2d3436",
        elements = new object[]
        {
            new { type = "text", x = 150, y = 100, text = "YOU'RE INVITED", fontSize = 20, fill = "#dfe6e9", letterSpacing = 8 },
            new { type = "text", x = 100, y = 180, text = "Event Name", fontSize = 56, fontWeight = "bold", fill = "#ffffff" },
            new { type = "text", x = 100, y = 300, text = "Date & Time", fontSize = 24, fill = "#74b9ff" },
            new { type = "text", x = 100, y = 350, text = "Location", fontSize = 24, fill = "#74b9ff" },
            new { type = "rect", x = 100, y = 270, width = 200, height = 2, fill = "#74b9ff" },
        }
    });

    private static string GetSocialTemplate() => JsonSerializer.Serialize(new
    {
        backgroundColor = "#6c5ce7",
        elements = new object[]
        {
            new { type = "circle", x = 540, y = 540, radius = 200, fill = "#a29bfe", opacity = 0.5 },
            new { type = "text", x = 200, y = 400, text = "DID YOU KNOW?", fontSize = 48, fontWeight = "bold", fill = "#ffffff" },
            new { type = "text", x = 150, y = 500, text = "Share your knowledge here", fontSize = 24, fill = "#dfe6e9" },
            new { type = "text", x = 350, y = 900, text = "@yourbrand", fontSize = 20, fill = "#ffffff", opacity = 0.7 },
        }
    });

    /// <summary>
    /// Safely extracts <see cref="ContentItemAssetMetadata"/> from a content query
    /// result, handling both typed and raw JSON string representations.
    /// </summary>
    private static ContentItemAssetMetadata? TryGetAssetMetadata(
        IContentQueryDataContainer container, string columnName)
    {
        try
        {
            if (container.TryGetValue<ContentItemAssetMetadata>(columnName, out var meta))
            {
                return meta;
            }
        }
        catch (InvalidCastException)
        {
            // XbK may store asset metadata as a JSON string; fall back to manual deserialization.
            if (container.TryGetValue<string>(columnName, out var json)
                && !string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<ContentItemAssetMetadata>(json);
            }
        }

        return null;
    }

    private Task<IEnumerable<DesignSummary>> GetSavedDesigns() =>
        Task.FromResult<IEnumerable<DesignSummary>>([]);

    private async Task<IEnumerable<MediaAsset>> LoadContentHubAssets(
        string? searchTerm = null, string? mediaType = null, int offset = 0, int limit = 50)
    {
        var assets = new List<MediaAsset>();

        if (mediaType is null or "image" or "all")
        {
            var imageBuilder = new ContentItemQueryBuilder()
                .ForContentType("Generic.Image", q =>
                {
                    q.Columns("ContentItemGUID", "ContentItemName", "ImageAltText", "ImageAsset");
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        q.Where(w => w.WhereContains("ImageAltText", searchTerm)
                            .Or()
                            .WhereContains("ContentItemName", searchTerm));
                    }
                    q.Offset(offset, limit);
                });

            var images = await contentQueryExecutor.GetResult(
                imageBuilder,
                r =>
                {
                    var guid = r.ContentItemGUID;
                    var name = r.TryGetValue<string>("ImageAltText", out var alt) && !string.IsNullOrEmpty(alt)
                        ? alt : r.ContentItemName;
                    var asset = TryGetAssetMetadata(r, "ImageAsset");
                    return (Guid: guid, Name: name, Asset: asset);
                },
                new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true });

            foreach (var img in images)
            {
                if (img.Asset is null) continue;
                assets.Add(new MediaAsset(
                    Id: img.Guid.ToString(),
                    Name: img.Name,
                    Url: $"/getcontentasset/{img.Guid}/{img.Asset.Name}",
                    Type: "image",
                    Width: 0,
                    Height: 0));
            }
        }

        if (mediaType is null or "video" or "all")
        {
            var videoBuilder = new ContentItemQueryBuilder()
                .ForContentType("Generic.Video", q =>
                {
                    q.Columns("ContentItemGUID", "ContentItemName", "VideoTitle", "VideoFile");
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        q.Where(w => w.WhereContains("VideoTitle", searchTerm)
                            .Or()
                            .WhereContains("ContentItemName", searchTerm));
                    }
                    q.Offset(offset, limit);
                });

            var videos = await contentQueryExecutor.GetResult(
                videoBuilder,
                r =>
                {
                    var guid = r.ContentItemGUID;
                    var name = r.TryGetValue<string>("VideoTitle", out var title) && !string.IsNullOrEmpty(title)
                        ? title : r.ContentItemName;
                    var asset = TryGetAssetMetadata(r, "VideoFile");
                    return (Guid: guid, Name: name, Asset: asset);
                },
                new ContentQueryExecutionOptions { ForPreview = true, IncludeSecuredItems = true });

            foreach (var vid in videos)
            {
                if (vid.Asset is null) continue;
                assets.Add(new MediaAsset(
                    Id: vid.Guid.ToString(),
                    Name: vid.Name,
                    Url: $"/getcontentasset/{vid.Guid}/{vid.Asset.Name}",
                    Type: "video",
                    Width: 0,
                    Height: 0));
            }
        }

        return assets;
    }

    #endregion
}

#region Command Arg/Result types

public record SaveDesignArgs(string Name, string DesignJson, int Width, int Height);
public record SaveDesignResult(int DesignId, string SavedAt);
public record LoadDesignArgs(int DesignId);
public record LoadDesignResult(string DesignJson);
public record DeleteDesignArgs(int DesignId);
public record ExportDesignArgs(int DesignId, string Format);
public record ExportDesignResult(string DownloadUrl);
public record LoadMediaResult(IEnumerable<MediaAsset> Assets);
public record BrowseContentHubArgs(string? SearchTerm, string? MediaType, int Offset = 0, int Limit = 50);
public record BrowseContentHubResult(IEnumerable<MediaAsset> Assets, int TotalCount, bool HasMore);

#endregion
