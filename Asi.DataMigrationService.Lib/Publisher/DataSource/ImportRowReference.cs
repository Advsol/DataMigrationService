namespace Asi.DataMigrationService.Lib.Publisher.DataSource
{
    public class ImportRowReference
    {
        public DataSourceImportInfo Import { get; set; }
        public int ProjectImportDataId { get; set; }
        public int RowNumber { get; set; }
    }
}