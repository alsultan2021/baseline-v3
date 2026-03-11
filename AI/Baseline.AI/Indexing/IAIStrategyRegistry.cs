using System.Security.Cryptography;
using System.Text;

namespace Baseline.AI.Indexing;

/// <summary>
/// Registry for AI indexing strategies. Maps strategy names to implementations.
/// </summary>
public interface IAIStrategyRegistry
{
    /// <summary>
    /// Gets a strategy by name.
    /// </summary>
    /// <param name="strategyName">The unique strategy name.</param>
    /// <returns>The strategy, or null if not found.</returns>
    IAIIndexingStrategy? GetStrategy(string strategyName);

    /// <summary>
    /// Gets all registered strategy names.
    /// </summary>
    IReadOnlyList<string> GetStrategyNames();

    /// <summary>
    /// Computes hash for a strategy's configuration.
    /// Used to detect strategy drift requiring rebuild.
    /// </summary>
    /// <param name="strategyName">The strategy name.</param>
    /// <returns>SHA256 hash of strategy configuration, or null if strategy not found.</returns>
    string? ComputeStrategyHash(string strategyName);
}

/// <summary>
/// Default implementation of <see cref="IAIStrategyRegistry"/> using DI-registered strategies.
/// </summary>
public sealed class DefaultAIStrategyRegistry : IAIStrategyRegistry
{
    private readonly IReadOnlyDictionary<string, IAIIndexingStrategy> _strategies;

    public DefaultAIStrategyRegistry(IEnumerable<IAIIndexingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.StrategyName, StringComparer.OrdinalIgnoreCase);
    }

    public IAIIndexingStrategy? GetStrategy(string strategyName) =>
        _strategies.TryGetValue(strategyName, out var strategy) ? strategy : null;

    public IReadOnlyList<string> GetStrategyNames() =>
        _strategies.Keys.ToList();

    public string? ComputeStrategyHash(string strategyName)
    {
        var strategy = GetStrategy(strategyName);
        if (strategy is null)
        {
            return null;
        }

        // Strategy provides its own hash computation
        var strategyHash = strategy.ComputeStrategyHash();
        if (!string.IsNullOrEmpty(strategyHash))
        {
            return strategyHash;
        }

        // Fallback: hash the strategy name + chunking options
        var options = strategy.GetChunkingOptions();
        var input = $"{strategyName}|{options.MaxChunkSize}|{options.ChunkOverlap}|{options.SplitOnParagraphs}";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
