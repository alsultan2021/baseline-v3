using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Baseline.AI.Services;

/// <summary>
/// Pass-through wrapper that only injects plugins into the kernel
/// without replacing the underlying chat completion service.
/// </summary>
internal sealed class PluginInjectionChatService(
    [FromKeyedServices(AiraPluginServiceKeys.OriginalChat)] IChatCompletionService kenticoService,
    IEnumerable<IAiraPlugin> plugins,
    IAiraPluginRegistry registry) : AiraPluginChatServiceBase(kenticoService, plugins, registry);
