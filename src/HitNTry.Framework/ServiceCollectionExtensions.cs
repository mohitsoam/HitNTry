using System.Data;
using Azure.Messaging.ServiceBus;
using Confluent.Kafka;
using HitNTry.Framework.Abstractions;
using HitNTry.PluginContracts.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HitNTry.Framework;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHitNTryFramework(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<PluginRuntimeOptions>? configure = null)
    {
        services.AddOptions<PluginRuntimeOptions>()
            .Bind(configuration.GetSection("HitNTry:Runtime"))
            .PostConfigure(options => configure?.Invoke(options));

        services.TryAddScoped<AppDbContext>(sp =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseInMemoryDatabase("HitNTry");
            return new AppDbContext(optionsBuilder.Options);
        });

        services.TryAddSingleton<IDbConnection>(_ =>
        {
            var connectionString = configuration.GetConnectionString("Sql") ??
                                   "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;";
            return new SqlConnection(connectionString);
        });

        services.TryAddSingleton(sp =>
        {
            var connectionString = configuration.GetConnectionString("ServiceBus") ??
                                   "Endpoint=sb://localhost/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=local";
            return new ServiceBusClient(connectionString);
        });

        services.TryAddSingleton<IProducer<Null, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
                ClientId = "HitNTry.Host"
            };
            return new ProducerBuilder<Null, string>(config)
                .SetErrorHandler((_, error) =>
                {
                    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Kafka");
                    logger.LogWarning("Kafka producer error: {Error}", error);
                })
                .Build();
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var configurationOptions = ConfigurationOptions.Parse(
                configuration.GetConnectionString("Redis") ?? "localhost:6379",
                true);
            configurationOptions.AbortOnConnectFail = false;
            configurationOptions.ConnectRetry = 1;
            configurationOptions.ConnectTimeout = 2000;
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddHostedService<PluginWarmupHostedService>();

        // Framework common business service available to host and plugins.
        services.TryAddSingleton<HitNTry.PluginContracts.IHitNTryBusinessService, HitNTryBusinessService>();

        return services;
    }
}

