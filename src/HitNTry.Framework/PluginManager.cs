using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HitNTry.Framework.Abstractions;
using HitNTry.Framework.Loading;
using HitNTry.Framework.Runtime;
using HitNTry.PluginContracts;
using HitNTry.PluginContracts.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HitNTry.Framework;

public sealed class PluginManager : IPluginManager, IDisposable
{
    private readonly PluginRuntimeOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PluginManager> _logger;
    private readonly ConcurrentDictionary<string, PluginDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    public PluginManager(
        IOptions<PluginRuntimeOptions> options,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PluginManager> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_options.PluginRootPath);
        await LoadAllAsync(cancellationToken: cancellationToken);
        if (_options.WatchForChanges)
        {
            _watcher = new FileSystemWatcher(_options.PluginRootPath, "*.dll")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnPluginChanged;
            _watcher.Created += OnPluginChanged;
            _watcher.Deleted += OnPluginChanged;
            _watcher.Renamed += OnPluginRenamed;
        }
    }

    public IReadOnlyCollection<PluginDescriptor> GetDescriptors() => _descriptors.Values.ToArray();

    public async Task<PluginDescriptor?> LoadAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        var file = new FileInfo(assemblyPath);
        if (!file.Exists)
        {
            _logger.LogWarning("Plugin assembly {AssemblyPath} was not found", assemblyPath);
            return null;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await LoadInternalAsync(file, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyCollection<PluginDescriptor>> LoadAllAsync(PluginFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var directory = new DirectoryInfo(_options.PluginRootPath);
        if (!directory.Exists)
        {
            return Array.Empty<PluginDescriptor>();
        }

        var descriptors = new List<PluginDescriptor>();
        await _lock.WaitAsync(cancellationToken);
        try
        {
            foreach (var file in directory.EnumerateFiles("*.dll", SearchOption.AllDirectories))
            {
                var descriptor = await LoadInternalAsync(file, cancellationToken);
                if (descriptor is null)
                {
                    continue;
                }

                if (!MatchesFilter(descriptor.Metadata, filter))
                {
                    continue;
                }

                descriptors.Add(descriptor);
            }
        }
        finally
        {
            _lock.Release();
        }

        return descriptors;
    }

    public async Task UnloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_descriptors.TryRemove(pluginId, out var descriptor))
        {
            return;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            descriptor.LoadContext.Unload();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<PluginExecutionResult> ExecuteAsync(string pluginId, PluginExecutionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_descriptors.TryGetValue(pluginId, out var descriptor))
        {
            throw new InvalidOperationException($"Plugin {pluginId} has not been loaded.");
        }

        return await ExecuteInternalAsync(descriptor, request, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PluginExecutionResult>> ExecuteAsync(PluginFilter filter, PluginExecutionRequest request, CancellationToken cancellationToken = default)
    {
        var targets = _descriptors.Values.Where(d => MatchesFilter(d.Metadata, filter)).ToArray();
        var results = new List<PluginExecutionResult>(targets.Length);
        foreach (var descriptor in targets)
        {
            results.Add(await ExecuteInternalAsync(descriptor, request, cancellationToken));
        }

        return results;
    }

    public async Task ReloadAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_descriptors.TryGetValue(pluginId, out var descriptor))
        {
            return;
        }

        await UnloadAsync(pluginId, cancellationToken);
        await LoadAsync(descriptor.AssemblyPath, cancellationToken);
    }

    private async Task<PluginDescriptor?> LoadInternalAsync(FileInfo file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loadContext = new Loading.PluginLoadContext(file.FullName, _options.SharedAssemblies);
        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(file.FullName);
            var moduleType = assembly
                .GetTypes()
                .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

            if (moduleType is null)
            {
                _logger.LogWarning("Skipping {Assembly} because no IModule implementation was found.", file.Name);
                loadContext.Unload();
                return null;
            }

            var metadata = ResolveMetadata(assembly);
            var manifest = TryLoadManifest(file.Directory);
            var descriptor = new PluginDescriptor(
                PluginId(metadata),
                file.FullName,
                DateTimeOffset.UtcNow,
                metadata,
                moduleType,
                PluginState.Loaded,
                manifest,
                loadContext);

            _descriptors[descriptor.PluginId] = descriptor;
            _logger.LogInformation("Loaded plugin {Plugin}", descriptor.PluginId);
            return descriptor;
        }
        catch (Exception ex)
        {
            loadContext.Unload();
            _logger.LogError(ex, "Failed to load plugin from {Assembly}", file.FullName);
            throw;
        }
    }

    private async Task<PluginExecutionResult> ExecuteInternalAsync(PluginDescriptor descriptor, PluginExecutionRequest request, CancellationToken cancellationToken)
    {
        _descriptors[descriptor.PluginId] = descriptor with { State = PluginState.Executing };
        var stopwatch = Stopwatch.StartNew();

        await using var context = CreatePluginContext(descriptor.Metadata, request);
        var logger = context.Logger;
        logger.LogInformation("Executing plugin {Plugin}", descriptor.PluginId);

        var dbContext = context.Services.GetService<AppDbContext>();
        PluginExecutionLog? log = null;
        if (dbContext is not null)
        {
            log = new PluginExecutionLog
            {
                PluginName = descriptor.Metadata.Name,
                Version = descriptor.Metadata.Version,
                Status = "Executing",
                CorrelationId = request.CorrelationId,
                    Tags = string.Join(',', descriptor.Metadata.Tags)
            };
            dbContext.ExecutionLogs.Add(log);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        try
        {
            var module = (IModule)ActivatorUtilities.CreateInstance(context.Services, descriptor.ModuleType);
            if (module is IPluginLifecycle lifecycle)
            {
                await lifecycle.OnLoadAsync(context, cancellationToken);
            }

            await module.ExecuteAsync(context, cancellationToken);

            if (module is IPluginLifecycle lifecycleUnload)
            {
                await lifecycleUnload.OnUnloadAsync(context, cancellationToken);
            }

            stopwatch.Stop();
            if (log is not null)
            {
                log.Status = "Completed";
                log.CompletedAt = DateTimeOffset.UtcNow;
                await dbContext!.SaveChangesAsync(cancellationToken);
            }

            var result = PluginExecutionResult.Success(descriptor.Metadata.Name, stopwatch.Elapsed);
            _descriptors[descriptor.PluginId] = descriptor with { State = PluginState.Completed };
            logger.LogInformation("Plugin {Plugin} completed in {Elapsed} ms", descriptor.PluginId, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Plugin {Plugin} failed", descriptor.PluginId);
            if (log is not null)
            {
                log.Status = "Faulted";
                log.Error = ex.ToString();
                log.CompletedAt = DateTimeOffset.UtcNow;
                await dbContext!.SaveChangesAsync(cancellationToken);
            }

            _descriptors[descriptor.PluginId] = descriptor with { State = PluginState.Faulted };
            if (descriptor.Manifest is not null && descriptor.Manifest.Tags.Contains("critical"))
            {
                // escalate
            }

            return PluginExecutionResult.Failure(descriptor.Metadata.Name, stopwatch.Elapsed, ex);
        }
    }

    private PluginContext CreatePluginContext(IPluginMetadata metadata, PluginExecutionRequest request)
    {
        var scope = _scopeFactory.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(metadata.Name);
        return new PluginContext(scope, _configuration, logger, metadata, request);
    }

    private static IPluginMetadata ResolveMetadata(Assembly assembly)
    {
        var metadataType = assembly
            .GetTypes()
            .FirstOrDefault(t => typeof(IPluginMetadata).IsAssignableFrom(t) && t is { IsInterface: false, IsAbstract: false });

        if (metadataType is null)
        {
            throw new InvalidOperationException($"Assembly {assembly.FullName} does not expose an IPluginMetadata implementation.");
        }

        if (Activator.CreateInstance(metadataType) is not IPluginMetadata metadataInstance)
        {
            throw new InvalidOperationException($"Unable to instantiate metadata {metadataType.FullName}.");
        }

        return metadataInstance;
    }

    private static PluginManifest? TryLoadManifest(DirectoryInfo? directory)
    {
        if (directory is null)
        {
            return null;
        }

        var manifestFile = Path.Combine(directory.FullName, "PluginManifest.json");
        if (!File.Exists(manifestFile))
        {
            return null;
        }

        var json = File.ReadAllText(manifestFile);
        return System.Text.Json.JsonSerializer.Deserialize<PluginManifest>(json);
    }

    private static bool MatchesFilter(IPluginMetadata metadata, PluginFilter? filter)
    {
        if (filter is null)
        {
            return true;
        }

        if (filter.Tags is not null && filter.Tags.Any() && !filter.Tags.Intersect(metadata.Tags, StringComparer.OrdinalIgnoreCase).Any())
        {
            return false;
        }

        if (filter.Version is not null && !string.Equals(metadata.Version, filter.Version, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (filter.Predicate is not null && !filter.Predicate(metadata))
        {
            return false;
        }

        return true;
    }

    private static string PluginId(IPluginMetadata metadata) => $"{metadata.Name}@{metadata.Version}";

    private void OnPluginChanged(object sender, FileSystemEventArgs e)
    {
        if (!_options.EnableHotReload)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var descriptor = _descriptors.Values.FirstOrDefault(d => string.Equals(d.AssemblyPath, e.FullPath, StringComparison.OrdinalIgnoreCase));
                if (descriptor is not null)
                {
                    await ReloadAsync(descriptor.PluginId);
                }
                else if (e.ChangeType is WatcherChangeTypes.Created or WatcherChangeTypes.Changed)
                {
                    await LoadAsync(e.FullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hot reload failed for {Path}", e.FullPath);
            }
        });
    }

    private void OnPluginRenamed(object sender, RenamedEventArgs e)
    {
        if (!_options.EnableHotReload)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await LoadAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload renamed plugin {Path}", e.FullPath);
            }
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _watcher?.Dispose();
        _lock.Dispose();
    }
}

