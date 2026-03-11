using Microsoft.Extensions.Logging;

namespace Baseline.SEO;

/// <summary>
/// No-op default implementation of <see cref="IAnswerEngineService"/>.
/// Returns empty/neutral results. Override via DI to provide AI-backed extraction.
/// </summary>
public class NoOpAnswerEngineService(ILogger<NoOpAnswerEngineService> logger)
    : IAnswerEngineService
{
    /// <inheritdoc/>
    public Task<FAQPage> ExtractFAQsAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: ExtractFAQsAsync called — returning empty");
        return Task.FromResult(new FAQPage());
    }

    /// <inheritdoc/>
    public Task<HowTo> GenerateHowToAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: GenerateHowToAsync called — returning empty");
        return Task.FromResult(new HowTo { Name = string.Empty });
    }

    /// <inheritdoc/>
    public Task<Speakable> CreateSpeakableAsync(string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: CreateSpeakableAsync called — returning empty");
        return Task.FromResult(new Speakable());
    }

    /// <inheritdoc/>
    public Task<SnippetPassage> GenerateSnippetPassageAsync(
        string question, string content, SnippetType snippetType = SnippetType.Paragraph,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: GenerateSnippetPassageAsync called — returning empty");
        return Task.FromResult(new SnippetPassage { Question = question, Passage = string.Empty });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<QAPair>> ExtractQuestionsAsync(
        string content, int maxQuestions = 10, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: ExtractQuestionsAsync called — returning empty");
        return Task.FromResult(Enumerable.Empty<QAPair>());
    }

    /// <inheritdoc/>
    public Task<DirectAnswer?> GetDirectAnswerAsync(
        string question, string content, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: GetDirectAnswerAsync called — returning null");
        return Task.FromResult<DirectAnswer?>(null);
    }

    /// <inheritdoc/>
    public Task<StructuredDataCollection> GenerateStructuredDataAsync(
        string content, string? contentType = null, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("NoOp AnswerEngine: GenerateStructuredDataAsync called — returning empty");
        return Task.FromResult(new StructuredDataCollection());
    }
}
