using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Asi.DataMigrationService.Lib.Data.Models
{
    [Table("ProjectImportData")]
    public class ProjectImportData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProjectImportDataId { get; set; }
        [Required]
        public int ProjectImportId { get; set; }
        [Required]
        public int RowNumber { get; set; }
        public string Data { get; set; }
        public ProjectImport ProjectImport { get; set; }

        public IList<object> GetDataList()
        {
            return Data != null
                ? JsonConvert.DeserializeObject<List<object>>(Data, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto})
                : new List<object>();
        }
        public void SetData(IEnumerable<object> data)
        {
            Data = JsonConvert.SerializeObject(data, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }
        public void SetData(IEnumerable<string> data)
        {
            Data = JsonConvert.SerializeObject(data, new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat, TypeNameHandling = TypeNameHandling.Auto } );
        }
    }
}
