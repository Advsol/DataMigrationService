using Asi.DataMigrationService.Lib.Publisher;
using System;
using System.ComponentModel.DataAnnotations;

namespace Asi.DataMigrationService.ComponentLib.GiftAid
{
    //Class dependent on GiftAidDataSourcePublisher, which is unused
    public class GiftAidImportTemplate : ImportTemplate
    {
        [Required]
        public string Id { get; set; }
        [Required]
        public DateTime? DeclarationReceived { get; set; }
        public DateTime? ConfirmationLetterSent { get; set; }
        [Required]
        public string MethodOfDeclaration { get; set; }
        public bool Future { get; set; }
        public bool Past { get; set; }
        public string DeclarationNotes { get; set; }
    }
}