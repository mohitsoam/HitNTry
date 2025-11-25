namespace HitNTry.Orchestration;

public sealed class PluginSchedulerOptions
{
    public const string SectionName = "HitNTry:Scheduler";
    public List<PluginSchedule> Schedules { get; set; } = new();
}

public sealed class PluginSchedule
{
    public string? PluginId { get; set; }
    public IReadOnlyCollection<string>? Tags { get; set; }
    public string? Cron { get; set; }
    public TimeSpan? Interval { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}

