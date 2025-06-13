using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    [Table("ProjectImport")]
    public class ProjectImport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectImportId { get; set; }
        public int ProjectDataSourceId { get; set; }
        [MaxLength(150)]
        [Required]
        public string Name { get; set; }
        public string PropertyNames { get; set; }
        public ICollection<ProjectImportData> Data {get; set;}
        public ProjectDataSource ProjectDataSource { get; set; }
        public IList<string> GetPropertyNameList()
        {
            return PropertyNames?.Split(',') ?? (IList<string>)new List<string>();
        }
        public void SetPropertyNames(IEnumerable<string> propertyNames)
        {
            PropertyNames = string.Join(',', propertyNames);
        }
    }
}
