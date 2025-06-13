using System.Collections.Generic;
using System.Linq;
using Asi.DataMigrationService.Lib.Data.Models;

namespace Asi.DataMigrationService.Lib.Publisher.DataSource
{
    public class DataSourceInfo
    {
        public DataSourceInfo(ProjectDataSource projectDataSource)
        {
            ProjectId = projectDataSource.ProjectId;
            ProjectDataSourceId = projectDataSource.ProjectDataSourceId;
            TypeName = projectDataSource.DataSourceType;
            Name = projectDataSource.Name;
            Data = projectDataSource.Data;
            Imports = projectDataSource.Imports.Select(p => new DataSourceImportInfo { ProjectImportId = p.ProjectImportId, Name = p.Name, PropertyNames = p.GetPropertyNameList(), DataSource = this }).ToList();
        }

        public string ProjectId { get; }
        public string Name { get; }
        public int ProjectDataSourceId { get; set; }
        public string TypeName { get;}
        public string Data { get; }
        public IList<DataSourceImportInfo> Imports { get; }
    }
}