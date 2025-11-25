namespace HitNTry.PluginContracts;

public sealed record BusinessRequest(
    string? PluginId = null,
    string? Action = null,
    IReadOnlyDictionary<string, string>? Properties = null);
