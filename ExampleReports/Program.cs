using System;
using System.Threading.Tasks;
using ExampleReports.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleReports
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = ConfigureServices();

            await ComplexReport(services);
            //await SimpleReport(services);
            //await ResumeValidationReport(services);
        }

        private static async Task ComplexReport(ServiceProvider services)
        {
            var report = services.GetRequiredService<IAccountUserCountsReport>();

            var startInclusive = DateTimeOffset.Parse("2020-07-01");
            var maxEndExclusive = DateTimeOffset.Parse("2020-07-02");

            await report.GenerateAsync(startInclusive, maxEndExclusive);
        }
        
        private static async Task SimpleReport(ServiceProvider services)
        {
            var report = services.GetRequiredService<ISimpleReport>();

            await report.GenerateAsync();
        }

        private static async Task ResumeValidationReport(ServiceProvider services)
        {
            var report = services.GetRequiredService<IResumeValidationReport>();

            await report.GenerateAsync();
        }

        private static ServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            DependencyInjectionConfig.ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
