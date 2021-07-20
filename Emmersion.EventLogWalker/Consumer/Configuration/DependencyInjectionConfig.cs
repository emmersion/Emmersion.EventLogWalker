using Microsoft.Extensions.DependencyInjection;

namespace Emmersion.EventLogWalker.Consumer.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.Scan(
                scan =>
                {
                    scan.FromAssembliesOf(typeof(DependencyInjectionConfig)).AddClasses().AsMatchingInterface().WithTransientLifetime();
                }
            );

            //REQUIRED: Must call
            Package.Configuration.DependencyInjectionConfig.ConfigureServices(services);

            services.AddSingleton<IConsumerSettings, ConsumerSettings>();
            services.AddSingleton(x => x.GetRequiredService<IConsumerSettings>().LoggerSettings);

            //REQUIRED: Must implement and register IInsightsSystemApiSettings
            services.AddSingleton(x => x.GetRequiredService<IConsumerSettings>().InsightsSystemApiSettings);
        }
    }
}
