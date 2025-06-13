using Asi.DataMigrationService.Core;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.PanelData
{
    
    internal class PanelDataSourcePublisher : DataSourcePublisherBase
    {
        public PanelDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory, ICommonServiceHttpClientFactory commonServiceHttpClientFactory) : base(clientFactory)
        {
            CommonServiceHttpClientFactory = commonServiceHttpClientFactory;
        }

        private ICommonServiceHttpClientFactory CommonServiceHttpClientFactory { get;}
        public override string DataSourceTypeName => "PanelData";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party" };

        public override string Title => "iMIS Panel Data Sources";
        public override bool IsHarvester => true;
        public override bool IsValidatable => true;
        public override Type UIComponentType => typeof(PanelDataSourceComponent);

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            ICommonServiceHttpClient service = null;
            DataSourceImportInfo importInfo = null;
            PanelDataSourceDataInfo panelData = null;
            foreach (var importRow in batch)
            {
                if (importInfo == null)
                {
                    importInfo = importRow.ImportRowReference.Import;
                    var panelDatas = JsonConvert.DeserializeObject<IList<PanelDataSourceDataInfo>>(importInfo.DataSource.Data, GlobalSettings.JsonSerializerSettings);
                    panelData = panelDatas.FirstOrDefault(p => p.Name.EqualsOrdinalIgnoreCase(importInfo.Name));
                    if (panelData == null) break;
                    service = CommonServiceHttpClientFactory.Create(panelData.Name, context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                }
                var data = await ImportRowToDataAsync(context, importRow, panelData);

                var key = panelData.BOEntityDefinition.IdentityPropertyDefinitions.Select(p => data.Properties.GetPropertyValue(p.Name)?.ToString()).ToArray();

                var response = await service.FindByIdAsync(key);
                if (response.IsSuccessStatusCode)
                {
                    var updateData = (GenericEntityData)response.Result;
                    foreach (var item in data.Properties)
                    {
                        updateData.Properties.SetPropertyValue(item.Name, item.Value);
                    }
                    response = await service.UpdateAsync(updateData);
                }
                else if (response.StatusCode == StatusCode.NotFound)
                {
                    response = await service.AddAsync(data);
                }
                if (response.IsSuccessStatusCode)
                {
                    groupSuccess.IncrementSuccessCount();
                }
                else
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, importRow, $"Error: {response.Message}"));
                    groupSuccess.IncrementErrorCount();
                }
            }
        }

        protected override async Task<IServiceResponse> PublishDataSourceAsync(PublishContext context, ProcessingMode processingMode, DataSourceInfo dataSource, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action, GroupSuccess groupSuccess)
        {
            if (processingMode == ProcessingMode.Publish)
            {
                var selected = dataSource.Data != null ? JsonConvert.DeserializeObject<IList<PanelDataSourceDataInfo>>(dataSource.Data, GlobalSettings.JsonSerializerSettings) : new List<PanelDataSourceDataInfo>();
                var boService = CommonServiceHttpClientFactory.Create<IBOEntityDefinitionService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);

                foreach (var panelDataInfo in selected)
                {
                    var newDefinition = panelDataInfo.BOEntityDefinition;
                    var response = await boService.FindByIdAsync(newDefinition.EntityTypeName);
                    if (response.IsSuccessStatusCode)
                    {
                        // replace
                        var oldDefinition = response.Result;
                        if (!panelDataInfo.Replace)
                        {
                            foreach (var oldProperty in oldDefinition.Properties)
                            {
                                if (!newDefinition.Properties.Any(p => p.Name.EqualsOrdinalIgnoreCase(oldProperty.Name)))
                                    newDefinition.Properties.Add(oldProperty);
                            }
                        }
                        response = await boService.UpdateAsync(newDefinition);
                        if (response.IsSuccessStatusCode)
                        {
                            groupSuccess.IncrementSuccessCount();
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSource, $"Updated {DataSourceTypeName}:{newDefinition.EntityTypeName}."));
                        }
                        else
                        {
                            groupSuccess.IncrementErrorCount();
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, $"Failed to update {DataSourceTypeName}:{newDefinition.EntityTypeName}, {response.Message}"));
                        }
                    }
                    else if (response.StatusCode == StatusCode.NotFound)
                    {
                        // add
                        response = await boService.AddAsync(newDefinition);
                        if (response.IsSuccessStatusCode)
                        {
                            groupSuccess.IncrementSuccessCount();
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSource, $"Added {DataSourceTypeName}:{newDefinition.EntityTypeName}"));
                        }
                        else
                        {
                            groupSuccess.IncrementErrorCount();
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, $"Failed to add {DataSourceTypeName}:{newDefinition.EntityTypeName}, {response.Message}"));
                        }
                    }
                    else
                    {
                        groupSuccess.IncrementErrorCount();
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, $"Failed to read {DataSourceTypeName}:{newDefinition.EntityTypeName}, {response.Message}"));
                    }
                }
            }

            foreach (var import in dataSource.Imports)
            {
                var response = await PublishImportInBatchesAsync(context, processingMode, dataSource, import, action, groupSuccess);
                if (!response.IsSuccessStatusCode || context.CancellationToken.IsCancellationRequested)
                    return response;
            }
            return new ServiceResponse();
        }

        protected override Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            return Task.CompletedTask;
        }

        private async Task<GenericEntityData> ImportRowToDataAsync(PublishContext context, ImportRow importRow, PanelDataSourceDataInfo panelData)
        {
            var result = new GenericEntityData(panelData.Name);

            var template = new ImportTemplate();
            await MapSourceToImportTemplateAsync(context, importRow, template);
            foreach (var item in template.OtherColumns)
            {
                var property = panelData.BOEntityDefinition.Properties.FirstOrDefault(predicate => predicate.Name.EqualsOrdinalIgnoreCase(item.Key));
                if (property != null)
                {
                    if (Utility.TryConvert(item.Value, property.PropertyType, context.Culture, out var value))
                    {
                        if (value != null)
                            result.Properties.Add(new GenericPropertyData(property.Name, value));
                    }
                    else
                    {
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, importRow, "Could not convert data."));
                    }
                }
            }
            return result;
        }
    }
}