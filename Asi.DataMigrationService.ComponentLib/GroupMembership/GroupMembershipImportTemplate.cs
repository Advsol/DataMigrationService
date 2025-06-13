using Asi.DataMigrationService.Lib.Publisher;
using System;
using System.ComponentModel.DataAnnotations;

namespace Asi.DataMigrationService.ComponentLib.GroupMembership
{
    //Class dependent on GroupMembershipDataSourcePublisher, which is unused
    public class GroupMembershipImportTemplate : ImportTemplate
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string GroupName { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string Role { get; set; }
    }
}