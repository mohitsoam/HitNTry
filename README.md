# HitNTry

Quick usage notes for invoking plugins from the host.

**Invoking Plugins With Properties**

Host code can invoke a loaded plugin and pass arbitrary properties which the plugin can read via `IPluginContext.Request.Properties`.

Copyable example:

```csharp
// Resolve the plugin manager from DI
var pluginManager = serviceProvider.GetRequiredService<HitNTry.Framework.Abstractions.IPluginManager>();

// Make a properties dictionary that the plugin will read
var properties = new Dictionary<string, string>
{
    ["input"] = "hello",
    ["useFramework"] = "true"
};

// Create the execution request including properties
var request = new HitNTry.PluginContracts.PluginExecutionRequest(Properties: properties);

// Either invoke by plugin id, or by filter. Example: invoke single plugin by id
var pluginId = "SampleDataIngestion@1.0.0"; // use the plugin's metadata Name@Version
var result = await pluginManager.ExecuteAsync(pluginId, request);

if (result.Succeeded)
{
    Console.WriteLine("Plugin executed successfully");
}
else
{
    Console.WriteLine($"Plugin failed: {result.Error}");
}
```

What the plugin sees
- In plugin code you can read `context.Request.Properties` (may be null) and react accordingly.
- Example (inside plugin `ExecuteAsync`):

```csharp
var props = context.Request.Properties ?? new Dictionary<string, string>();
if (props.TryGetValue("input", out var input))
{
    logger.LogInformation("Plugin received input: {Input}", input);
}
```

Notes
- The host's `IPluginManager` will create a scoped `IPluginContext` for each plugin invocation; plugins should use `context.GetRequiredService<T>()` to access services registered in the host scope.
- The framework exposes a shared business service interface `HitNTry.PluginContracts.IHitNTryBusinessService` which plugins can call (or the host can call plugins). See `src/HitNTry.PluginContracts` and `src/HitNTry.Framework` for examples.

Summary:

Command: dotnet build c:\git\HitNTry\HitNTry.sln

Run pwsh command? (background terminal):
$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run --project "src\HitNTry.Dashboard\HitNTry.Dashboard.csproj"

Set-Location -Path 'C:\git\HitNTry'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project 'src\HitNTry.Dashboard\HitNTry.Dashboard.csproj'