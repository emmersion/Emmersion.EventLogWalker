using Microsoft.Extensions.DependencyInjection;

namespace Emmersion.EventLogWalker.Package.Configuration
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

            Emmersion.Http.DependencyInjectionConfig.ConfigureServices(services);
        }
    }
}
