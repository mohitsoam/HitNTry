namespace HitNTry.PluginContracts;

/// <summary>
/// Describes a plugin so that hosts can reason about identity, authorship, and capabilities.
/// </summary>
public interface IPluginMetadata
{
    string Name { get; }
    string Version { get; }
    string Author { get; }
    string Description { get; }
    IReadOnlyCollection<string> Tags { get; }
}

