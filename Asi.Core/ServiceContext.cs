using System.Security.Claims;
using Asi.Core.Interfaces;

namespace Asi.DataMigrationService.Core
{
    public class ServiceContext : IServiceContext
    {
        public ServiceContext(ClaimsIdentity identity)
        {
            Identity = identity;
        }
        public ClaimsIdentity Identity { get; }
        public string UserId => Identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        public string TenantId => Identity.FindFirst(AppClaimTypes.TenantId)?.Value;
        public string UserName => Identity.Name;
    }
}
