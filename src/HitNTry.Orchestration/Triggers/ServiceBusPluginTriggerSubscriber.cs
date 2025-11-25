using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HitNTry.Orchestration.Triggers;

internal sealed class ServiceBusPluginTriggerSubscriber : IHostedService
{
    private readonly ServiceBusClient _client;
    private readonly IPluginTriggerBus _bus;
    private readonly PluginTriggerOptions _options;
    private readonly ILogger<ServiceBusPluginTriggerSubscriber> _logger;
    private ServiceBusProcessor? _processor;

    public ServiceBusPluginTriggerSubscriber(
        ServiceBusClient client,
        IPluginTriggerBus bus,
        IOptions<PluginTriggerOptions> options,
        ILogger<ServiceBusPluginTriggerSubscriber> logger)
    {
        _client = client;
        _bus = bus;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _processor = _client.CreateProcessor(_options.ServiceBusQueue, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = true,
                MaxConcurrentCalls = 1
            });
            _processor.ProcessMessageAsync += HandleMessageAsync;
            _processor.ProcessErrorAsync += args =>
            {
                _logger.LogError(args.Exception, "ServiceBus trigger failure: {ErrorSource}", args.ErrorSource);
                return Task.CompletedTask;
            };
            await _processor.StartProcessingAsync(cancellationToken);
            _logger.LogInformation("Listening for Service Bus triggers on queue {Queue}", _options.ServiceBusQueue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to start ServiceBus processor. The orchestrator will continue without ServiceBus triggers.");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var payload = args.Message.Body.ToString();
        var trigger = new PluginTriggerEvent(
            PluginTriggerSource.ServiceBus,
            PluginIdFromPayload(payload),
            null,
            payload);
        await _bus.PublishAsync(trigger, args.CancellationToken);
    }

    private static string? PluginIdFromPayload(string payload)
        => string.IsNullOrWhiteSpace(payload)
            ? null
            : payload.Split(':', 2, StringSplitOptions.TrimEntries)[0];
}

