using System.Collections.Generic;
using System.Data;
using System.Linq;
using HitNTry.Framework;
using HitNTry.Framework.Abstractions;
using HitNTry.PluginContracts;
using HitNTry.PluginContracts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace HitNTry.Framework.Tests;

public sealed class PluginManagerTests : IAsyncDisposable
{
    private readonly string _pluginRoot = Path.Combine(Path.GetTempPath(), "HitNTryTests", Guid.NewGuid().ToString("N"));
    private readonly ServiceProvider _serviceProvider;

    public PluginManagerTests()
    {
        Directory.CreateDirectory(_pluginRoot);
        CopySamplePluginArtifacts(_pluginRoot);
        _serviceProvider = BuildServices(_pluginRoot);
    }

    [Fact]
    public async Task PluginManager_Loads_And_Executes_Sample_Plugin()
    {
        using var manager = new PluginManager(
            _serviceProvider.GetRequiredService<IOptions<PluginRuntimeOptions>>(),
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _serviceProvider.GetRequiredService<IConfiguration>(),
            _serviceProvider.GetRequiredService<ILogger<PluginManager>>());

        Assert.NotEmpty(Directory.GetFiles(_pluginRoot, "*.dll", SearchOption.AllDirectories));

        await manager.InitializeAsync();
        var descriptors = manager.GetDescriptors();
        Assert.NotEmpty(descriptors);

        var descriptor = descriptors.First();
        var result = await manager.ExecuteAsync(descriptor.PluginId, new PluginExecutionRequest());
        Assert.True(result.Succeeded);

        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        Assert.NotEmpty(await dbContext.ExecutionLogs.ToListAsync());
    }

    private static ServiceProvider BuildServices(string pluginRoot)
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.AddConsole();
        });

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("HitNTryTests"));

        // No external transport dependencies required for these tests.

        var dbMock = new Mock<IDbConnection>();
        dbMock.SetupGet(db => db.ConnectionString).Returns("Server=(localdb)\\MSSQLLocalDB;");
        services.AddSingleton(dbMock.Object);

        services.Configure<PluginRuntimeOptions>(options =>
        {
            options.PluginRootPath = pluginRoot;
            options.WatchForChanges = false;
            options.EnableHotReload = false;
        });

        return services.BuildServiceProvider();
    }

    private static void CopySamplePluginArtifacts(string destination)
    {
        var configuration = Environment.GetEnvironmentVariable("Configuration") ?? "Debug";
#if !DEBUG
        configuration = "Release";
#endif
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var source = Path.Combine(solutionRoot, "plugins", "HitNTry.SamplePlugin", "bin", configuration, "net8.0");
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Sample plugin output not found at {source}. Build the solution before running tests.");
        }

        foreach (var file in Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destinationPath = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Directory.Exists(_pluginRoot))
        {
            await Task.Run(() => Directory.Delete(_pluginRoot, recursive: true));
        }

        await _serviceProvider.DisposeAsync();
    }
}

