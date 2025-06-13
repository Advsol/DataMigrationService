using System.Security.Claims;

namespace Asi.DataMigrationService.Core
{
    public interface IServiceContext
    {
        ClaimsIdentity Identity { get; }
        string TenantId { get; }
        string UserId { get; }
        string UserName { get; }
    }
}