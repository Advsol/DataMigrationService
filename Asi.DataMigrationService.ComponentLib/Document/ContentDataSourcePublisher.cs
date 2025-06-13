using Asi.DataMigrationService.Core.Client;

namespace Asi.DataMigrationService.ComponentLib.Document
{
    
    internal class ContentDataSourcePublisher : DocumentDataSourcePublisher
    {
        public ContentDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "Content";
        public override string Title => "iMIS Content";
        public override string DocumentRoot => "@/";
        public override string DocumentTypeName => "Content";
    }
}