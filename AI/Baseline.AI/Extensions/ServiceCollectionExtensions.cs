using Baseline.AI.Data;
using Baseline.AI.Events;
using Baseline.AI.Indexing;
using Baseline.AI.Plugins;
using Baseline.AI.Services;
using Baseline.AI.Workers;

using CMS.DataEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Baseline.AI;

/// <summary>
/// Extension methods for registering Baseline AI services.
/// Follows the same pattern as AddKenticoLucene.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Baseline AI services with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddBaselineAI(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddBaselineAIInternal(configuration);

        // Register default strategy
        AIStrategyStorage.AddStrategy<DefaultAIIndexingStrategy>("Default");

        return services;
    }

    /// <summary>
    /// Adds Baseline AI services with custom configuration.
    /// Similar to AddKenticoLucene pattern.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Builder configuration action.</param>
    /// <param name="configuration">Optional IConfiguration.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddBaselineAI(builder =>
    /// {
    ///     builder.RegisterStrategy&lt;MySearchStrategy&gt;("MySearch");
    ///     builder.RegisterProvider&lt;OpenAIProvider&gt;();
    ///     builder.RegisterVectorStore&lt;InMemoryVectorStore&gt;();
    /// }, configuration);
    /// </code>
    /// </example>
    public static IServiceCollection AddBaselineAI(
        this IServiceCollection services,
        Action<IAIBuilder> configure,
        IConfiguration? configuration = null)
    {
        services.AddBaselineAIInternal(configuration);

        var builder = new AIBuilder(services);
        configure(builder);

        if (builder.IncludeDefaultStrategy)
        {
            builder.RegisterStrategy<DefaultAIIndexingStrategy>("Default");
        }

        return services;
    }

    private static void AddBaselineAIInternal(
        this IServiceCollection services,
        IConfiguration? configuration)
    {
        // Configure options from appsettings.json
        if (configuration is not null)
        {
            services.Configure<BaselineAIOptions>(
                configuration.GetSection(BaselineAIOptions.SECTION_NAME));
        }

        // Register application part so MVC discovers ViewComponents and Razor views
        services.AddControllersWithViews()
            .AddApplicationPart(typeof(ServiceCollectionExtensions).Assembly);

        // Register core services
        services
            .AddSingleton<BaselineAIOptions>(sp =>
            {
                var options = new BaselineAIOptions();
                configuration?.GetSection(BaselineAIOptions.SECTION_NAME).Bind(options);
                return options;
            })
            // Note: IInfoProvider<T> uses Provider<T>.Instance which requires DB tables to exist.
            // These are resolved lazily in services that need them, not at startup.
            .AddSingleton<IAIStrategyRegistry, DefaultAIStrategyRegistry>()
            .AddSingleton<IAIChunkingService, DefaultAIChunkingService>()
            .AddSingleton<ITextChunker, DefaultTextChunker>()
            .AddSingleton<IAIEmbeddingService, DefaultAIEmbeddingService>()
            // Use Scoped for services that access Kentico DB to avoid blocking startup
            .AddScoped<IAIIndexManager, DefaultAIIndexManager>()
            .AddScoped<IAIContentScanner, DefaultAIContentScanner>()
            .AddSingleton<AIContentEventHandler>()
            .AddScoped<IAISearchService, DefaultAISearchService>()
            .AddScoped<IChatbotService, DefaultChatbotService>()
            .AddTransient<DefaultAIIndexingStrategy>()
            // Register hosted service with lazy dependency resolution
            .AddHostedService<AIIndexQueueWorker>();

        // Register default implementations if not already registered
        services.TryAddSingleton<IVectorStore, NoOpVectorStore>();
        services.TryAddSingleton<IAIProvider, NoOpAIProvider>();
        services.TryAddSingleton<IChatSessionStore, InMemoryChatSessionStore>();
    }

    /// <summary>
    /// Adds content auto-tagging services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for auto-tagging options.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers:
    /// - IContentAutoTaggingService for suggesting and applying tags to content
    /// - ITaxonomyEmbeddingService for managing taxonomy embeddings
    /// 
    /// Requires AddBaselineAI to be called first for embedding and LLM services.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddBaselineAI(configuration)
    ///         .AddAutoTagging(options =>
    ///         {
    ///             options.EnabledTaxonomies = ["Categories", "Tags"];
    ///             options.MinConfidence = 0.7f;
    ///             options.UseLLM = true;
    ///         });
    /// </code>
    /// </example>
    public static IServiceCollection AddAutoTagging(
        this IServiceCollection services,
        Action<AutoTaggingOptions>? configure = null)
    {
        // Register XbK providers for taxonomy and content access
        services.AddScoped<ITaxonomyProvider, XbKTaxonomyProvider>();
        services.AddScoped<IContentProvider, XbKContentProvider>();

        // Register auto-tagging services
        services.AddScoped<IContentAutoTaggingService, DefaultContentAutoTaggingService>();
        services.AddScoped<ITaxonomyEmbeddingService, DefaultTaxonomyEmbeddingService>();

        // Apply custom configuration if provided
        if (configure is not null)
        {
            services.Configure<BaselineAIOptions>(options =>
            {
                options.EnableAutoTagging = true;
                configure(options.AutoTagging);
            });
        }

        return services;
    }

    /// <summary>
    /// Tries to add a service if it hasn't been registered yet.
    /// </summary>
    private static void TryAddSingleton<TService, TImplementation>(
        this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        if (!services.Any(d => d.ServiceType == typeof(TService)))
        {
            services.AddSingleton<TService, TImplementation>();
        }
    }

    /// <summary>
    /// Replaces the default in-memory session store with IDistributedCache-backed storage.
    /// Requires <c>services.AddDistributedMemoryCache()</c>, <c>AddStackExchangeRedisCache()</c>,
    /// or similar to be registered first.
    /// </summary>
    public static IServiceCollection AddBaselineAIDistributedSessions(this IServiceCollection services)
    {
        // Remove existing InMemory registration
        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IChatSessionStore));
        if (existing is not null)
        {
            services.Remove(existing);
        }

        services.AddSingleton<IChatSessionStore, DistributedChatSessionStore>();
        return services;
    }

    // ──────────────────────────────────────────────────────────────
    //  AIRA Core Tool Plugins
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the core MCP-like AIRA tool plugins that have no admin dependency:
    /// Think, Todo, WebFetch, ContentRead, ContentSearch.
    /// </summary>
    public static IServiceCollection AddAiraCoreTools(this IServiceCollection services)
    {
        services.AddAiraPlugin<ThinkPlugin>();
        services.AddAiraPlugin<TodoPlugin>();
        services.AddAiraPlugin<WebFetchPlugin>();
        services.AddAiraPlugin<ContentReadPlugin>();
        services.AddAiraPlugin<ContentSearchPlugin>();
        services.AddAiraPlugin<SearchAiraPlugin>();
        services.AddAiraPlugin<ExperimentsAiraPlugin>();

        services.AddHttpClient("AiraWebFetch", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; BaselineAI/1.0; +https://github.com/nickndev)");
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        });

        return services;
    }

    // ──────────────────────────────────────────────────────────────
    //  AIRA Plugin Infrastructure
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Accumulated per-plugin options during <c>AddAiraPlugin</c> calls, keyed by implementation type.
    /// Resolved to plugin-name keys at <c>UseAiraPlugins</c> time when DI instances are available.
    /// </summary>
    private static readonly Dictionary<Type, AiraPluginOptions> s_pluginOptionsByType = [];

    /// <summary>
    /// Registers an <see cref="IAiraPlugin"/> so it is automatically added to the
    /// Semantic Kernel when the AIRA chat completion service is invoked.
    /// Optionally configure per-plugin options such as <see cref="AiraPluginOptions.EnhancementPrompt"/>.
    /// </summary>
    /// <typeparam name="TPlugin">Plugin type implementing <see cref="IAiraPlugin"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional per-plugin configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAiraPlugin<TPlugin>(
        this IServiceCollection services,
        Action<AiraPluginOptions>? configure = null)
        where TPlugin : class, IAiraPlugin
    {
        services.AddScoped<IAiraPlugin, TPlugin>();

        var options = new AiraPluginOptions();
        configure?.Invoke(options);
        s_pluginOptionsByType[typeof(TPlugin)] = options;

        return services;
    }

    /// <summary>
    /// Wraps Kentico's chat service to inject registered <see cref="IAiraPlugin"/> instances
    /// into the kernel — without replacing the underlying LLM.
    /// Call after <c>AddKentico()</c>. Use <c>AddAiraPlugin&lt;T&gt;()</c> to register plugins first.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection UseAiraPlugins(this IServiceCollection services)
    {
        CaptureKenticoChatService(services);

        services.AddKeyedScoped<IChatCompletionService>("Aira", (sp, _) =>
            ActivatorUtilities.CreateInstance<PluginInjectionChatService>(sp));

        services.AddScoped<IAiraPluginRegistry>(sp =>
        {
            var plugins = sp.GetServices<IAiraPlugin>().ToList();

            // Re-key options by actual PluginName (resolved at runtime from DI instances).
            var optionsByName = new Dictionary<string, AiraPluginOptions>();
            foreach (var plugin in plugins)
            {
                if (s_pluginOptionsByType.TryGetValue(plugin.GetType(), out var opts))
                {
                    optionsByName[plugin.PluginName] = opts;
                }
            }

            return AiraPluginRegistry.Create(plugins, optionsByName);
        });

        return services;
    }

    /// <summary>
    /// Captures the existing Kentico AIRA <c>IChatCompletionService</c> registration
    /// under a secondary key so it can be resolved by the plugin injection decorator.
    /// </summary>
    private static void CaptureKenticoChatService(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(
            reg => reg.IsKeyedService
                && reg.ServiceType == typeof(IChatCompletionService)
                && string.Equals(reg.ServiceKey as string, "Aira", StringComparison.Ordinal));

        if (descriptor is not null)
        {
            services.AddKeyedSingleton<IChatCompletionService>(
                AiraPluginServiceKeys.OriginalChat, (sp, _) =>
                {
                    if (descriptor.ImplementationFactory is not null)
                        return (IChatCompletionService)descriptor.ImplementationFactory(sp);
                    if (descriptor.KeyedImplementationFactory is not null)
                        return (IChatCompletionService)descriptor.KeyedImplementationFactory(sp, "Aira");
                    if (descriptor.ImplementationInstance is not null)
                        return (IChatCompletionService)descriptor.ImplementationInstance;
                    if (descriptor.KeyedImplementationInstance is not null)
                        return (IChatCompletionService)descriptor.KeyedImplementationInstance;

                    return (IChatCompletionService)ActivatorUtilities.CreateInstance(
                        sp, descriptor.ImplementationType ?? descriptor.KeyedImplementationType!);
                });
        }
    }
}
