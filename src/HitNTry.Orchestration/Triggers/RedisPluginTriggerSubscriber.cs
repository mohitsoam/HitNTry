using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HitNTry.Orchestration.Triggers;

internal sealed class RedisPluginTriggerSubscriber : IHostedService
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IPluginTriggerBus _bus;
    private readonly PluginTriggerOptions _options;
    private readonly ILogger<RedisPluginTriggerSubscriber> _logger;
    private ChannelMessageQueue? _channel;

    public RedisPluginTriggerSubscriber(
        IConnectionMultiplexer multiplexer,
        IPluginTriggerBus bus,
        IOptions<PluginTriggerOptions> options,
        ILogger<RedisPluginTriggerSubscriber> logger)
    {
        _multiplexer = multiplexer;
        _bus = bus;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var subscriber = _multiplexer.GetSubscriber();
            _channel = await subscriber.SubscribeAsync(new RedisChannel(_options.RedisChannel, RedisChannel.PatternMode.Literal));
            _channel.OnMessage(async message =>
            {
                var trigger = new PluginTriggerEvent(
                    PluginTriggerSource.Redis,
                    PluginIdFromPayload(message.Message),
                    null,
                    message.Message);
                await _bus.PublishAsync(trigger, cancellationToken);
            });
            _logger.LogInformation("Listening for Redis triggers on {Channel}", _options.RedisChannel);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis trigger listener failed to start. The orchestrator will continue without Redis triggers.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.UnsubscribeAsync();
        }
    }

    private static string? PluginIdFromPayload(RedisValue value)
    {
        var payload = value.HasValue ? value.ToString() : null;
        if (payload is null)
        {
            return null;
        }

        var separatorIndex = payload.IndexOf(':');
        return separatorIndex > 0 ? payload[..separatorIndex] : payload;
    }
}

