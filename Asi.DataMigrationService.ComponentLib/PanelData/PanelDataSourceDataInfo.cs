using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;

namespace Asi.DataMigrationService.ComponentLib.PanelData
{
    public class PanelDataSourceDataInfo : SourceDataInfo
    {
        public bool IncludeData { get; set; }
        public bool Replace { get; set; }
        public bool CanIncludeData => true;
        public new string Name => BOEntityDefinition.EntityTypeName;
        public BOEntityDefinitionData BOEntityDefinition => (BOEntityDefinitionData)Data;
    }
}