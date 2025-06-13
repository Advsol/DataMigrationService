using System.ComponentModel.DataAnnotations;
using Asi.DataMigrationService.Lib.Publisher;

namespace Asi.DataMigrationService.ComponentLib.PartyAddress
{
    public class PartyAddressImportTemplate : ImportTemplate
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string AddressPurpose { get; set; }
        [Required]
        public string CityName { get; set; }
        public string CommunicationReasons { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        [Required]
        public string CountrySubEntityCode { get; set; }
        public string CountrySubEntityName { get; set; }
        public string Email { get; set; }
        public string Fax { get; set; }
        public string LocalGovernmentDistrict1 { get; set; }
        public string LocalGovernmentDistrict2 { get; set; }
        public string Phone { get; set; }
        [Required]
        public string PostalCode { get; set; }
    }
}