using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace Asi.DataMigrationService.Core.Client
{
    public static class AsiCoreClientExtensions
    {
        public static IServiceCollection AddClient(this IServiceCollection services)
        {
            services.AddTransient<ProxyGenerator>();
            services.AddTransient<ICommonServiceHttpClientFactory, CommonServiceHttpClientFactory>();
            services.AddTransient<ISecureHttpClientFactory, SecureHttpClientFactory>();
            return services;
        }
    }
}