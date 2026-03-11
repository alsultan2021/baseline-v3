using System.Text;

namespace Baseline.AI.Indexing;

/// <summary>
/// Service for splitting text content into chunks for embedding.
/// The chunking service owns all chunking logic - strategies only provide options.
/// </summary>
public interface IAIChunkingService
{
    /// <summary>
    /// Splits content into chunks suitable for embedding.
    /// </summary>
    /// <param name="content">The text content to chunk.</param>
    /// <param name="options">Chunking configuration.</param>
    /// <returns>Ordered list of chunks with their indices.</returns>
    IReadOnlyList<TextChunk> ChunkContent(string content, ChunkingOptions options);
}

/// <summary>
/// Default implementation of <see cref="IAIChunkingService"/>.
/// Uses paragraph-aware chunking with configurable overlap.
/// </summary>
public sealed class DefaultAIChunkingService : IAIChunkingService
{
    public IReadOnlyList<TextChunk> ChunkContent(string content, ChunkingOptions options)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var chunks = new List<TextChunk>();
        var maxSize = options.MaxChunkSize;
        var overlap = options.ChunkOverlap;

        if (options.SplitOnParagraphs)
        {
            chunks = ChunkByParagraphs(content, maxSize, overlap);
        }
        else
        {
            chunks = ChunkBySize(content, maxSize, overlap);
        }

        // Assign indices
        return chunks
            .Select((c, i) => c with { ChunkIndex = i })
            .ToList();
    }

    private static List<TextChunk> ChunkByParagraphs(string content, int maxSize, int overlap)
    {
        var paragraphs = content
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        var chunks = new List<TextChunk>();
        var currentChunk = new StringBuilder();

        foreach (var para in paragraphs)
        {
            // If adding this paragraph would exceed max size, finalize current chunk
            if (currentChunk.Length > 0 && currentChunk.Length + para.Length + 2 > maxSize)
            {
                chunks.Add(new TextChunk(currentChunk.ToString().Trim(), 0));

                // Start new chunk with overlap from end of previous
                var overlapText = GetOverlapText(currentChunk.ToString(), overlap);
                currentChunk.Clear();
                if (!string.IsNullOrEmpty(overlapText))
                {
                    currentChunk.Append(overlapText).Append(' ');
                }
            }

            // If single paragraph exceeds max, chunk it by size
            if (para.Length > maxSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(new TextChunk(currentChunk.ToString().Trim(), 0));
                    currentChunk.Clear();
                }

                var subChunks = ChunkBySize(para, maxSize, overlap);
                chunks.AddRange(subChunks);
            }
            else
            {
                if (currentChunk.Length > 0)
                {
                    currentChunk.Append("\n\n");
                }
                currentChunk.Append(para);
            }
        }

        // Add final chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(new TextChunk(currentChunk.ToString().Trim(), 0));
        }

        return chunks;
    }

    private static List<TextChunk> ChunkBySize(string content, int maxSize, int overlap)
    {
        var chunks = new List<TextChunk>();

        if (content.Length <= maxSize)
        {
            chunks.Add(new TextChunk(content, 0));
            return chunks;
        }

        int start = 0;
        while (start < content.Length)
        {
            int end = Math.Min(start + maxSize, content.Length);

            // Try to break at word boundary
            if (end < content.Length)
            {
                int lastSpace = content.LastIndexOf(' ', end - 1, Math.Min(100, end - start));
                if (lastSpace > start)
                {
                    end = lastSpace;
                }
            }

            var chunkText = content[start..end].Trim();
            if (!string.IsNullOrEmpty(chunkText))
            {
                chunks.Add(new TextChunk(chunkText, 0));
            }

            // Move start with overlap
            start = end - overlap;
            if (start >= content.Length - 10)
            {
                break;
            }
        }

        return chunks;
    }

    private static string GetOverlapText(string text, int overlap)
    {
        if (overlap <= 0 || string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (text.Length <= overlap)
        {
            return text;
        }

        // Get last 'overlap' characters, try to start at word boundary
        var start = text.Length - overlap;
        var nextSpace = text.IndexOf(' ', start);
        if (nextSpace > start && nextSpace < text.Length - 10)
        {
            start = nextSpace + 1;
        }

        return text[start..];
    }
}
