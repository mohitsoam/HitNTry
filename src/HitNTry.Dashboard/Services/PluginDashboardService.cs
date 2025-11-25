using HitNTry.Framework.Abstractions;
using HitNTry.Framework.Loading;
using HitNTry.Orchestration;
using HitNTry.Orchestration.Triggers;
using HitNTry.PluginContracts;
using HitNTry.PluginContracts.Data;
using Microsoft.EntityFrameworkCore;

namespace HitNTry.Dashboard.Services;

public sealed class PluginDashboardService
{
    private readonly IPluginManager _pluginManager;
    private readonly IPluginExecutionOrchestrator _orchestrator;
    private readonly IPluginTriggerBus _triggerBus;
    private readonly AppDbContext _dbContext;

    public PluginDashboardService(
        IPluginManager pluginManager,
        IPluginExecutionOrchestrator orchestrator,
        IPluginTriggerBus triggerBus,
        AppDbContext dbContext)
    {
        _pluginManager = pluginManager;
        _orchestrator = orchestrator;
        _triggerBus = triggerBus;
        _dbContext = dbContext;
    }

    public Task<IReadOnlyCollection<PluginDescriptor>> GetPluginsAsync()
        => Task.FromResult(_pluginManager.GetDescriptors());

    public Task<PluginExecutionResult> ExecutePluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var request = new PluginExecutionRequest(CorrelationId: Guid.NewGuid().ToString("N"));
        return _orchestrator.ExecuteAsync(pluginId, request, cancellationToken);
    }

    public Task<PluginExecutionResult> ExecutePluginWithPropertiesAsync(string pluginId, IReadOnlyDictionary<string, string>? properties, CancellationToken cancellationToken = default)
    {
        var request = new PluginExecutionRequest(CorrelationId: Guid.NewGuid().ToString("N"), Properties: properties);
        return _orchestrator.ExecuteAsync(pluginId, request, cancellationToken);
    }

    public Task<IReadOnlyCollection<PluginExecutionResult>> ExecuteFilteredAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        var filter = new PluginFilter(tags);
        var request = new PluginExecutionRequest(CorrelationId: Guid.NewGuid().ToString("N"), Tags: tags);
        return _orchestrator.ExecuteAsync(filter, request, cancellationToken);
    }

    public Task ReloadAsync(string pluginId, CancellationToken cancellationToken = default)
        => _pluginManager.ReloadAsync(pluginId, cancellationToken);

    public Task UnloadAsync(string pluginId, CancellationToken cancellationToken = default)
        => _pluginManager.UnloadAsync(pluginId, cancellationToken);

    public Task LoadAsync(string absolutePath, CancellationToken cancellationToken = default)
        => _pluginManager.LoadAsync(absolutePath, cancellationToken);

    public Task PublishTriggerAsync(string? pluginId, IReadOnlyCollection<string>? tags, string? payload = null, CancellationToken cancellationToken = default)
    {
        var trigger = new PluginTriggerEvent(PluginTriggerSource.Manual, pluginId, tags, payload);
        return _triggerBus.PublishAsync(trigger, cancellationToken);
    }

    public Task<List<PluginExecutionLog>> GetExecutionLogsAsync(CancellationToken cancellationToken = default)
        => _dbContext.ExecutionLogs
            .OrderByDescending(log => log.StartedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
}

