using HitNTry.PluginContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitNTry.Framework.Runtime;

internal sealed class PluginContext : IPluginContext, IAsyncDisposable
{
    private readonly IServiceScope _serviceScope;

    public PluginContext(
        IServiceScope serviceScope,
        IConfiguration configuration,
        ILogger logger,
        IPluginMetadata metadata,
        PluginExecutionRequest request)
    {
        _serviceScope = serviceScope;
        Configuration = configuration;
        Logger = logger;
        Metadata = metadata;
        Request = request;
    }

    public IServiceProvider Services => _serviceScope.ServiceProvider;
    public IConfiguration Configuration { get; }
    public ILogger Logger { get; }
    public IPluginMetadata Metadata { get; }
    public PluginExecutionRequest Request { get; }

    public ValueTask DisposeAsync()
    {
        _serviceScope.Dispose();
        return ValueTask.CompletedTask;
    }
}

