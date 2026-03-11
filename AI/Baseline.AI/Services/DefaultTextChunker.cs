using System.Text.RegularExpressions;

namespace Baseline.AI;

/// <summary>
/// Default text chunker implementation.
/// Splits text into chunks suitable for embedding.
/// </summary>
internal sealed partial class DefaultTextChunker : ITextChunker
{
    // Approximate tokens per character (average for English)
    private const double TokensPerChar = 0.25;

    /// <inheritdoc />
    public IReadOnlyList<TextChunk> ChunkText(
        string text,
        int maxChunkTokens = 512,
        int overlapTokens = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var chunks = new List<TextChunk>();
        var maxChunkChars = (int)(maxChunkTokens / TokensPerChar);
        var overlapChars = (int)(overlapTokens / TokensPerChar);

        // Try to split on paragraph boundaries first
        var paragraphs = SplitIntoParagraphs(text);

        var currentChunk = string.Empty;
        var currentStart = 0;
        var chunkIndex = 0;

        foreach (var paragraph in paragraphs)
        {
            var trimmedParagraph = paragraph.Trim();
            if (string.IsNullOrEmpty(trimmedParagraph))
            {
                continue;
            }

            // If paragraph alone is too long, split it further
            if (EstimateTokens(trimmedParagraph) > maxChunkTokens)
            {
                // Flush current chunk first
                if (!string.IsNullOrWhiteSpace(currentChunk))
                {
                    chunks.Add(CreateChunk(currentChunk, currentStart, chunkIndex++));
                }

                // Split long paragraph by sentences
                var sentences = SplitIntoSentences(trimmedParagraph);
                currentChunk = string.Empty;
                currentStart = text.IndexOf(trimmedParagraph, StringComparison.Ordinal);

                foreach (var sentence in sentences)
                {
                    if (EstimateTokens(currentChunk + " " + sentence) <= maxChunkTokens)
                    {
                        currentChunk = string.IsNullOrEmpty(currentChunk)
                            ? sentence
                            : currentChunk + " " + sentence;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(currentChunk))
                        {
                            chunks.Add(CreateChunk(currentChunk, currentStart, chunkIndex++));
                            // Add overlap from the end of current chunk
                            currentChunk = GetOverlapText(currentChunk, overlapChars) + " " + sentence;
                            currentStart = text.IndexOf(sentence, StringComparison.Ordinal);
                        }
                        else
                        {
                            currentChunk = sentence;
                        }
                    }
                }
            }
            else
            {
                // Check if adding this paragraph exceeds limit
                var combined = string.IsNullOrEmpty(currentChunk)
                    ? trimmedParagraph
                    : currentChunk + "\n\n" + trimmedParagraph;

                if (EstimateTokens(combined) <= maxChunkTokens)
                {
                    if (string.IsNullOrEmpty(currentChunk))
                    {
                        currentStart = text.IndexOf(trimmedParagraph, StringComparison.Ordinal);
                    }
                    currentChunk = combined;
                }
                else
                {
                    // Flush current chunk and start new one
                    if (!string.IsNullOrWhiteSpace(currentChunk))
                    {
                        chunks.Add(CreateChunk(currentChunk, currentStart, chunkIndex++));
                    }

                    currentStart = text.IndexOf(trimmedParagraph, StringComparison.Ordinal);
                    currentChunk = trimmedParagraph;
                }
            }
        }

        // Add final chunk
        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(CreateChunk(currentChunk, currentStart, chunkIndex));
        }

        return chunks;
    }

    private static TextChunk CreateChunk(string content, int startPosition, int index)
    {
        return new TextChunk
        {
            Index = index,
            Content = content.Trim(),
            StartPosition = startPosition,
            EndPosition = startPosition + content.Length,
            EstimatedTokens = EstimateTokens(content)
        };
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Simple estimation: ~4 characters per token for English
        return (int)Math.Ceiling(text.Length * TokensPerChar);
    }

    private static string GetOverlapText(string text, int overlapChars)
    {
        if (text.Length <= overlapChars)
            return text;

        var start = text.Length - overlapChars;
        // Try to start at a word boundary
        var spaceIndex = text.IndexOf(' ', start);
        if (spaceIndex > 0 && spaceIndex < text.Length - 10)
        {
            start = spaceIndex + 1;
        }

        return text[start..];
    }

    private static IReadOnlyList<string> SplitIntoParagraphs(string text)
    {
        return ParagraphRegex().Split(text)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    private static IReadOnlyList<string> SplitIntoSentences(string text)
    {
        return SentenceRegex().Split(text)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    [GeneratedRegex(@"\n\s*\n", RegexOptions.Compiled)]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+", RegexOptions.Compiled)]
    private static partial Regex SentenceRegex();
}
