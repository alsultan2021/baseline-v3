namespace Baseline.SEO;

/// <summary>
/// A direct answer to a question.
/// </summary>
public record DirectAnswer
{
    /// <summary>
    /// The question.
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// The direct answer.
    /// </summary>
    public required string Answer { get; init; }

    /// <summary>
    /// Confidence in the answer (0-1).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Source passage from content.
    /// </summary>
    public string? SourcePassage { get; init; }

    /// <summary>
    /// Whether answer contains a definitive fact.
    /// </summary>
    public bool IsDefinitive { get; init; }
}
