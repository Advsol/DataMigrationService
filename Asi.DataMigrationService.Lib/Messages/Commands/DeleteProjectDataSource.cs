using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class DeleteProjectDataSource : ICommand
    {
        public DeleteProjectDataSource(string projectId, int projectDataSourceId)
        {
            ProjectId = projectId;
            ProjectDataSourceId = projectDataSourceId;
        }
        public string ProjectId { get; }
        public int ProjectDataSourceId { get; }
    }
}
