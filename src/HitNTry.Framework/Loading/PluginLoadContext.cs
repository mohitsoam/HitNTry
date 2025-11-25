using System.Reflection;
using System.Runtime.Loader;

namespace HitNTry.Framework.Loading;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly HashSet<string> _sharedAssemblies;

    public PluginLoadContext(string pluginPath, IEnumerable<string> sharedAssemblies)
        : base($"HitNTry_{Path.GetFileNameWithoutExtension(pluginPath)}", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
        _sharedAssemblies = new HashSet<string>(sharedAssemblies, StringComparer.OrdinalIgnoreCase);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (_sharedAssemblies.Contains(assemblyName.Name ?? string.Empty))
        {
            return null;
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? base.LoadUnmanagedDll(unmanagedDllName) : LoadUnmanagedDllFromPath(path);
    }
}

