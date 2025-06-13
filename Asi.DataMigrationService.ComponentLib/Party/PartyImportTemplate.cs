using System;
using System.ComponentModel.DataAnnotations;
using Asi.DataMigrationService.ComponentLib.PartyAddress;

namespace Asi.DataMigrationService.ComponentLib.Party
{
    public class PartyImportTemplate : PartyAddressImportTemplate
    {
        public DateTime? BirthDate { get; set; }
        public string Category { get; set; }
        public string Designation { get; set; }
        public string FirstName { get; set; }
        public string Gender { get; set; }
        public string InformalName { get; set; }
        public string LastName { get; set; }
        public string MemberType { get; set; }
        public string MiddleName { get; set; }
        public string MobilePhone { get; set; }
        public string NamePrefix { get; set; }
        public string NameSuffix { get; set; }
        public string NationalGovernmentDistrict { get; set; }
        public string OrganizationName { get; set; }
        [MaxLength(30)]
        [Required]
        public string PartyType { get; set; }
        public string PrimaryClubId { get; set; }
        public string PrimaryOrganizationId { get; set; }
        public string PrimaryOrganizationTitle { get; set; }
        public string ReceiptPreference { get; set; }
        public string Status { get; set; }
    }
}