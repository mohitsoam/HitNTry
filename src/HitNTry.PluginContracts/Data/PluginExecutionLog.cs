namespace HitNTry.PluginContracts.Data;

public sealed record PluginExecutionLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string PluginName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? CorrelationId { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? OutputPayload { get; set; }
    public string? Error { get; set; }
    public string Tags { get; set; } = string.Empty;
}

