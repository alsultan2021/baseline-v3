using System.Text.RegularExpressions;

namespace Baseline.AI.Security;

/// <summary>
/// Sanitizes content to prevent prompt injection and other security issues.
/// Used before content is stored in the vector database.
/// </summary>
public static class ContentSanitizer
{
    private static readonly string[] InjectionPatterns =
    [
        "ignore previous", "ignore all previous", "disregard previous",
        "forget previous", "disregard all instructions",
        "system:", "developer:", "assistant:", "tool:",
        "you are now", "act as", "pretend to be",
        "<script", "</script>", "javascript:",
        "{{", "}}", "{%", "%}", // Template injection
        "DROP TABLE", "DELETE FROM", "UPDATE SET", // SQL injection patterns
        "eval(", "exec(", "execute(", // Code execution
    ];

    private static readonly Regex HtmlTagPattern = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultipleWhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes content by removing/neutralizing injection patterns and HTML.
    /// </summary>
    /// <param name="content">Content to sanitize.</param>
    /// <param name="maxLength">Maximum length (default 8000 characters).</param>
    /// <returns>Sanitized content.</returns>
    public static string Sanitize(string content, int maxLength = 8000)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        // 1. Cap length first
        if (content.Length > maxLength)
        {
            content = content[..maxLength];
        }

        // 2. Remove/neutralize injection patterns
        foreach (var pattern in InjectionPatterns)
        {
            content = content.Replace(pattern, $"[{pattern.Length}chars]", StringComparison.OrdinalIgnoreCase);
        }

        // 3. Strip HTML tags (keep text content)
        content = HtmlTagPattern.Replace(content, " ");

        // 4. Normalize whitespace
        content = MultipleWhitespacePattern.Replace(content.Trim(), " ");

        return content;
    }

    /// <summary>
    /// Checks if content contains suspicious patterns that might indicate injection attempts.
    /// </summary>
    /// <param name="content">Content to check.</param>
    /// <returns>True if suspicious patterns detected.</returns>
    public static bool ContainsSuspiciousPatterns(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        foreach (var pattern in InjectionPatterns)
        {
            if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets list of detected suspicious patterns in content.
    /// </summary>
    /// <param name="content">Content to analyze.</param>
    /// <returns>List of detected pattern names.</returns>
    public static IReadOnlyList<string> GetDetectedPatterns(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return Array.Empty<string>();
        }

        var detected = new List<string>();

        foreach (var pattern in InjectionPatterns)
        {
            if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                detected.Add(pattern);
            }
        }

        return detected;
    }
}
