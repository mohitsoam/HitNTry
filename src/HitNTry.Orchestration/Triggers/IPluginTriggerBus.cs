namespace HitNTry.Orchestration.Triggers;

public interface IPluginTriggerBus
{
    Task PublishAsync(PluginTriggerEvent trigger, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PluginTriggerEvent> ListenAsync(CancellationToken cancellationToken = default);
}

