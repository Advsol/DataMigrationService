using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Communications.DataContracts;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Asi.DataMigrationService.ComponentLib
{
    
    internal class TaskDefinitionDataSourcePublisher : DataSourcePublisherBase
    {
        public TaskDefinitionDataSourcePublisher(Core.Client.ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "TaskDefinition";

        public override IList<string> DependentPublisherTypeNames => new List<string> { };

        public override string EntityTypeName => "TaskDefinition";
        public override bool IsHarvester => true;
        public override bool IsValidatable => false;

        public override Func<SourceDataInfo, RenderFragment> SelectorDetailFormatting => (source) =>
        {
            var data = (TaskDefinitionData)source.Data;
            return ExpandData("td", new string[] { data.Name, data.Description });
        };

        public override Func<RenderFragment> SelectorHeaderFormatting => () => ExpandData("th", new[] { "Name", "Description" });
        public override string Title => "iMIS Process Automation Tasks";

        public override Type UIComponentType => typeof(StandardExtractDataSourceComponent);

        protected override NameValueCollection GetDescriptiveFields(SourceDataInfo sourceInfo = null)
        {
            var data = sourceInfo?.Data as TaskDefinitionData;
            return new NameValueCollection()
            {
                {"Name", data?.Name },
                {"Description", data?.Description }
            };
        }
    }
}