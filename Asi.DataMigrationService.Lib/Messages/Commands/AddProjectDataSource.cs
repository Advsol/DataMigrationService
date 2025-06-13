using Asi.Core.Interfaces.Messaging;
using System.Collections.Generic;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class AddProjectDataSource : ICommand
    {
        public AddProjectDataSource(string projectId, string name, string dataSourceType)
        {
            ProjectId = projectId;
            Name = name;
            DataSourceType = dataSourceType;
        }
        public string ProjectId { get; }
        public string Name { get; }
        public string DataSourceType { get; }
        public string Data { get; set; }
        public IList<string> PropertyNames { get; set; }
        public IList<IList<object>> ImportData { get; set; }
    }
}
