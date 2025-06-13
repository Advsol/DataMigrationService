using Asi.DataMigrationService.Core.Client;

namespace Asi.DataMigrationService.ComponentLib.Document
{
    internal class BusinessObjectDataSourcePublisher : DocumentDataSourcePublisher
    {
        public BusinessObjectDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "BusinessObject";
        public override string Title => "iMIS Business Objects";
        public override string DocumentRoot => "$/Design Business Object Definition";
        public override string DocumentTypeName => "Business Objects";
    }
}