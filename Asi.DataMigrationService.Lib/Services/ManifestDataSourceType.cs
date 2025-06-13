using System.Collections.Generic;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;

namespace Asi.DataMigrationService.Lib.Services
{
    public class ManifestDataSourceType
    {
        public string ProjectId { get; set; }
        public string DataSourceTypeName { get; set; }
        public IDataSourcePublisher DataSourceProcessor { get; set; }
        public IList<DataSourceInfo> DataSources { get; } = new List<DataSourceInfo>();
    }
}
