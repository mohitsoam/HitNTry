using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HitNTry.Orchestration.Triggers;

internal sealed class KafkaPluginTriggerSubscriber : IHostedService
{
    private readonly ILogger<KafkaPluginTriggerSubscriber> _logger;

    public KafkaPluginTriggerSubscriber(ILogger<KafkaPluginTriggerSubscriber> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka triggers are disabled in this build.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

