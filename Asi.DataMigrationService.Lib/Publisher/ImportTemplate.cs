using System;
using System.Collections.Generic;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class ImportTemplate
    {
        public Dictionary<string, string> OtherColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
