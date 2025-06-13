using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class DeleteImports : ICommand
    {
        public DeleteImports(int projectDataSourceId)
        {
            ProjectDataSourceId = projectDataSourceId;
        }

        public int ProjectDataSourceId { get; set; }
    }
}
