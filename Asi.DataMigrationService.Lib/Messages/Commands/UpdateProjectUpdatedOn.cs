using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class UpdateProjectUpdatedOn : ICommand
    {
        public UpdateProjectUpdatedOn(string projectId)
        {
            ProjectId = projectId;
        }

        public string ProjectId { get; set; }
    }
}
