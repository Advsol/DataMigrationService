using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Events
{
    public class ProjectDataSourceDeleted : IEvent
    {
        public ProjectDataSourceDeleted(string projectId, int projectDataSourceId)
        {
            ProjectId = projectId;
            ProjectDataSourceId = projectDataSourceId;
        }
        public string ProjectId { get; }
        public int ProjectDataSourceId { get; }
    }
}
