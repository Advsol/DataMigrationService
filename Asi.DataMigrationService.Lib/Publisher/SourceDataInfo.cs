using Asi.Soa.Core.Attributes;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class SourceDataInfo
    {
        public string Id => IdentityAttribute.GetIdentity(Data).Id?.ToString();
        public bool IsSelected { get; set; }
        public object Data { get; set; }
        public string Name { get; set; }
    }
}