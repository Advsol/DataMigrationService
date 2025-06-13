using System.Collections.Generic;

namespace Asi.DataMigrationService.Lib.Publisher.DataSource
{
    public class ImportRow
    {
        public ImportRow(ImportRowReference dataSourceRowReference, IList<object> data)
        {
            ImportRowReference = dataSourceRowReference;
            Data = data;
        }

        public IList<object> Data { get; }
        public ImportRowReference ImportRowReference { get; }
    }
}