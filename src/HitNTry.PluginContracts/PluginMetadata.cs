namespace HitNTry.PluginContracts;

public sealed record PluginMetadata(
    string Name,
    string Version,
    string Author,
    string Description,
    IReadOnlyCollection<string> Tags) : IPluginMetadata;

