using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    [Table("Tenant")]
    public class Tenant
    {
        [Key]
        [StringLength(50)]
        public string TenantId { get; set; }
        [StringLength(100)]
        public string Name { get; set; }
    }
}
