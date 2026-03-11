namespace Baseline.AI.Services;

/// <summary>
/// No-op implementation of <see cref="IAIProvider"/> for scenarios where AI provider is not configured.
/// Allows AI services to run without an AI provider dependency.
/// Throws NotImplementedException for operations that require an actual provider.
/// </summary>
internal sealed class NoOpAIProvider : IAIProvider
{
    public string ProviderName => "NoOp";

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "No AI provider is configured. " +
            "Please register an AI provider using builder.RegisterProvider<OpenAIProvider>() or similar in AddBaselineAI configuration.");

    public Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "No AI provider is configured. " +
            "Please register an AI provider using builder.RegisterProvider<OpenAIProvider>() or similar in AddBaselineAI configuration.");

    public Task<AIResponse> GenerateChatCompletionAsync(
        IEnumerable<AIChatMessage> messages,
        AICompletionOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotImplementedException(
            "No AI provider is configured. " +
            "Please register an AI provider using builder.RegisterProvider<OpenAIProvider>() or similar in AddBaselineAI configuration.");

    public async IAsyncEnumerable<AIStreamChunk> GenerateChatCompletionStreamingAsync(
        IEnumerable<AIChatMessage> messages,
        AICompletionOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "No AI provider is configured. " +
            "Please register an AI provider using builder.RegisterProvider<OpenAIProvider>() or similar in AddBaselineAI configuration.");
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162
    }
}
