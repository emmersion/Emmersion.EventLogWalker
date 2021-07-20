using System;
using System.Threading.Tasks;
using Emmersion.EventLogWalker.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace Emmersion.EventLogWalker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = ConfigureServices();

            await ComplexReport(services);
            //await SimpleReport(services);
        }

        private static async Task ComplexReport(ServiceProvider services)
        {
            var report = services.GetRequiredService<IAccountUserCountsReport>();

            var startInclusive = DateTimeOffset.Parse("2021-06-01");
            var maxEndExclusive = DateTimeOffset.Parse("2021-08-01");

            await report.GenerateAsync(startInclusive, maxEndExclusive);
        }
        
        private static async Task SimpleReport(ServiceProvider services)
        {
            var report = services.GetRequiredService<ISimplestReport>();

            await report.GenerateAsync();
        }

        private static ServiceProvider ConfigureServices()
        {
            var serviceCollection = new ServiceCollection();
            Consumer.Configuration.DependencyInjectionConfig.ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
