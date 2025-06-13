using System.Security.Claims;
using System.Security.Principal;

namespace Asi.DataMigrationService.Lib
{
    public static class MigrationUtility
    {
        public static string GetTenant(IIdentity identity)
        {
            var claimsIdentity = identity as ClaimsIdentity;
            var name = claimsIdentity?.Name ?? "anonymous@public.com";
            var parts = name.Split('@');
            return parts.Length == 2 ? parts[1].ToLowerInvariant() : null;
        }
    }
}
