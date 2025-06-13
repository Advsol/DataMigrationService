using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Queries;
using Asi.Soa.Membership.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Lib.Services
{
    public class PublishService : IPublishService
    {
        private readonly IEnumerable<IDataSourcePublisher> _processors;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICommonServiceHttpClientFactory _commonServiceHttpClientFactory;
        private readonly IProjectQueries _projectQueries;

        public PublishService(IEnumerable<IDataSourcePublisher> processors, IServiceProvider serviceProvider,
            ICommonServiceHttpClientFactory commonServiceHttpClientFactory, IProjectQueries projectQueries)
        {
            _processors = processors;
            _serviceProvider = serviceProvider;
            _commonServiceHttpClientFactory = commonServiceHttpClientFactory;
            _projectQueries = projectQueries;
        }
        public List<(string PublisherType, string Name)> GetProcessorTypeNames()
        {
            return _processors.Select(p => (DataSourceType: p.DataSourceTypeName, p.Title)).Where(i => i.Title.EndsWith("File") == false && i.Title.Equals("iMIS Products") == false && i.Title.Equals("iMIS Communication") == false).OrderBy(p => p.Title).ToList();
        }
        public IDataSourcePublisher Create(string processorType)
        {
            var processor = _processors.FirstOrDefault(p => p.DataSourceTypeName.EqualsOrdinalIgnoreCase(processorType));
            if (processor is null) return null;

            // get a unique instance
            return (IDataSourcePublisher)_serviceProvider.GetRequiredService(processor.GetType());
        }

        public async Task PublishAsync(string projectId, PublishContext context)
        {
            // confirm target
            var settingsService = _commonServiceHttpClientFactory.Create<ISystemSettingsService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            var settingsResponse = await settingsService.FindByIdAsync(0);
            if (!settingsResponse.IsSuccessStatusCode)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, $"Can not login to target service: {context.TargetLoginInformation.Uri}, error: {settingsResponse.Message}"));
                return;
            }
            var isV10 = settingsResponse.Result.ImisMajorVersion.Length <= 2;
            context.Platform = isV10 ? Platform.V10 : Platform.V100;

            // create the publish manifest
            var manifest = new PublishManifest(this, _projectQueries);
            var response = await manifest.InitializeAsync(projectId, context);
            if (!response.IsSuccessStatusCode)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, response.Message));
                return;
            }

            var project = await _projectQueries.GetProjectAsync(projectId);

            var totalRun = new Stopwatch();
            totalRun.Start();

            var platform = settingsResponse.Result.ImisMajorVersion.ToString().Equals("20") ? "Enterprise (v10)" : "Professional (v100)";
            var runType = context.RunType.ToString().Equals("Publish", StringComparison.InvariantCultureIgnoreCase) ? "migration" : "validation";

            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, $"Platform: {platform}"));
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, $"Begin {runType} for project {project.Name}. ({context.TargetLoginInformation.UserCredentials.UserName.ToUpperInvariant()} - {context.TargetLoginInformation.Uri})"));
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, $"Data source types: {manifest.DataSourceTypes.Count()}"));
            
            // validate
            var hasError = false;
            foreach (var dataSourceType in manifest.DataSourceTypes)
            {
                if (dataSourceType.DataSourceProcessor.IsValidatable)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();

                        var response2 = await dataSourceType.DataSourceProcessor.ValidateAsync(context, dataSourceType);
                        hasError |= !response2.IsSuccessStatusCode;
                        if (response2.Result != null)
                            hasError |= response2.Result.ErrorCount > 0;

                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType, "Cancelled."));
                            await ExitMessage(context, project, totalRun);
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSourceType, exception.ToString()));
                        await ExitMessage(context, project, totalRun);
                        return;
                    }
                }
                else 
                {                  
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType, $"{(dataSourceType.DataSourceProcessor).Title} is not validated."));
                }
            }

            if (context.RunType == RunType.Publish)
            {
                if (hasError)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, "Aborting migration due to validation error(s)."));
                    return;
                }
                foreach (var dataSourceType in manifest.DataSourceTypes)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        await dataSourceType.DataSourceProcessor.PublishAsync(context, dataSourceType);

                        if (context.CancellationToken.IsCancellationRequested)
                        {
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType, "Cancelled."));
                            await ExitMessage(context, project, totalRun);
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSourceType, exception.ToString()));
                        await ExitMessage(context, project, totalRun);
                        return;
                    }
                }
            }
            await ExitMessage(context, project, totalRun);

            static async Task ExitMessage(PublishContext context, Project project, Stopwatch totalRun)
            {
                var runType = context.RunType.ToString().Equals("Publish", StringComparison.InvariantCultureIgnoreCase) ? "migration" : "validation";
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, $"Complete {runType} for project {project.Name}. Elapsed time: {totalRun.Elapsed:d\\.hh\\:mm\\:ss}"));
            }
        }
    }
}
