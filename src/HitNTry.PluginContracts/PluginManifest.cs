using System.Text.Json.Serialization;

namespace HitNTry.PluginContracts;

/// <summary>
/// Optional manifest that can travel with plugins (PluginManifest.json).
/// </summary>
public sealed record PluginManifest
{
    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; init; } = string.Empty;

    [JsonPropertyName("packageId")]
    public string PackageId { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; init; } = string.Empty;

    [JsonPropertyName("tags")]
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;
}

