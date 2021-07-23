using Microsoft.Extensions.DependencyInjection;

namespace ExampleReports.Configuration
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
            Emmersion.EventLogWalker.Configuration.DependencyInjectionConfig.ConfigureServices(services);

            services.AddSingleton<IConsumerSettings, ConsumerSettings>();

            //REQUIRED: Must implement and register IInsightsSystemApiSettings
            services.AddSingleton(x => x.GetRequiredService<IConsumerSettings>().InsightsSystemApiSettings);
        }
    }
}
