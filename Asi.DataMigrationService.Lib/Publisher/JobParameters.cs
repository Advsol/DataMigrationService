namespace Asi.DataMigrationService.Lib.Publisher
{
    public class JobParameters
    {
        public string ProjectId { get; set; }
        public int ProjectJobId { get; set; }
        public RunType RunType { get; set; }
        public LoginInformation TargetLoginInformation { get; set; }
        public string SubmittedBy { get; set; }
    }
}
