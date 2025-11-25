using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HitNTry.Dashboard.Services;
using HitNTry.Framework.Abstractions;
using HitNTry.Orchestration;
using HitNTry.Orchestration.Triggers;
using HitNTry.PluginContracts;
using HitNTry.PluginContracts.Data;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace HitNTry.Framework.Tests;

public class PluginDashboardServiceTests
{
    [Fact]
    public async Task PublishTriggerAsync_EnqueuesEvent()
    {
        var manager = new Mock<IPluginManager>();
        var orchestrator = new Mock<IPluginExecutionOrchestrator>();
        var bus = new PluginTriggerBus();

        var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

        var service = new PluginDashboardService(manager.Object, orchestrator.Object, bus, dbContext);

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readTask = ReadNextTriggerAsync(bus, cts.Token);

        await service.PublishTriggerAsync("plugin", new[] { "demo" }, "payload", cts.Token);

        var trigger = await readTask;
        Assert.Equal("plugin", trigger.PluginId);
        Assert.Contains("demo", trigger.Tags!);
        Assert.Equal("payload", trigger.Payload);
    }

    [Fact]
    public async Task ExecuteFilteredAsync_DelegatesToOrchestrator()
    {
        var manager = new Mock<IPluginManager>();
        var orchestrator = new Mock<IPluginExecutionOrchestrator>();
        orchestrator
            .Setup(o => o.ExecuteAsync(It.IsAny<PluginFilter>(), It.IsAny<PluginExecutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PluginExecutionResult>());
        var bus = new PluginTriggerBus();

        var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

        var service = new PluginDashboardService(manager.Object, orchestrator.Object, bus, dbContext);

        await service.ExecuteFilteredAsync(new[] { "demo" });

        orchestrator.Verify(o => o.ExecuteAsync(It.Is<PluginFilter>(f => f.Tags!.Contains("demo")), It.IsAny<PluginExecutionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static async Task<PluginTriggerEvent> ReadNextTriggerAsync(IPluginTriggerBus bus, CancellationToken token)
    {
        await foreach (var trigger in bus.ListenAsync(token))
        {
            return trigger;
        }

        throw new InvalidOperationException("No trigger received.");
    }
}

