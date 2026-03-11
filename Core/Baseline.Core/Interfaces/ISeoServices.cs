namespace Baseline.Core;

/// <summary>
/// Service for generating robots.txt content.
/// </summary>
public interface IRobotsTxtService
{
    /// <summary>
    /// Generates the robots.txt content.
    /// </summary>
    Task<string> GenerateAsync();
}

/// <summary>
/// Service for generating llms.txt content for AI crawlers.
/// </summary>
public interface ILlmsTxtService
{
    /// <summary>
    /// Generates the llms.txt content.
    /// </summary>
    Task<string> GenerateAsync();
}

/// <summary>
/// Service for generating security.txt content.
/// </summary>
public interface ISecurityTxtService
{
    /// <summary>
    /// Generates the security.txt content.
    /// </summary>
    Task<string> GenerateAsync();
}

/// <summary>
/// Service for generating JSON-LD from content.
/// </summary>
public interface IJsonLdGenerator
{
    /// <summary>
    /// Generates JSON-LD script tag from a schema object.
    /// </summary>
    string Generate(object schema);

    /// <summary>
    /// Generates JSON-LD script tag from multiple schema objects.
    /// </summary>
    string Generate(IEnumerable<object> schemas);
}
