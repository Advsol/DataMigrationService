using Asi.DataMigrationService.Lib.Publisher;
using Microsoft.Extensions.DependencyInjection;

namespace Asi.DataMigrationService.ComponentLib
{
    public static class DataMigrationServiceComponentLibExtensions
    {
        public static IServiceCollection AddDataMigrationServiceComponentLib(this IServiceCollection services)
        {
            services.AddTransient<StandardImportDataSourceComponent>();
            services.Scan(scan => scan
                .FromCallingAssembly()
                    .AddClasses(classes => classes.AssignableTo<IDataSourcePublisher>())
                        .AsSelf()
                        .AsImplementedInterfaces()
                        .WithTransientLifetime()
            );
            return services;
        }
    }
}