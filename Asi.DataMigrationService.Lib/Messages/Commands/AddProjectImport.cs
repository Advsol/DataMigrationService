using Asi.Core.Interfaces.Messaging;
using System.Collections.Generic;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class AddProjectImport : ICommand
    {
        public AddProjectImport(int projectDataSourceId, string name, IEnumerable<string> propertyNames)
        {
            ProjectDataSourceId = projectDataSourceId;
            Name = name;
            PropertyNames = propertyNames;
        }

        public int ProjectDataSourceId { get; set; }
        public string Name { get; set; }
        public IEnumerable<string> PropertyNames { get; set; }
    }
}
