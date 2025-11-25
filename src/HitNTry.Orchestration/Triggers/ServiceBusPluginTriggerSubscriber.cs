using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HitNTry.Orchestration.Triggers;

internal sealed class ServiceBusPluginTriggerSubscriber : IHostedService
{
    private readonly ILogger<ServiceBusPluginTriggerSubscriber> _logger;

    public ServiceBusPluginTriggerSubscriber(ILogger<ServiceBusPluginTriggerSubscriber> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("ServiceBus triggers are disabled in this build.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

