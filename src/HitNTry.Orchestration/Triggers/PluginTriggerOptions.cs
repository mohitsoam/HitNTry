namespace HitNTry.Orchestration.Triggers;

public sealed class PluginTriggerOptions
{
    public const string SectionName = "HitNTry:Triggers";
    public string RedisChannel { get; set; } = "hitntry.plugins";
    public string ServiceBusQueue { get; set; } = "hitntry-plugins";
    public string KafkaTopic { get; set; } = "hitntry.plugins";
    public string KafkaGroupId { get; set; } = "hitntry-host";
}

