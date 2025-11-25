using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HitNTry.Orchestration.Triggers;

internal sealed class KafkaPluginTriggerSubscriber : BackgroundService
{
    private readonly IPluginTriggerBus _bus;
    private readonly PluginTriggerOptions _options;
    private readonly ILogger<KafkaPluginTriggerSubscriber> _logger;
    private readonly ConsumerConfig _config;

    public KafkaPluginTriggerSubscriber(
        IPluginTriggerBus bus,
        IOptions<PluginTriggerOptions> options,
        ILogger<KafkaPluginTriggerSubscriber> logger,
        IConfiguration configuration)
    {
        _bus = bus;
        _options = options.Value;
        _logger = logger;
        _config = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _options.KafkaGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<Ignore, string>(_config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogWarning("Kafka consumer error: {Error}", error);
            })
            .Build();

        try
        {
            consumer.Subscribe(_options.KafkaTopic);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka trigger listener failed to subscribe. The orchestrator will keep running.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message is null)
                {
                    continue;
                }

                var trigger = new PluginTriggerEvent(
                    PluginTriggerSource.Kafka,
                    PluginIdFromPayload(result.Message.Value),
                    null,
                    result.Message.Value);
                await _bus.PublishAsync(trigger, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kafka trigger polling failed, retrying...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        consumer.Close();
    }

    private static string? PluginIdFromPayload(string payload)
        => string.IsNullOrWhiteSpace(payload)
            ? null
            : payload.Split(':', 2, StringSplitOptions.TrimEntries)[0];
}

