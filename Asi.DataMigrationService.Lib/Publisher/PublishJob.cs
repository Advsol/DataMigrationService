using System;
using System.Threading;
using System.Threading.Tasks;
using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public class PublishJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IPublishService _processorService;

        public PublishJob(IServiceProvider serviceProvider, IPublishService processorService)
        {
            _serviceProvider = serviceProvider;
            _processorService = processorService;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        public async Task RunAsync(JobParameters jobParameters, CancellationToken cancellationToken)
        {
            var errorLogger = new JobPublishMessageLogger(_serviceProvider, jobParameters.ProjectJobId);

            try
            {
                await SetJobState(jobParameters, ProjectJobState.Processing, cancellationToken);

                // create context
                var context = new PublishContext(_serviceProvider, errorLogger.LogMessageAsync, cancellationToken)
                {
                    TargetLoginInformation = jobParameters.TargetLoginInformation,
                    RunType = jobParameters.RunType
                };
                if (!await context.InitializeAsync())
                    return;

                await _processorService.PublishAsync(jobParameters.ProjectId, context);
            }
            catch (Exception exception)
            {
                await errorLogger.LogMessageAsync(new PublishMessage(PublishMessageType.Error, exception.Message));
            }
            finally
            {
                await SetJobState(jobParameters, ProjectJobState.Completed, cancellationToken);
            }
        }

        private async Task SetJobState(JobParameters jobParameters, ProjectJobState state, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var projectJob = await dbContext.ProjectJobs.FindAsync(jobParameters.ProjectJobId);
            if (projectJob != null)
            {
                projectJob.State = state;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
