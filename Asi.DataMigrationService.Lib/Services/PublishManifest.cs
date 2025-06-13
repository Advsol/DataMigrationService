using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.DataMigrationService.Lib.Queries;
using Asi.Soa.Core.DataContracts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.Lib.Services
{
    public class PublishManifest
    {
        private readonly IPublishService _processorService;
        private readonly IProjectQueries _projectQueries;
        public IList<ManifestDataSourceType> DataSourceTypes = new List<ManifestDataSourceType>();

        public PublishManifest(IPublishService processorService, IProjectQueries projectQueries)
        {
            _processorService = processorService;
            _projectQueries = projectQueries;
        }
        public async Task<IServiceResponse> InitializeAsync(string projectId, PublishContext context)
        {
            var fatalError = false;
            var project = await _projectQueries.GetProjectAsync(projectId);
            if (project is null)
            {
                var message = $"Project {projectId} not found.";
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, message));
                return new ServiceResponse(StatusCode.BadRequest) { Message = message };
            }
            context.ProjectInfo = project.GetProjectInfo();

            var dataSources = await _projectQueries.GetProjectDataSourcesAsync(projectId);

            foreach (var dataSource in dataSources)
            {
                var dataSourceType = DataSourceTypes.FirstOrDefault(p => p.DataSourceTypeName == dataSource.DataSourceType);
                if (dataSourceType == null)
                {
                    var processor = _processorService.Create(dataSource.DataSourceType);
                    if (processor is null)
                    {
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, $"Data source type {dataSource.DataSourceType} is not supported."));
                        fatalError = true;
                        continue;
                    }
                    await processor.InitializeAsync(context);
                    dataSourceType = new ManifestDataSourceType { ProjectId = projectId, DataSourceTypeName = dataSource.DataSourceType, DataSourceProcessor = processor };
                    DataSourceTypes.Add(dataSourceType);
                }

                dataSourceType.DataSources.Add(new DataSourceInfo(dataSource));
            }
            if (fatalError)
                return new ServiceResponse(StatusCode.BadRequest) { Message = $"Error in {nameof(PublishManifest)}." };
            // order by dependencies, then type name
            DataSourceTypes = DataSourceTypes.OrderBy(p => p.DataSourceTypeName).TopologicalSort(Dependencies).ToList();
            return new ServiceResponse();
        }

        private IEnumerable<ManifestDataSourceType> Dependencies(ManifestDataSourceType arg) => DataSourceTypes.Where(p => arg.DataSourceProcessor.DependentPublisherTypeNames.Contains(p.DataSourceProcessor.DataSourceTypeName));
    }
}
