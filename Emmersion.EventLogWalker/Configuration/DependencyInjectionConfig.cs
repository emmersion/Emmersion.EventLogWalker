using System.Runtime.CompilerServices;
using Emmersion.EventLogWalker.Http;
using Microsoft.Extensions.DependencyInjection;

[assembly:InternalsVisibleTo("Emmersion.EventLogWalker.UnitTests")]
[assembly:InternalsVisibleTo("Emmersion.EventLogWalker.IntegrationTests")]
[assembly:InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Emmersion.EventLogWalker.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.Scan(
                scan =>
                {
                    scan.FromAssembliesOf(typeof(DependencyInjectionConfig)).AddClasses(publicOnly: false).AsMatchingInterface().WithTransientLifetime();
                }
            );

            services.AddSingleton<IHttpClient, HttpClient>();
            services.AddTransient<IPager, InsightsSystemApiPager>();
        }
    }
}
