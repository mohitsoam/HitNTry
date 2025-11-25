using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HitNTry.Orchestration.Triggers;

internal sealed class RedisPluginTriggerSubscriber : IHostedService
{
    private readonly ILogger<RedisPluginTriggerSubscriber> _logger;

    public RedisPluginTriggerSubscriber(ILogger<RedisPluginTriggerSubscriber> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Redis triggers are disabled in this build.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

