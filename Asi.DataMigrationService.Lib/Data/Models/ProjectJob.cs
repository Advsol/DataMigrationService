using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    public enum ProjectJobState
    {
        Submitted,
        Processing,
        Completed
    }
    [Table("ProjectJob")]
    public class ProjectJob
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectJobId { get; set; }
        public string ProjectId { get; set; }
        public ProjectJobState State {get; set;}
        public DateTime SubmittedOnUtc { get; set; }
        [MaxLength(100)]
        public string SubmittedBy { get; set;}
        public DateTime? StartedOnUtc { get; set; }
        public DateTime? CompletedOnUtc { get; set; }

        public Project Project { get; set; }
        public ICollection<ProjectJobMessage> ProjectJobMessages { get; set; }
    }

    public enum ProjectJobMessageType
    {
        Information,
        Warning,
        Error
    }
    [Table("ProjectJobMessage")]
    public class ProjectJobMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectJobMessageId { get; set; }
        public int ProjectJobId { get; set;}

        public ProjectJobMessageType MessageType { get; set; }
        [MaxLength(50)]
        public string Processor { get; set; }
        [MaxLength(100)]
        public string Source { get; set; }
        public int RowNumber { get; set; }
        [MaxLength(500)]
        public string Message { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        public ProjectJob ProjectJob { get; set; }
    }
}
