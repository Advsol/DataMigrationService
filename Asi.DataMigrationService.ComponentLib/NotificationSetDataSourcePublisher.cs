using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Communications.DataContracts;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Asi.DataMigrationService.ComponentLib
{
    
    internal class NotificationSetDataSourcePublisher : DataSourcePublisherBase
    {
        public NotificationSetDataSourcePublisher(Core.Client.ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "NotificationSet";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "TaskDefinition" };

        public override string EntityTypeName => "NotificationSet";
        public override bool IsHarvester => true;
        public override bool IsValidatable => false;
        public override string Title => "iMIS Alert Sets";
        public override Type UIComponentType => typeof(StandardExtractDataSourceComponent);

        protected override NameValueCollection GetDescriptiveFields(SourceDataInfo sourceInfo = null)
        {
            var data = sourceInfo?.Data as NotificationSetData;
            return new NameValueCollection()
            {
                {"Name", data?.Name },
                {"Description", data?.Description }
            };
        }
    }
}