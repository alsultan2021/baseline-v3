namespace Baseline.AI;

/// <summary>
/// Configuration options for Baseline AI integration.
/// Similar to LuceneSearchOptions, these options define how AI features are configured.
/// </summary>
public sealed class BaselineAIOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SECTION_NAME = "BaselineAI";

    /// <summary>
    /// The AI provider to use (OpenAI, Azure, Anthropic, Ollama).
    /// </summary>
    public AIProviderType Provider { get; set; } = AIProviderType.OpenAI;

    /// <summary>
    /// API key for the AI service.
    /// For Azure, this is the API key from your Azure OpenAI resource.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint for the AI service.
    /// Required for Azure OpenAI and Ollama. Optional for OpenAI.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Embedding model to use for generating vector embeddings.
    /// Examples: "text-embedding-3-small", "text-embedding-ada-002"
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Chat/completion model to use for AI responses.
    /// Examples: "gpt-4o-mini", "gpt-4o", "claude-3-5-sonnet"
    /// </summary>
    public string ChatModel { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Embedding dimension size.
    /// Default for text-embedding-3-small is 1536.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// Enable vector search integration.
    /// When enabled, content is embedded and stored for semantic search.
    /// </summary>
    public bool EnableVectorSearch { get; set; } = true;

    /// <summary>
    /// Enable the AI chatbot widget for live site.
    /// </summary>
    public bool EnableChatbot { get; set; } = false;

    /// <summary>
    /// Enable AI-powered content auto-tagging.
    /// </summary>
    public bool EnableAutoTagging { get; set; } = false;

    /// <summary>
    /// Enable AI-powered search suggestions.
    /// </summary>
    public bool EnableSearchSuggestions { get; set; } = true;

    /// <summary>
    /// Enable automatic content embeddings on publish.
    /// </summary>
    public bool EnableAutoEmbeddings { get; set; } = true;

    /// <summary>
    /// RAG (Retrieval-Augmented Generation) configuration.
    /// </summary>
    public RAGOptions RAG { get; set; } = new();

    /// <summary>
    /// Chatbot configuration options.
    /// </summary>
    public ChatbotOptions Chatbot { get; set; } = new();

    /// <summary>
    /// Auto-tagging configuration options.
    /// </summary>
    public AutoTaggingOptions AutoTagging { get; set; } = new();

    /// <summary>
    /// Content types to index with embeddings.
    /// Empty list means all content types are indexed.
    /// </summary>
    public List<string> IndexedContentTypes { get; set; } = [];

    /// <summary>
    /// Content types to exclude from embedding indexing.
    /// </summary>
    public List<string> ExcludedContentTypes { get; set; } = [];

    /// <summary>
    /// Maximum tokens per chunk when splitting content for embeddings.
    /// </summary>
    public int MaxChunkTokens { get; set; } = 512;

    /// <summary>
    /// Overlap tokens between chunks.
    /// </summary>
    public int ChunkOverlapTokens { get; set; } = 50;
}

/// <summary>
/// AI provider types supported by Baseline.AI.
/// </summary>
public enum AIProviderType
{
    /// <summary>
    /// OpenAI API (api.openai.com)
    /// </summary>
    OpenAI,

    /// <summary>
    /// Azure OpenAI Service
    /// </summary>
    Azure,

    /// <summary>
    /// Anthropic Claude API
    /// </summary>
    Anthropic,

    /// <summary>
    /// Ollama for local LLM deployment
    /// </summary>
    Ollama,

    /// <summary>
    /// Custom provider implementation
    /// </summary>
    Custom
}

/// <summary>
/// RAG (Retrieval-Augmented Generation) options.
/// </summary>
public sealed class RAGOptions
{
    /// <summary>
    /// Number of similar documents to retrieve.
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0).
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.7;

    /// <summary>
    /// Include source citations in AI responses.
    /// </summary>
    public bool IncludeSourceCitations { get; set; } = true;

    /// <summary>
    /// Maximum context tokens to include in prompts.
    /// </summary>
    public int MaxContextTokens { get; set; } = 4000;

    /// <summary>
    /// System prompt for RAG context.
    /// </summary>
    public string SystemPrompt { get; set; } = """
        You are a helpful assistant for the website. Answer questions based on the provided context.
        If you cannot find the answer in the context, say so honestly.
        Always cite your sources when providing information.
        """;

    /// <summary>
    /// Enable hybrid search (combine keyword and semantic search).
    /// </summary>
    public bool EnableHybridSearch { get; set; } = true;

    /// <summary>
    /// Weight for semantic search in hybrid mode (0.0 to 1.0).
    /// </summary>
    public double SemanticWeight { get; set; } = 0.7;
}

