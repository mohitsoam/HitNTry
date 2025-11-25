using System.Data;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using HitNTry.PluginContracts;
using HitNTry.PluginContracts.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HitNTry.SamplePlugin;

public sealed class SampleDataIngestionModule : IModule, IPluginLifecycle
{
    public IPluginMetadata Metadata { get; } = new SamplePluginMetadata();

    public async Task ExecuteAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var logger = context.GetRequiredService<ILogger<SampleDataIngestionModule>>();

        // Optional transports — resolve if available so tests without transport clients still run.
        var serviceBus = context.Services.GetService(typeof(Azure.Messaging.ServiceBus.ServiceBusClient)) as Azure.Messaging.ServiceBus.ServiceBusClient;
        var kafkaProducer = context.Services.GetService(typeof(Confluent.Kafka.IProducer<Confluent.Kafka.Null, string>)) as Confluent.Kafka.IProducer<Confluent.Kafka.Null, string>;
        var redis = context.Services.GetService(typeof(StackExchange.Redis.IConnectionMultiplexer)) as StackExchange.Redis.IConnectionMultiplexer;
        var dbConnection = context.Services.GetService(typeof(System.Data.IDbConnection)) as System.Data.IDbConnection;
        var appDb = context.GetRequiredService<AppDbContext>();

        var payload = $"Triggered at {DateTimeOffset.UtcNow:O}";
        logger.LogInformation("Sample plugin executing with payload {Payload}", payload);

        // Demonstrate reading host-provided request properties so plugins can participate
        // in higher-level business calls. The host may pass arbitrary properties; here
        // we look for `input` and `useFramework`.
        var props = context.Request.Properties ?? new Dictionary<string, string>();
        if (props.TryGetValue("input", out var input))
        {
            logger.LogInformation("Plugin received input property: {Input}", input);
        }

        if (props.TryGetValue("useFramework", out var useFramework) &&
            bool.TryParse(useFramework, out var useFrameworkFlag) && useFrameworkFlag)
        {
            // Plugin can call back into framework services. This demonstrates a plugin
            // invoking the shared business service. The framework implementation may
            // forward requests back to plugins or run default logic.
            try
            {
                var business = context.GetRequiredService<HitNTry.PluginContracts.IHitNTryBusinessService>();
                var br = new HitNTry.PluginContracts.BusinessRequest(PluginId: null, Action: "FromPlugin", Properties: props);
                var brResult = await business.ExecuteAsync(br, cancellationToken);
                logger.LogInformation("Framework business call returned: {Success} {Message}", brResult.Success, brResult.Message);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Framework business call failed from plugin");
            }
        }

        // Simulate writing to EF Core
        appDb.ExecutionLogs.Add(new PluginExecutionLog
        {
            PluginName = Metadata.Name,
            Version = Metadata.Version,
            Status = "Sample",
            OutputPayload = payload,
            Tags = string.Join(',', Metadata.Tags)
        });
        await appDb.SaveChangesAsync(cancellationToken);

        // Surface information from the injected brokers without relying on live infrastructure.
        if (kafkaProducer is not null)
        {
            logger.LogInformation("Kafka producer would publish to {Topic}", configuration["Kafka:SampleTopic"]);
        }
        else
        {
            logger.LogInformation("Kafka producer not configured");
        }

        if (serviceBus is not null)
        {
            logger.LogInformation("Service Bus namespace: {Namespace}", serviceBus.FullyQualifiedNamespace);
        }
        else
        {
            logger.LogInformation("Service Bus not configured");
        }

        if (redis is not null)
        {
            logger.LogInformation("Redis configuration: {Config}", redis.Configuration);
        }
        else
        {
            logger.LogInformation("Redis not configured");
        }

        if (dbConnection is not null)
        {
            logger.LogInformation("Database connection: {ConnectionString}", dbConnection.ConnectionString);
        }
        else
        {
            logger.LogInformation("Database connection not configured");
        }
    }
}

public sealed class SamplePluginMetadata : IPluginMetadata
{
    public string Name => "SampleDataIngestion";
    public string Version => "1.0.0";
    public string Author => "HitNTry Team";
    public string Description => "Demonstrates full DI surface area inside a plugin.";
    public IReadOnlyCollection<string> Tags => new[] { "demo", "ingestion", "sample" };
}
