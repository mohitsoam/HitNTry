using HitNTry.Framework.Abstractions;
using HitNTry.PluginContracts;

namespace HitNTry.Orchestration;

public sealed class PluginExecutionOrchestrator(IPluginManager pluginManager) : IPluginExecutionOrchestrator
{
    public Task<PluginExecutionResult> ExecuteAsync(string pluginId, PluginExecutionRequest request, CancellationToken cancellationToken = default)
        => pluginManager.ExecuteAsync(pluginId, request, cancellationToken);

    public Task<IReadOnlyCollection<PluginExecutionResult>> ExecuteAsync(PluginFilter filter, PluginExecutionRequest request, CancellationToken cancellationToken = default)
        => pluginManager.ExecuteAsync(filter, request, cancellationToken);
}

