using System.ComponentModel.DataAnnotations;

using Baseline.AI.Admin.Components;
using Baseline.AI.Admin.Models;
using Baseline.AI.Data;

using Kentico.Xperience.Admin.Base.FormAnnotations;

namespace Baseline.AI.Admin.UIPages;

/// <summary>
/// Flat configuration model for AI Knowledge Base create / edit — mirrors Lucene's
/// <c>LuceneConfigurationModel</c>. All fields on a single page: name, display name,
/// strategy, and the inline path configuration React component.
/// </summary>
public sealed class AIKnowledgeBaseConfigModel
{
    /// <summary>DB primary key — 0 for new records.</summary>
    public int Id { get; set; }

    [TextInputComponent(
        Label = "Code Name",
        ExplanationText = "Unique code name for this knowledge base. Changing this value on an existing knowledge base without changing application code will cause the search experience to stop working.",
        Order = 1)]
    [Required]
    [MinLength(1)]
    public string KnowledgeBaseName { get; set; } = "";

    [TextInputComponent(
        Label = "Display Name",
        Order = 2)]
    [Required]
    [MinLength(1)]
    public string DisplayName { get; set; } = "";

    [TextInputComponent(
        Label = "Strategy Name",
        ExplanationText = "The indexing strategy specified in code during dependency registration",
        Order = 3)]
    public string StrategyName { get; set; } = "";

    [AIKBPathConfigurationComponent(
        Label = "Configured Paths",
        ExplanationText = "Manage the content tree paths this knowledge base will scan",
        Order = 4)]
    public IEnumerable<AIKBPathConfiguration> Paths { get; set; } = [];

    /// <summary>Empty constructor for model binding.</summary>
    public AIKnowledgeBaseConfigModel() { }

    /// <summary>Populate from existing knowledge base + paths.</summary>
    public AIKnowledgeBaseConfigModel(AIKnowledgeBaseInfo kb, IEnumerable<AIKBPathConfiguration> paths)
    {
        Id = kb.KnowledgeBaseId;
        KnowledgeBaseName = kb.KnowledgeBaseName;
        DisplayName = kb.KnowledgeBaseDisplayName;
        StrategyName = kb.KnowledgeBaseStrategyName;
        Paths = paths;
    }
}
