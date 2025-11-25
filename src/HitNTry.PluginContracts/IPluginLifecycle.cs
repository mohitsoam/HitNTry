namespace HitNTry.PluginContracts;

/// <summary>
/// Optional lifecycle hooks plugins can implement for resource management.
/// </summary>
public interface IPluginLifecycle
{
    Task OnLoadAsync(IPluginContext context, CancellationToken cancellationToken) 
        => Task.CompletedTask;

    Task OnUnloadAsync(IPluginContext context, CancellationToken cancellationToken) 
        => Task.CompletedTask;

    Task OnErrorAsync(IPluginContext context, Exception exception, CancellationToken cancellationToken) 
        => Task.CompletedTask;
}

