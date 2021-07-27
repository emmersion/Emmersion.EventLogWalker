using Emmersion.EventLogWalker.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Emmersion.EventLogWalker.Configuration
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

            services.AddSingleton<IHttpClient, HttpClient>();

        }
    }
}
