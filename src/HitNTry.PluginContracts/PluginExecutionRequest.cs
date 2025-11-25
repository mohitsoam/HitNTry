namespace HitNTry.PluginContracts;

/// <summary>
/// Rich execution context supplied by the orchestrator when a plugin runs.
/// </summary>
public sealed record PluginExecutionRequest(
    string? CorrelationId = null,
    IReadOnlyCollection<string>? Tags = null,
    string? Version = null,
    IReadOnlyDictionary<string, string>? Properties = null);

