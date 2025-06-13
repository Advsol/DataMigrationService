using Asi.Core.Interfaces.Messaging;
using Asi.DataMigrationService.Lib.Data.Models;
using System.Collections.Generic;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class AddProjectImportData : ICommand
    {
        public AddProjectImportData(int projectImportId, IEnumerable<ProjectImportData> projectImportData)
        {
            ProjectImportId = projectImportId;
            ProjectImportData = projectImportData;
        }
        public int ProjectImportId { get; set; }
        public IEnumerable<ProjectImportData> ProjectImportData { get; set; }
    }
}
