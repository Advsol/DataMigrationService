using System.Collections.Generic;

namespace Asi.DataMigrationService.Lib.Publisher.DataSource
{
    public class DataSourceImportInfo
    {
        public DataSourceInfo DataSource { get; set; }
        public string Name { get; set; }
        public int ProjectImportId { get; set; }
        public IList<string> PropertyNames { get; set; }
    }
}