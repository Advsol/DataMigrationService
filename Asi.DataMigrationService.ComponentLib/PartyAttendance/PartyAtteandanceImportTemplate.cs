using System;
using System.ComponentModel.DataAnnotations;
using Asi.DataMigrationService.Lib.Publisher;

namespace Asi.DataMigrationService.ComponentLib.PartyAttendance
{
    public class PartyAttendanceImportTemplate : ImportTemplate
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string AttendanceTypeCode { get; set; }
        [Required]
        public DateTime? AttendanceDate { get; set; }
        [Required]
        public string OrganizationName { get; set; }
        public string Description { get; set; }
        public bool IsCheckedIn { get; set; }
    }
}