using HitNTry.PluginContracts;
using HitNTry.Framework.Loading;

namespace HitNTry.Framework.Abstractions;

public interface IPluginManager
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    IReadOnlyCollection<PluginDescriptor> GetDescriptors();
    Task<PluginDescriptor?> LoadAsync(string assemblyPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PluginDescriptor>> LoadAllAsync(PluginFilter? filter = null, CancellationToken cancellationToken = default);
    Task UnloadAsync(string pluginId, CancellationToken cancellationToken = default);
    Task<PluginExecutionResult> ExecuteAsync(string pluginId, PluginExecutionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PluginExecutionResult>> ExecuteAsync(PluginFilter filter, PluginExecutionRequest request, CancellationToken cancellationToken = default);
    Task ReloadAsync(string pluginId, CancellationToken cancellationToken = default);
}

