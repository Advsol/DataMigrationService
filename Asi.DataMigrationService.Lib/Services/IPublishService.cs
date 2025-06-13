using System.Collections.Generic;
using System.Threading.Tasks;
using Asi.DataMigrationService.Lib.Publisher;

namespace Asi.DataMigrationService.Lib.Services
{
    public interface IPublishService
    {
        IDataSourcePublisher Create(string name);
        List<(string PublisherType, string Name)> GetProcessorTypeNames();
        Task PublishAsync(string projectId, PublishContext processorContext);
    }
}
