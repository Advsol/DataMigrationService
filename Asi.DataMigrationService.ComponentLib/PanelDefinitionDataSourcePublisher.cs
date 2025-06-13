using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Asi.DataMigrationService.ComponentLib
{
    
    internal class PanelDefinitionDataSourcePublisher : DataSourcePublisherBase
    {
        public PanelDefinitionDataSourcePublisher(Core.Client.ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "PanelDefinition";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party", "PanelData" };

        public override string EntityTypeName => "PanelDefinition";
        public override bool IsHarvester => true;
        public override bool IsValidatable => false;

        public override string Title => "iMIS Panel Definitions";

        public override Type UIComponentType => typeof(StandardExtractDataSourceComponent);

        protected override NameValueCollection GetDescriptiveFields(SourceDataInfo sourceInfo = null)
        {
            var data = sourceInfo?.Data as PanelDefinitionData;
            return new NameValueCollection()
            {
                {"Panel Name", data?.Name }
            };
        }
    }
}