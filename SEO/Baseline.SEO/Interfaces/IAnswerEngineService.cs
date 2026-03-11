namespace Baseline.SEO;

/// <summary>
/// Service for Answer Engine optimization.
/// Structures content for featured snippets, FAQ boxes, and AI-generated answers.
/// Models are in the Baseline.SEO.Models namespace (Models/ directory).
/// </summary>
/// <remarks>
/// <b>Core overlap:</b> Baseline.Core's <c>IStructuredDataService</c> provides
/// standard JSON-LD generation (Article, FAQ, Breadcrumb, etc.) from pre-structured data.
/// This interface adds <i>AI-powered extraction</i> — automatically detecting FAQs, HowTo
/// patterns, and speakable content from raw text. Use <see cref="ICoreSEOBridge"/> to
/// combine both modules at the site level.
/// </remarks>
public interface IAnswerEngineService
{
    /// <summary>
    /// Extracts FAQ content from text and generates FAQPage structured data.
    /// </summary>
    Task<FAQPage> ExtractFAQsAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates HowTo structured data from instructional content.
    /// </summary>
    Task<HowTo> GenerateHowToAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates speakable content annotations for voice search.
    /// </summary>
    Task<Speakable> CreateSpeakableAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a passage optimized for featured snippets.
    /// </summary>
    Task<SnippetPassage> GenerateSnippetPassageAsync(
        string question,
        string content,
        SnippetType snippetType = SnippetType.Paragraph,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts key questions that content can answer.
    /// </summary>
    Task<IEnumerable<QAPair>> ExtractQuestionsAsync(
        string content,
        int maxQuestions = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a direct answer for a specific question from content.
    /// </summary>
    Task<DirectAnswer?> GetDirectAnswerAsync(
        string question,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates all applicable structured data for content.
    /// </summary>
    Task<StructuredDataCollection> GenerateStructuredDataAsync(
        string content,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}
