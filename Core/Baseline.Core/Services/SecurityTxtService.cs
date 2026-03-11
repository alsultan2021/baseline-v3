using System.Text;

namespace Baseline.Core;

/// <summary>
/// Default implementation of <see cref="ISecurityTxtService"/>.
/// </summary>
internal sealed class SecurityTxtService(SecurityTxtOptions options) : ISecurityTxtService
{
    public Task<string> GenerateAsync()
    {
        if (string.IsNullOrEmpty(options.Contact))
        {
            return Task.FromResult(string.Empty);
        }

        var sb = new StringBuilder();

        // Contact is required
        sb.AppendLine($"Contact: {options.Contact}");

        // Expires is required per RFC 9116
        var expires = options.Expires ?? DateTimeOffset.UtcNow.AddYears(1);
        sb.AppendLine($"Expires: {expires:yyyy-MM-ddTHH:mm:sszzz}");

        if (!string.IsNullOrEmpty(options.Encryption))
        {
            sb.AppendLine($"Encryption: {options.Encryption}");
        }

        if (!string.IsNullOrEmpty(options.Acknowledgments))
        {
            sb.AppendLine($"Acknowledgments: {options.Acknowledgments}");
        }

        if (options.PreferredLanguages.Count > 0)
        {
            sb.AppendLine($"Preferred-Languages: {string.Join(", ", options.PreferredLanguages)}");
        }

        if (!string.IsNullOrEmpty(options.Canonical))
        {
            sb.AppendLine($"Canonical: {options.Canonical}");
        }

        if (!string.IsNullOrEmpty(options.Policy))
        {
            sb.AppendLine($"Policy: {options.Policy}");
        }

        if (!string.IsNullOrEmpty(options.Hiring))
        {
            sb.AppendLine($"Hiring: {options.Hiring}");
        }

        return Task.FromResult(sb.ToString().TrimEnd());
    }
}
