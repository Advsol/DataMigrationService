using Asi.Core.Interfaces.Messaging;
using Asi.DataMigrationService.Lib.Data.Models;

namespace Asi.DataMigrationService.Lib.Messages.Commands
{
    public class CopyProject : ICommand
    {
        public CopyProject(string sourceProject, string name)
        {
            SourceProjectId = sourceProject;
            Name = name;
        }

        public string Name { get; set; }
        public string SourceProjectId { get; set; }
    }

    public class CreateProject : ICommand
    {
        public CreateProject(string name, string desciption, ProjectInfo projectInfo = null)
        {
            Name = name;
            Desciption = desciption;
            ProjectInfo = projectInfo;
        }

        public string Desciption { get; }
        public string Name { get; }
        public ProjectInfo ProjectInfo { get; }
    }
}