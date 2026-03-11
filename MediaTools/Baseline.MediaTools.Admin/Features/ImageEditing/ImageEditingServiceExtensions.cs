using Baseline.MediaTools.Admin.Features.ImageEditing;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ImageEditingServiceExtensions
{
    /// <summary>
    /// Registers image editing and AI generation services used by the ImageEditor admin form component.
    /// Call from <c>Program.cs</c> during service configuration.
    /// </summary>
    public static IServiceCollection AddImageEditing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IImageEditingService, ImageEditingService>();

        services.Configure<GeminiImageOptions>(
            configuration.GetSection(GeminiImageOptions.SectionName));
        services.AddHttpClient("GeminiImage");
        services.AddSingleton<IImageGenerationService, GeminiImageGenerationService>();

        return services;
    }
}
