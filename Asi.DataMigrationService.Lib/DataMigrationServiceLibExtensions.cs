using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.Hub;
using Asi.DataMigrationService.Lib.Queries;
using Asi.DataMigrationService.Lib.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using System;

namespace Asi.DataMigrationService.Lib
{
    public static class DataMigrationServiceLibExtensions
    {
        public static IServiceCollection AddDataMigrationServiceLib(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDataMigrationServiceLibCore();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        public static IServiceCollection AddDataMigrationServiceLibCore(this IServiceCollection services)
        {
            services.AddTransient<IProjectQueries, ProjectQueries>()
                    .AddTransient<IPublishService, PublishService>()
                    .AddTransient(provider => new Lazy<IPublishService>(provider.GetRequiredService<IPublishService>))
                    .AddSingleton<DataMigrationServiceBackgroundService>()
                    .AddTransient<PublishJob>()
                    .Scan(scan => scan.FromType<ProjectService>().AsImplementedInterfaces());

            return services;
        }
    }
}