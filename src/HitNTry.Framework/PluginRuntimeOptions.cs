namespace HitNTry.Framework;

public sealed class PluginRuntimeOptions
{
    public string PluginRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "Plugins");
    public bool WatchForChanges { get; set; } = true;
    public bool EnableHotReload { get; set; } = true;
    public IReadOnlyCollection<string> SharedAssemblies { get; set; } = new[]
    {
        "HitNTry.PluginContracts",
        "Microsoft.Extensions.Logging.Abstractions",
        "Microsoft.Extensions.Configuration.Abstractions",
        "System.Runtime"
    };
}

