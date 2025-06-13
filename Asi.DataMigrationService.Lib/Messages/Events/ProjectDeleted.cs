using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Events
{
    public class ProjectDeleted : IEvent
    {
        public ProjectDeleted(string projectId)
        {
            ProjectId = projectId;
        }
        public string ProjectId { get; }
    }
}
