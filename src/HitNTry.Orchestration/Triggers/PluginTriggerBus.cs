using System.Threading.Channels;

namespace HitNTry.Orchestration.Triggers;

public sealed class PluginTriggerBus : IPluginTriggerBus
{
    private readonly Channel<PluginTriggerEvent> _channel = Channel.CreateUnbounded<PluginTriggerEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public Task PublishAsync(PluginTriggerEvent trigger, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(trigger, cancellationToken).AsTask();

    public async IAsyncEnumerable<PluginTriggerEvent> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await _channel.Reader.WaitToReadAsync(cancellationToken))
        {
            while (_channel.Reader.TryRead(out var trigger))
            {
                yield return trigger;
            }
        }
    }
}

