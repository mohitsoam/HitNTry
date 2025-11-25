using Cronos;
using HitNTry.Framework.Abstractions;
using HitNTry.Orchestration.Triggers;
using HitNTry.PluginContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HitNTry.Orchestration;

public sealed class PluginOrchestrationBackgroundService : BackgroundService
{
    private readonly IPluginManager _pluginManager;
    private readonly PluginSchedulerOptions _schedulerOptions;
    private readonly IPluginTriggerBus _triggerBus;
    private readonly ILogger<PluginOrchestrationBackgroundService> _logger;

    public PluginOrchestrationBackgroundService(
        IPluginManager pluginManager,
        IOptions<PluginSchedulerOptions> schedulerOptions,
        IPluginTriggerBus triggerBus,
        ILogger<PluginOrchestrationBackgroundService> logger)
    {
        _pluginManager = pluginManager;
        _schedulerOptions = schedulerOptions.Value;
        _triggerBus = triggerBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var scheduleTasks = _schedulerOptions.Schedules
            .Where(s => s.Enabled)
            .Select(schedule => RunScheduleAsync(schedule, stoppingToken))
            .ToList();

        scheduleTasks.Add(ListenForTriggersAsync(stoppingToken));

        await Task.WhenAll(scheduleTasks);
    }

    private async Task RunScheduleAsync(PluginSchedule schedule, CancellationToken token)
    {
        if (schedule.Interval is not null)
        {
            await RunIntervalScheduleAsync(schedule, token);
        }
        else if (!string.IsNullOrWhiteSpace(schedule.Cron))
        {
            await RunCronScheduleAsync(schedule, token);
        }
    }

    private async Task RunIntervalScheduleAsync(PluginSchedule schedule, CancellationToken token)
    {
        var timer = new PeriodicTimer(schedule.Interval ?? TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(token))
        {
            await ExecuteScheduleAsync(schedule, token);
        }
    }

    private async Task RunCronScheduleAsync(PluginSchedule schedule, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(schedule.Cron))
        {
            return;
        }

        var cron = CronExpression.Parse(schedule.Cron);
        while (!token.IsCancellationRequested)
        {
            var next = cron.GetNextOccurrence(DateTime.UtcNow);
            if (next is null)
            {
                break;
            }

            var delay = next.Value - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, token);
            }

            await ExecuteScheduleAsync(schedule, token);
        }
    }

    private async Task ExecuteScheduleAsync(PluginSchedule schedule, CancellationToken token)
    {
        try
        {
            var request = new PluginExecutionRequest(
                CorrelationId: Guid.NewGuid().ToString("N"),
                Tags: schedule.Tags,
                Version: null,
                Properties: new Dictionary<string, string>
                {
                    ["Schedule"] = schedule.Description ?? "interval"
                });

            if (!string.IsNullOrWhiteSpace(schedule.PluginId))
            {
                await _pluginManager.ExecuteAsync(schedule.PluginId, request, token);
                return;
            }

            var filter = new PluginFilter(schedule.Tags);
            await _pluginManager.ExecuteAsync(filter, request, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled execution of {Plugin} failed", schedule.PluginId ?? string.Join(',', schedule.Tags ?? Array.Empty<string>()));
        }
    }

    private async Task ListenForTriggersAsync(CancellationToken token)
    {
        await foreach (var trigger in _triggerBus.ListenAsync(token))
        {
            try
            {
                var request = new PluginExecutionRequest(
                    CorrelationId: Guid.NewGuid().ToString("N"),
                    Tags: trigger.Tags,
                    Properties: new Dictionary<string, string>
                    {
                        ["TriggerSource"] = trigger.Source.ToString(),
                        ["Payload"] = trigger.Payload ?? string.Empty
                    });

                if (!string.IsNullOrWhiteSpace(trigger.PluginId))
                {
                    await _pluginManager.ExecuteAsync(trigger.PluginId, request, token);
                }
                else if (trigger.Tags is not null)
                {
                    await _pluginManager.ExecuteAsync(new PluginFilter(trigger.Tags), request, token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Trigger execution failed");
            }
        }
    }
}

