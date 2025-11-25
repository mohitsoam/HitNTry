using Microsoft.EntityFrameworkCore;

namespace HitNTry.PluginContracts.Data;

/// <summary>
/// Minimal EF Core context shared between host and plugins.
/// Consumers can extend via partial classes or model configuration.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PluginExecutionLog> ExecutionLogs => Set<PluginExecutionLog>();
}

