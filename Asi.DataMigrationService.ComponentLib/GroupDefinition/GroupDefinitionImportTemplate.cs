using Asi.DataMigrationService.Lib.Publisher;
using System.ComponentModel.DataAnnotations;

namespace Asi.DataMigrationService.ComponentLib.GroupDefinition
{
    //Class dependent on GroupDefinitionDataSourcePublisher, which is unused
    public class GroupDefinitionImportTemplate : ImportTemplate
    {
        [Required]
        public string GroupName { get; set; }
        [Required]
        public string GroupClass { get; set; }
        [Required]
        public string Description { get; set; }
        public string GroupOwnerId { get; set; }
    }
}