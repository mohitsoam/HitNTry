using HitNTry.Dashboard.Services;
using HitNTry.PluginContracts;
using Microsoft.AspNetCore.Mvc;

namespace HitNTry.Dashboard.Api;

public static class PluginEndpoints
{
    public static RouteGroupBuilder MapPluginsApi(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (PluginDashboardService service)
            => Results.Ok(await service.GetPluginsAsync()));

        group.MapPost("/{pluginId}/execute", async ([FromRoute] string pluginId, PluginDashboardService service)
            => Results.Ok(await service.ExecutePluginAsync(pluginId)));

        group.MapPost("/execute/by-tags", async ([FromBody] string[] tags, PluginDashboardService service)
            => Results.Ok(await service.ExecuteFilteredAsync(tags)));

        group.MapPost("/{pluginId}/reload", async ([FromRoute] string pluginId, PluginDashboardService service) =>
        {
            await service.ReloadAsync(pluginId);
            return Results.Ok();
        });

        group.MapDelete("/{pluginId}", async ([FromRoute] string pluginId, PluginDashboardService service) =>
        {
            await service.UnloadAsync(pluginId);
            return Results.Ok();
        });

        group.MapPost("/triggers", async ([FromBody] ManualTriggerRequest request, PluginDashboardService service) =>
        {
            await service.PublishTriggerAsync(request.PluginId, request.Tags, request.Payload);
            return Results.Accepted();
        });

        group.MapGet("/logs", async (PluginDashboardService service)
            => Results.Ok(await service.GetExecutionLogsAsync()));

        return group;
    }

    public sealed record ManualTriggerRequest(string? PluginId, IReadOnlyCollection<string>? Tags, string? Payload);
}