/// <summary>
/// Chatbot widget options.
/// </summary>
public sealed class ChatbotOptions
{
    /// <summary>
    /// Chatbot widget title.
    /// </summary>
    public string Title { get; set; } = "Chat Assistant";

    /// <summary>
    /// Placeholder text for input.
    /// </summary>
    public string Placeholder { get; set; } = "Ask a question...";

    /// <summary>
    /// Welcome message shown when chat opens.
    /// </summary>
    public string WelcomeMessage { get; set; } = "Hello! How can I help you today?";

    /// <summary>
    /// Position of the chatbot widget.
    /// </summary>
    public ChatbotPosition Position { get; set; } = ChatbotPosition.BottomRight;

    /// <summary>
    /// Maximum conversation history messages to include.
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 10;

    /// <summary>
    /// Session timeout in minutes.
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Theme color for the chatbot widget.
    /// </summary>
    public string ThemeColor { get; set; } = "#007bff";

    /// <summary>
    /// Enable typing indicator animation.
    /// </summary>
    public bool ShowTypingIndicator { get; set; } = true;

    /// <summary>
    /// Enable suggested questions/prompts.
    /// </summary>
    public bool EnableSuggestedQuestions { get; set; } = true;

    /// <summary>
    /// Suggested starter questions.
    /// </summary>
    public List<string> StarterQuestions { get; set; } = [];

    /// <summary>
    /// Rate limit: max requests per window (default 20).
    /// </summary>
    public int RateLimitPermitCount { get; set; } = 20;

    /// <summary>
    /// Rate limit: sliding window in seconds (default 60).
    /// </summary>
    public int RateLimitWindowSeconds { get; set; } = 60;
}

/// <summary>
/// Chatbot widget position.
/// </summary>
public enum ChatbotPosition
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft
}

/// <summary>
/// Auto-tagging configuration options.
/// </summary>
public sealed class AutoTaggingOptions
{
    /// <summary>
    /// Taxonomy names to use for auto-tagging.
    /// Empty list means all taxonomies are available.
    /// </summary>
    public List<string> EnabledTaxonomies { get; set; } = [];

    /// <summary>
    /// Minimum confidence score to suggest a tag (0.0 to 1.0).
    /// </summary>
    public double MinConfidence { get; set; } = 0.7;

    /// <summary>
    /// Maximum tags to suggest per taxonomy.
    /// </summary>
    public int MaxTagsPerTaxonomy { get; set; } = 5;

    /// <summary>
    /// Use LLM for intelligent tag matching (vs pure embedding similarity).
    /// </summary>
    public bool UseLLM { get; set; } = true;

    /// <summary>
    /// Include tag descriptions when building embeddings.
    /// </summary>
    public bool IncludeTagDescriptions { get; set; } = true;

    /// <summary>
    /// Automatically apply tags above a certain confidence threshold.
    /// </summary>
    public bool AutoApply { get; set; } = false;

    /// <summary>
    /// Confidence threshold for auto-apply (higher than suggestion threshold).
    /// </summary>
    public double AutoApplyThreshold { get; set; } = 0.9;

    /// <summary>
    /// Content types eligible for auto-tagging.
    /// Empty list means all content types.
    /// </summary>
    public List<string> EligibleContentTypes { get; set; } = [];

    /// <summary>
    /// Fields to analyze for tag matching.
    /// </summary>
    public List<string> AnalyzedFields { get; set; } = ["Title", "Summary", "Description", "Content"];

    /// <summary>
    /// System prompt for LLM-based tag suggestion.
    /// </summary>
    public string LLMPrompt { get; set; } = """
        Analyze the following content and suggest the most relevant tags from the provided taxonomy.
        For each tag you suggest, provide a confidence score (0-1) and a brief reason.
        Only suggest tags that are truly relevant to the content.
        
        Content:
        {content}
        
        Available Tags:
        {tags}
        
        Respond in JSON format:
        {
            "suggestions": [
                { "tagName": "...", "confidence": 0.95, "reason": "..." }
            ]
        }
        """;
}
