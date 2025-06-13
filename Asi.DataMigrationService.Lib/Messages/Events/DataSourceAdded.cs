using Asi.Core.Interfaces.Messaging;

namespace Asi.DataMigrationService.Lib.Messages.Events
{
    public class DataSourceAdded : IEvent
    {
        public DataSourceAdded(int dataSourceId)
        {
            DataSourceId = dataSourceId;
        }
        public int DataSourceId { get; }
    }
}
