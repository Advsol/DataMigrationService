using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Asi.DataMigrationService.Lib.Publisher.Hub
{
    public interface IDataMigrationServicer
    {
        Task GetUserName();

        Task PublishMessage(string message);

        Task RunPublishJob(JobParameters jobParameters);

        Task ShowTime(DateTime currentTime);
    }

    //[Authorize]
    public class DataMigrationServiceHub : Hub<IDataMigrationServicer>
    {
        private readonly DataMigrationServiceBackgroundService _worker;

        public DataMigrationServiceHub(DataMigrationServiceBackgroundService backgoundService)
        {
            _worker = backgoundService;
        }

        public async Task AddToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task<int> RunPublishJob(JobParameters jobParameters)
        {
            try
            {
                return (await _worker.RunPublishJobAsync(jobParameters)).Result;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public async Task SendTimeToClients(DateTime dateTime)
        {
            await Clients.All.ShowTime(dateTime);
        }
    }
}