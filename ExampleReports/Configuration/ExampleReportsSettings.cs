using System;
using System.IO;
using Emmersion.EventLogWalker.Configuration;
using Microsoft.Extensions.Configuration;

namespace ExampleReports.Configuration
{
    public interface IExampleReportsSettings
    {
        IInsightsSystemApiSettings InsightsSystemApiSettings { get; }
    }

    public class ExampleReportsSettings : IExampleReportsSettings
    {
        private readonly IConfigurationRoot configurationRoot;

        public ExampleReportsSettings()
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
