namespace Baseline.AI.Admin;

/// <summary>
/// Custom permissions for AI Knowledge Base operations.
/// Mirrors Lucene's LuceneIndexPermissions pattern.
/// </summary>
internal static class AIKnowledgeBasePermissions
{
    /// <summary>
    /// Permission required to rebuild a knowledge base index.
    /// </summary>
    public const string REBUILD = "Rebuild";
}
