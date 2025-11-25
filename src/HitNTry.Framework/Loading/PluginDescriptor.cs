using System.Runtime.Loader;
using HitNTry.PluginContracts;

namespace HitNTry.Framework.Loading;

public sealed record PluginDescriptor(
    string PluginId,
    string AssemblyPath,
    DateTimeOffset LoadedAt,
    IPluginMetadata Metadata,
    Type ModuleType,
    PluginState State,
    PluginManifest? Manifest,
    AssemblyLoadContext LoadContext);

