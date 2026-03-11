using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baseline.MediaTools.Admin.Features.ImageEditing;

/// <summary>
/// Google Gemini image generation service. Supports two API styles:
/// <list type="bullet">
///   <item><b>Gemini models</b> (gemini-2.5-flash-image, gemini-3-pro-image-preview) —
///   use the <c>generateContent</c> endpoint.</item>
///   <item><b>Imagen models</b> (imagen-4.0-generate-001, etc.) —
///   use the <c>predict</c> endpoint.</item>
/// </list>
/// </summary>
public class GeminiImageGenerationService(
    IHttpClientFactory httpClientFactory,
    IOptions<GeminiImageOptions> options,
    ILogger<GeminiImageGenerationService> logger) : IImageGenerationService
{
    private readonly GeminiImageOptions config = options.Value;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool IsAvailable => !string.IsNullOrWhiteSpace(config.ApiKey);

    public async Task<ImageGenerationResult> GenerateAsync(
        ImageGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "Gemini API key not configured. Set GeminiImageGeneration:ApiKey in appsettings.");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentException("Prompt is required.", nameof(request));
        }

        bool isGeminiModel = config.Model.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase);

        return isGeminiModel
            ? await GenerateViaGemini(request, cancellationToken)
            : await GenerateViaImagen(request, cancellationToken);
    }

    private async Task<ImageGenerationResult> GenerateViaGemini(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("GeminiImage");
        var url = $"{config.BaseUrl}/models/{config.Model}:generateContent?key={config.ApiKey}";

        var promptText = request.Prompt;
        if (!string.IsNullOrWhiteSpace(request.NegativePrompt))
        {
            promptText += $"\n\nAvoid: {request.NegativePrompt}";
        }

        if (request.AspectRatio is not null and not "1:1")
        {
            promptText += $"\n\nAspect ratio: {request.AspectRatio}";
        }

        var parts = new List<GeminiPart>();

        if (request.ReferenceImageBytes is { Length: > 0 })
        {
            parts.Add(new GeminiPart
            {
                InlineData = new GeminiInlineData
                {
                    MimeType = request.ReferenceImageMimeType ?? "image/png",
                    Data = Convert.ToBase64String(request.ReferenceImageBytes)
                }
            });
            promptText = $"Using the provided reference image as a style/composition guide, generate: {promptText}";
        }

        parts.Add(new GeminiPart { Text = promptText });

        var body = new GeminiRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts = parts
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                ResponseModalities = ["IMAGE", "TEXT"],
                MaxOutputTokens = 4096
            }
        };

        var json = JsonSerializer.Serialize(body, JsonOptions);

        logger.LogInformation(
            "Generating image via Gemini ({Model}): prompt=\"{Prompt}\"",
            config.Model, request.Prompt[..Math.Min(80, request.Prompt.Length)]);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Gemini API error {Status}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Gemini API returned {(int)response.StatusCode}: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<GeminiResponse>(responseJson, JsonOptions);

        var imagePart = result?.Candidates?
            .SelectMany(c => c.Content?.Parts ?? [])
            .FirstOrDefault(p => p.InlineData?.MimeType?.StartsWith("image/") == true)
            ?? throw new InvalidOperationException(
                "No image returned by Gemini API. The model may have refused the prompt.");

        var imageBytes = Convert.FromBase64String(imagePart.InlineData!.Data!);

        return BuildResult(imageBytes, request.Prompt, cancellationToken);
    }

    private async Task<ImageGenerationResult> GenerateViaImagen(
        ImageGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("GeminiImage");
        var url = $"{config.BaseUrl}/models/{config.Model}:predict?key={config.ApiKey}";

        var body = BuildImagenBody(request);
        var json = JsonSerializer.Serialize(body, JsonOptions);

        logger.LogInformation(
            "Generating image via Imagen ({Model}): prompt=\"{Prompt}\", aspect={Aspect}",
            config.Model, request.Prompt[..Math.Min(80, request.Prompt.Length)], request.AspectRatio);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Imagen API error {Status}: {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException(
                $"Imagen API returned {(int)response.StatusCode}: {errorBody}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<ImagenPredictResponse>(responseJson, JsonOptions);

        var prediction = result?.Predictions?.FirstOrDefault()
            ?? throw new InvalidOperationException("No image returned by Imagen API.");

        var imageBytes = Convert.FromBase64String(prediction.BytesBase64Encoded);

        return BuildResult(imageBytes, request.Prompt, cancellationToken);
    }

    private ImageGenerationResult BuildResult(
        byte[] imageBytes, string prompt, CancellationToken ct)
    {
        int width = 0, height = 0;
        try
        {
            using var ms = new MemoryStream(imageBytes);
            using var img = SixLabors.ImageSharp.Image.Load(ms);
            width = img.Width;
            height = img.Height;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read dimensions from generated image");
        }

        logger.LogInformation("Image generated: {Width}x{Height}, {Size} bytes",
            width, height, imageBytes.Length);

        return new ImageGenerationResult
        {
            ImageBytes = imageBytes,
            Width = width,
            Height = height,
            Prompt = prompt
        };
    }

    private static ImagenPredictRequest BuildImagenBody(ImageGenerationRequest request)
    {
        var parameters = new ImagenParameters
        {
            SampleCount = Math.Clamp(request.NumImages, 1, 4),
            AspectRatio = request.AspectRatio switch
            {
                "1:1" or "3:4" or "4:3" or "9:16" or "16:9" => request.AspectRatio,
                _ => "1:1"
            }
        };

        if (!string.IsNullOrWhiteSpace(request.NegativePrompt))
        {
            parameters.NegativePrompt = request.NegativePrompt;
        }

        return new ImagenPredictRequest
        {
            Instances = [new ImagenInstance { Prompt = request.Prompt }],
            Parameters = parameters
        };
    }

    // ─── Gemini generateContent DTOs ────────────────────────────────────────

    private class GeminiRequest
    {
        public List<GeminiContent> Contents { get; set; } = [];
        public GeminiGenerationConfig? GenerationConfig { get; set; }
    }

    private class GeminiContent
    {
        public List<GeminiPart> Parts { get; set; } = [];
    }

    private class GeminiPart
    {
        public string? Text { get; set; }
        public GeminiInlineData? InlineData { get; set; }
    }

    private class GeminiInlineData
    {
        public string? MimeType { get; set; }
        public string? Data { get; set; }
    }

    private class GeminiGenerationConfig
    {
        public List<string> ResponseModalities { get; set; } = [];
        public int? MaxOutputTokens { get; set; }
    }

    private class GeminiResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    // ─── Imagen predict DTOs ────────────────────────────────────────────────

    private class ImagenPredictRequest
    {
        public List<ImagenInstance> Instances { get; set; } = [];
        public ImagenParameters Parameters { get; set; } = new();
    }

    private class ImagenInstance
    {
        public string Prompt { get; set; } = "";
    }

    private class ImagenParameters
    {
        public int SampleCount { get; set; } = 1;
        public string AspectRatio { get; set; } = "1:1";
        public string? NegativePrompt { get; set; }
    }

    private class ImagenPredictResponse
    {
        public List<ImagenPrediction>? Predictions { get; set; }
    }

    private class ImagenPrediction
    {
        public string BytesBase64Encoded { get; set; } = "";
        public string MimeType { get; set; } = "image/png";
    }
}
