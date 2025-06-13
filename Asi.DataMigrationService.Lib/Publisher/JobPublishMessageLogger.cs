using System;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Publisher.Hub;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class JobPublishMessageLogger
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly int _projectJobId;
        private readonly IHubContext<DataMigrationServiceHub, IDataMigrationServicer> _processingHub;

        public JobPublishMessageLogger(IServiceProvider serviceProvider, int projectJobId)
        {
            _serviceProvider = serviceProvider;
            _projectJobId = projectJobId;
            _processingHub = _serviceProvider.GetRequiredService<IHubContext<DataMigrationServiceHub, IDataMigrationServicer>>();
        }

        public async Task LogMessageAsync(PublishMessage message)
        {
            var pjm = new ProjectJobMessage
            {
                ProjectJobId = _projectJobId,
                MessageType = (ProjectJobMessageType)(int)message.MessageType,
                Processor = message.DataSourceTypeName,
                Source = message.DataSourceName,
                Message = message.Message.Truncate(500),
                RowNumber = message.RowNumber,
                CreatedOnUtc = DateTime.UtcNow
            };
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.ProjectJobMessages.Add(pjm);
                await dbContext.SaveChangesAsync();
            }

            if (_processingHub != null)
            {
                await _processingHub.Clients.Group($"Job:{_projectJobId}").PublishMessage(message.ToString());
            }
        }
    }
}
