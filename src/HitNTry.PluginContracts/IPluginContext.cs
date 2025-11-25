using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HitNTry.PluginContracts;

/// <summary>
/// Service locator for plugin code. Backed by host-managed IServiceProvider scopes.
/// </summary>
public interface IPluginContext
{
    IServiceProvider Services { get; }
    IConfiguration Configuration { get; }
    ILogger Logger { get; }
    IPluginMetadata Metadata { get; }
    PluginExecutionRequest Request { get; }

    T GetRequiredService<T>() where T : notnull => Services.GetRequiredService<T>();
}

