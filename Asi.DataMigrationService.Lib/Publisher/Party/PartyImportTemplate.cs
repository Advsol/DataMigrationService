using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Asi.DataMigrationService.Lib.Processor.Party
{
    public class PartyImportTemplate
    {
        [MaxLength(30)]
        [Required]
        public string PartyId { get; set; }
        [Required]
        public string PartyType { get; set; }
        public string Status { get; set; }
        public string MemberType { get; set; }
        public string Category { get; set; }
        public string PrimaryOrganizationId { get; set; }
        public string NamePrefix { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string NameSuffix { get; set; }
        public string InformalName { get; set; }
        public string Designation { get; set; }
        public DateTime? BirthData { get; set; }
        public string Gender { get; set; }
        public string PrimaryOrganizationTitle { get; set; }
        public string AddressPurpose { get; set; }
        public string CommunicationsReasons { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string CityName { get; set; }
        public string CountrySubEntityCode { get; set; }
        public string CountrySubEntityName { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string PostalCode { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string OrganizationName { get; set; }
        public string NationalGovernmentDistrict { get; set; }
        public string LocalGovernmentDistrict1 { get; set; }
        public string LocalGovernmentDistrict2 { get; set; }

        public string PrimaryClubId { get; set; }
    }

}
