using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class DeleteProject : ICommand
    {
        public DeleteProject(string projectId)
        {
            ProjectId = projectId;
        }
        public string ProjectId { get; }
    }
}
