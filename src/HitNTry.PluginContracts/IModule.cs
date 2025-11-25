namespace HitNTry.PluginContracts;

/// <summary>
/// Base contract every plugin module must implement.
/// </summary>
public interface IModule
{
    IPluginMetadata Metadata { get; }
    Task ExecuteAsync(IPluginContext context, CancellationToken cancellationToken = default);
}

