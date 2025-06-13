using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.Document
{
    internal class DocumentDataSourcePublisher : DataSourcePublisherBase
    {
        public DocumentDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "Document";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "PanelDefinition" };

        public override string Title => "iMIS Documents";
        public virtual string DocumentRoot => "$/";
        public virtual string DocumentTypeName => "Documents";
        public override bool IsHarvester => true;
        public override bool IsValidatable => false;
        public override Type UIComponentType => typeof(DocumentDataSourceComponent);

        public override RenderFragment CreateUIComponent(Project project, ProjectDataSource projectDataSource, LoginInformation sourceLoginInformation, Dictionary<string, object> additionalParameters = null)
        {
            var parameters = new Dictionary<string, object>
            {
                { "DocumentRoot", DocumentRoot },
                { "DocumentTypeName", DocumentTypeName }
            };
            return base.CreateUIComponent(project, projectDataSource, sourceLoginInformation, parameters);
        }

        protected override Task PublishExtractedDataAsync(ICommonServiceAsync service, PublishContext context, DataSourceInfo dataSource, GroupSuccess groupSuccess, Func<SourceDataInfo, string> dataFormatter)
        {
            // we use a non-standard approach on document (service type). So even though is "harvested" data, we do the work in PublishBatchAsync
            return Task.CompletedTask;
        }

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var service = ClientFactory.Create<IContentService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            foreach (var row in batch)
            {
                if (row.Data?.Count == 1)
                {
                    var data = (byte[])row.Data[0];
                    var response = await service.ImportContentAsync(data);
                    if (response.IsSuccessStatusCode)
                    {
                        groupSuccess.IncrementSuccessCount();
                    }
                    else
                    {
                        groupSuccess.IncrementErrorCount();
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, response.Message));
                    }
                }
            }
        }

        protected override async Task<IServiceResponse> PublishDataSourceAsync(PublishContext context, ProcessingMode processingMode, DataSourceInfo dataSource, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action, GroupSuccess groupSuccess)
        {
            return await base.PublishDataSourceAsync(context, processingMode, dataSource, action, groupSuccess);
        }

        protected override Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            return Task.CompletedTask;
        }
    }
}