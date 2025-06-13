using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Events
{
    public class ProjectCreated : IEvent
    {
        public ProjectCreated(string projectId)
        {
            ProjectId = projectId;
        }
        public string ProjectId { get; }
    }
}
