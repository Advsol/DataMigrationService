using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.DataMigrationService.Lib.Services;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public enum PublishMessageType
    {
        Information,
        Warning,
        Error
    }
    public class PublishMessage
    {
        public PublishMessageType MessageType { get; }
        public string DataSourceName { get; }
        public string DataSourceTypeName { get; }
        public int RowNumber { get; }
        public string Message { get; }

        public PublishMessage(PublishMessageType messageType, ImportRow dataSourceRow, string message) : this(messageType, dataSourceRow.ImportRowReference, message)
        {
        }

        public PublishMessage(PublishMessageType messageType, ImportRowReference dataSourceRowReference, string message)
        {
            MessageType = messageType;
            DataSourceName = dataSourceRowReference.Import.DataSource.Name;
            DataSourceTypeName = dataSourceRowReference.Import.DataSource.TypeName;
            RowNumber = dataSourceRowReference.RowNumber;
            Message = message;
        }

        public PublishMessage(PublishMessageType messageType, DataSourceInfo dataSource, string message)
        {
            MessageType = messageType;
            DataSourceName = dataSource?.Name;
            DataSourceTypeName = dataSource?.TypeName;
            Message = message;
        }
        public PublishMessage(PublishMessageType messageType, ManifestDataSourceType group, string message)
        {
            MessageType = messageType;
            DataSourceTypeName = group.DataSourceTypeName;
            Message = message;
        }
        public PublishMessage(PublishMessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }

        public override string ToString()
        {
            var message = $"{MessageType}";
            if (DataSourceName != null)
                message += $": Source: {DataSourceName}";
            if (RowNumber > 0)
                message += $", Row: {RowNumber}";
            message += $", {Message}";
            return message;
        }
    }
}
