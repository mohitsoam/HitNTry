using HitNTry.Dashboard.Api;
using HitNTry.Dashboard.Components;
using HitNTry.Dashboard.Services;
using HitNTry.Framework;
using HitNTry.Orchestration;
using HitNTry.PluginContracts.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("HitNTry"));

builder.Services.AddHitNTryFramework(builder.Configuration, options =>
{
    options.PluginRootPath = Path.Combine(builder.Environment.ContentRootPath, "Plugins");
});
builder.Services.AddHitNTryOrchestration(builder.Configuration);
builder.Services.AddScoped<PluginDashboardService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGroup("/api/plugins")
    .MapPluginsApi();

app.Run();
