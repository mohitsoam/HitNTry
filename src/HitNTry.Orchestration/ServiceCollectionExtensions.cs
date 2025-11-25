using HitNTry.Orchestration.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HitNTry.Orchestration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHitNTryOrchestration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPluginTriggerBus, PluginTriggerBus>();
        services.Configure<PluginSchedulerOptions>(configuration.GetSection(PluginSchedulerOptions.SectionName));
        services.Configure<PluginTriggerOptions>(configuration.GetSection(PluginTriggerOptions.SectionName));
        services.AddSingleton<IPluginExecutionOrchestrator, PluginExecutionOrchestrator>();
        services.AddHostedService<PluginOrchestrationBackgroundService>();
        services.AddHostedService<RedisPluginTriggerSubscriber>();
        services.AddHostedService<ServiceBusPluginTriggerSubscriber>();
        services.AddHostedService<KafkaPluginTriggerSubscriber>();
        return services;
    }
}

