using System;
using System.IO;
using System.Reflection;
using Emmersion.EventLogWalker.Configuration;
using Microsoft.Extensions.Configuration;

namespace ExampleConsumers.Configuration
{
    public interface IConsumerSettings
    {
        IInsightsSystemApiSettings InsightsSystemApiSettings { get; }
    }

    public class ConsumerSettings : IConsumerSettings
    {
        private static readonly string assemblyName = Assembly.GetAssembly(typeof(ConsumerSettings)).GetName().Name;
        private readonly IConfigurationRoot configurationRoot;

        public ConsumerSettings()
        {
            var localConfigOverridesPath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "truenorth-overrides/insights", "concept-appSettings.json");
            configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile(localConfigOverridesPath, optional: true)
                .Build();
        }

        public IInsightsSystemApiSettings InsightsSystemApiSettings => new ConsumerInsightsSystemApiSettings
        {
            BaseUrl = configurationRoot.GetValue<string>("InsightsSystemApi:BaseUrl"),
            ApiKey = configurationRoot.GetValue<string>("InsightsSystemApi:ApiKey")
        };
    }

    public class ConsumerInsightsSystemApiSettings : IInsightsSystemApiSettings
    {
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
