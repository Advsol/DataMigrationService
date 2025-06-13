using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    [Table("ProjectDataSource")]
    public class ProjectDataSource
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectDataSourceId { get; set; }
        public string ProjectId { get; set; }
        [MaxLength(100)]
        [Required]
        public string Name { get; set; }
        [Required]
        [MaxLength(50)]
        public string DataSourceType { get; set; }
        public string Data { get; set; }
        public Project Project { get; set; }
        public ICollection<ProjectImport> Imports { get; set; }
    }
}
