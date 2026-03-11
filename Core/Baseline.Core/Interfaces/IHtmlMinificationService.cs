namespace Baseline.Core;

/// <summary>
/// Service for HTML minification in production.
/// </summary>
public interface IHtmlMinificationService
{
    /// <summary>
    /// Minifies HTML content.
    /// </summary>
    /// <param name="html">The HTML to minify.</param>
    /// <returns>Minified HTML.</returns>
    string Minify(string html);

    /// <summary>
    /// Checks if minification is enabled.
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Service for ads.txt content generation.
/// </summary>
public interface IAdsTxtService
{
    /// <summary>
    /// Generates the ads.txt content.
    /// </summary>
    Task<string> GenerateAsync();
}
/// <summary>
/// Service for generating channel-specific code snippets (analytics, tracking, etc.).
/// Per Kentico docs: Web channel snippets for metadata, CSS, and JavaScript.
/// </summary>
public interface IChannelSnippetService
{
    /// <summary>
    /// Gets metadata snippets for the current channel.
    /// </summary>
    Task<IEnumerable<ChannelSnippet>> GetMetadataSnippetsAsync();

    /// <summary>
    /// Gets CSS snippets for the current channel.
    /// </summary>
    Task<IEnumerable<ChannelSnippet>> GetCssSnippetsAsync();

    /// <summary>
    /// Gets JavaScript snippets for the current channel.
    /// </summary>
    Task<IEnumerable<ChannelSnippet>> GetJavaScriptSnippetsAsync();
}

/// <summary>
/// A channel-specific code snippet.
/// </summary>
public class ChannelSnippet
{
    /// <summary>
    /// Snippet code name for identification.
    /// </summary>
    public string CodeName { get; set; } = string.Empty;

    /// <summary>
    /// Snippet type: Metadata, CSS, or JavaScript.
    /// </summary>
    public SnippetType Type { get; set; }

    /// <summary>
    /// The snippet content (code).
    /// </summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Types of channel snippets.
/// </summary>
public enum SnippetType
{
    Metadata,
    Css,
    JavaScript
}
/// <summary>
/// Service for app-ads.txt content generation (mobile apps).
/// </summary>
public interface IAppAdsTxtService
{
    /// <summary>
    /// Generates the app-ads.txt content.
    /// </summary>
    Task<string> GenerateAsync();
}
