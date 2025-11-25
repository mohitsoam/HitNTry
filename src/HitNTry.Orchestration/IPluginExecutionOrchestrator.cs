using HitNTry.PluginContracts;

namespace HitNTry.Orchestration;

public interface IPluginExecutionOrchestrator
{
    Task<PluginExecutionResult> ExecuteAsync(string pluginId, PluginExecutionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PluginExecutionResult>> ExecuteAsync(PluginFilter filter, PluginExecutionRequest request, CancellationToken cancellationToken = default);
}

