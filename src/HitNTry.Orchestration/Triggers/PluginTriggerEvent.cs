namespace HitNTry.Orchestration.Triggers;

public enum PluginTriggerSource
{
    Manual,
    Redis,
    Kafka,
    ServiceBus,
    Unknown
}

public sealed record PluginTriggerEvent(
    PluginTriggerSource Source,
    string? PluginId = null,
    IReadOnlyCollection<string>? Tags = null,
    string? Payload = null);

