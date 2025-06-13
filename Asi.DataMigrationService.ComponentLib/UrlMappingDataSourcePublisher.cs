using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Asi.DataMigrationService.ComponentLib
{
    
    internal class UrlMappingDataSourcePublisher : DataSourcePublisherBase
    {
        public UrlMappingDataSourcePublisher(Core.Client.ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "UrlMapping";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Document", "Content" };

        public override string EntityTypeName => "UrlMapping";
        public override bool IsHarvester => true;
        public override bool IsValidatable => false;

        public override string Title => "iMIS Website Shortcuts";

        public override Type UIComponentType => typeof(StandardExtractDataSourceComponent);

        protected override NameValueCollection GetDescriptiveFields(SourceDataInfo sourceInfo = null)
        {
            var data = sourceInfo?.Data as UrlMappingData;
            return new NameValueCollection()
            {
                {"Name", data?.DirectoryName },
                {"Url", data?.Url },
                {"Description", data?.Description }
            };
        }
    }
}