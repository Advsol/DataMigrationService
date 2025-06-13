using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Queries;
using Asi.Soa.Core.DataContracts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Asi.DataMigrationService.Lib.Publisher.Hub
{
    public class DataMigrationServiceBackgroundService : BackgroundService
    {
        private readonly ILogger<DataMigrationServiceBackgroundService> _logger;
        private readonly IHubContext<DataMigrationServiceHub, IDataMigrationServicer> _processinghHub;
        private readonly IServiceProvider _serviceProvider;
        private readonly ActionBlock<Func<Task>> _actionBlock;
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenRegistration _cancellationTokenRegistration = new CancellationTokenRegistration();
        private readonly int _maxDegreeOfParallelism = 2;
        private readonly IProjectQueries _projectQueries;

        public DataMigrationServiceBackgroundService(ILogger<DataMigrationServiceBackgroundService> logger, IHubContext<DataMigrationServiceHub, IDataMigrationServicer> processinghHub,
            IServiceProvider serviceProvider, IProjectQueries projectQueries)
        {
            _logger = logger;
            _processinghHub = processinghHub;
            _serviceProvider = serviceProvider;
            _cancellationToken = _cancellationTokenRegistration.Token;
            _actionBlock = new ActionBlock<Func<Task>>(action => action.Invoke()
                , new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                    BoundedCapacity = _maxDegreeOfParallelism
                });
            _projectQueries = projectQueries;
        }

        public async Task<IServiceResponse<int>> RunPublishJobAsync(JobParameters jobParameters)
        {
            try
            {
                var job = new ProjectJob
                {
                    ProjectId = jobParameters.ProjectId,
                    State = ProjectJobState.Submitted,
                    SubmittedBy = jobParameters.SubmittedBy,
                    SubmittedOnUtc = DateTime.UtcNow
                };
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    dbContext.ProjectJobs.Add(job);
                    await dbContext.SaveChangesAsync();
                }
                jobParameters.ProjectJobId = job.ProjectJobId;

                _actionBlock.Post(async () => await Run(jobParameters));
                return new ServiceResponse<int> { Result = job.ProjectJobId };
            }
            catch (Exception exception)
            {
                return new ServiceResponse<int> { Exception = exception };
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        private async Task Run(JobParameters jobParameters)
        {
            using var scope = _serviceProvider.CreateScope();
            try
            {
                var service = scope.ServiceProvider.GetRequiredService<PublishJob>();
                await service.RunAsync(jobParameters, _cancellationToken);

                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var projectJob = await _projectQueries.GetProjectJobAsync(jobParameters.ProjectJobId);
                if (projectJob != null)
                {
                    projectJob.CompletedOnUtc = DateTime.UtcNow;
                    dbContext.ProjectJobs.Update(projectJob);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error in {nameof(DataMigrationServiceBackgroundService)}.{nameof(Run)}");
            }
        }
    }
}