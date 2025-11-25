using HitNTry.Framework.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HitNTry.Framework;

internal sealed class PluginWarmupHostedService(
    IPluginManager pluginManager,
    ILogger<PluginWarmupHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing HitNTry plugin runtime...");
        await pluginManager.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Plugin runtime shutting down.");
        return Task.CompletedTask;
    }
}

