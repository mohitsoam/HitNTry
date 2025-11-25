namespace HitNTry.PluginContracts;

public sealed record PluginFilter(
    IReadOnlyCollection<string>? Tags = null,
    string? Version = null,
    Func<IPluginMetadata, bool>? Predicate = null);

