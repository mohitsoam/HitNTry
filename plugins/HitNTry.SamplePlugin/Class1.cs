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
        var serviceBus = context.GetRequiredService<ServiceBusClient>();
        var kafkaProducer = context.GetRequiredService<IProducer<Null, string>>();
        var redis = context.GetRequiredService<IConnectionMultiplexer>();
        var dbConnection = context.GetRequiredService<IDbConnection>();
        var appDb = context.GetRequiredService<AppDbContext>();

        var payload = $"Triggered at {DateTimeOffset.UtcNow:O}";
        logger.LogInformation("Sample plugin executing with payload {Payload}", payload);

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
        logger.LogInformation("Kafka producer {Producer} would publish to {Topic}", kafkaProducer.Name, configuration["Kafka:SampleTopic"]);
        logger.LogInformation("Service Bus namespace: {Namespace}", serviceBus.FullyQualifiedNamespace);
        logger.LogInformation("Redis configuration: {Config}", redis.Configuration);
        logger.LogInformation("Database connection: {ConnectionString}", dbConnection.ConnectionString);
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
