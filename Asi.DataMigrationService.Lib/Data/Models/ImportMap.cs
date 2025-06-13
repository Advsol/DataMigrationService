using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    public class ImportMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ImportMapId { get; set; }
        [StringLength(50)]
        [Required]
        public string TenantId { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public string MapInfo { get; set; }
    }
}
