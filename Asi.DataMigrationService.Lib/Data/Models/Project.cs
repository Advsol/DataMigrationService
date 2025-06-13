using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    [Table("Project")]
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [MaxLength(50)]
        public string ProjectId { get; set; }
        [MaxLength(100)]
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProjectInfo { get; set; }
        [Required]
        public DateTime CreatedOn { get; set; }
        [Required]
        public DateTime UpdatedOn { get; set; }
        public bool IsLocked { get; set; }
        public ICollection<ProjectDataSource> DataSources { get; set; }
        [StringLength(50)]
        public string TenantId { get; set; }
        public Tenant Tenant { get; set; }
        public ICollection<ProjectJob> ProjectJobs { get; set; }
        public override string ToString() => $"Project: {{{ProjectId}, {Name}}}";
        public ProjectInfo GetProjectInfo() => ProjectInfo != null ? JsonConvert.DeserializeObject<ProjectInfo>(ProjectInfo) : new ProjectInfo();
        public void SetProjectInfo(ProjectInfo projectInfo) => ProjectInfo = JsonConvert.SerializeObject(projectInfo);
    }

    public class ProjectInfo
    {
        private string _culture;

        public string Culture { get => string.IsNullOrEmpty( _culture) ? "en-US" : _culture; set => _culture = value; }
        public bool CreateAccounts { get; set; }
        public bool AutoAssignPartyId { get; set; } = true;
        public bool AllowUpdates { get; set; } = true;
    }
}
