using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;

namespace Asi.DataMigrationService.ComponentLib.Document
{
    public class DocumentSourceDataInfo : SourceDataInfo
    {
        public DocumentSummaryData Document => (DocumentSummaryData)Data;
        public string AlternateName => Document.AlternateName?.NullTrim() ?? Document.Name;
        public bool IsSelectedByParent { get; set; }
        public new string Id => Document.DocumentVersionId;
        public bool IsSystem => Document.IsSystem;
    }
}