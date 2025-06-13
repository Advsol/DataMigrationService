using Asi.DataMigrationService.Core;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.DataMigrationService.Lib.Publisher.Party;
using Asi.DataMigrationService.Lib.Services;
using Asi.Soa.Core.Attributes;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public enum ProcessingMode
    {
        Validate,
        Publish
    }

    public abstract class DataSourcePublisherBase : IDataSourcePublisher
    {
        protected DataSourcePublisherBase(ICommonServiceHttpClientFactory clientFactory)
        {
            ClientFactory = clientFactory;
        }
        public ICommonServiceHttpClientFactory ClientFactory { get; set; }

        /// <summary> A map of import fields to import template properties. </summary>
        public Dictionary<string, string> ImportTemplatePropertyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, ImportTemplateProperty> _importTemplateProperties;

        /// <summary>   Gets the type of the processor. </summary>
        ///
        /// <value> The type of the processor. </value>
        public abstract string DataSourceTypeName { get; }
        public abstract bool IsHarvester { get; }
        public abstract bool IsValidatable { get; }

        public virtual Func<RenderFragment> SelectorHeaderFormatting => () => ExpandData("th", GetDescriptiveFields().AllKeys);
        public virtual Func<SourceDataInfo, RenderFragment> SelectorDetailFormatting => (data) => ExpandData("td", GetDescriptiveFields(data).AllValues());
        public virtual string EntityTypeName { get; }

        protected virtual NameValueCollection GetDescriptiveFields(SourceDataInfo sourceInfo = null)
        {
            return new NameValueCollection();
        }

        /// <summary>   Gets a list of types of the dependent processors. </summary>
        ///
        /// <value> A list of types of the dependent processors. </value>
        public abstract IList<string> DependentPublisherTypeNames { get; }

        /// <summary>   Gets the name. </summary>
        ///
        /// <value> The name. </value>
        public abstract string Title { get; }

        /// <summary> A dictionary of available import fields and requirements</summary>
        ///
        /// <value> The import template properties. </value>
        public Dictionary<string, ImportTemplateProperty> ImportTemplateProperties
        {
            get
            {
                if (_importTemplateProperties == null)
                {
                    var properties = new Dictionary<string, ImportTemplateProperty>(StringComparer.OrdinalIgnoreCase);
                    var importTemplate = CreateImportTemplateInstance();
                    if (importTemplate != null)
                    {
                        var infos = importTemplate.GetType().GetProperties();
                        foreach (var info in infos)
                        {
                            properties.Add(info.Name, new ImportTemplateProperty(info));
                        }
                    }
                    _importTemplateProperties = properties;
                }
                return _importTemplateProperties;
            }
        }

        /// <summary>   Gets the type of the available properties import template. </summary>
        ///
        /// <value> The type of the import template. </value>
        public virtual ImportTemplate CreateImportTemplateInstance() => null;

        /// <summary>   Gets the type of the component. </summary>
        ///
        /// <value> The type of the component. </value>
        public virtual Type UIComponentType { get; }

        protected static async Task LogValidationErrors(PublishContext context, ImportRow row, ValidationResult results)
        {
            foreach (var error in results.Errors)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, error.ErrorMessage));
            }
        }

        public virtual RenderFragment CreateUIComponent(Project project, ProjectDataSource projectDataSource, LoginInformation sourceLoginInformation, Dictionary<string, object> additionalParameters = null)
        {
            void renderFragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
            {
                builder.OpenComponent(0, UIComponentType);
                builder.AddAttribute(1, "Project", project);
                builder.AddAttribute(1, "ProjectDataSource", projectDataSource);
                builder.AddAttribute(1, "SourceLoginInformation", sourceLoginInformation);
                builder.AddAttribute(1, "Title", Title);
                builder.AddAttribute(1, "DataSourceTypeName", DataSourceTypeName);
                builder.AddAttribute(1, nameof(DataSourcePublisherBase), this);
                if (additionalParameters != null)
                {
                    foreach (var item in additionalParameters)
                    {
                        builder.AddAttribute(1, item.Key, item.Value);
                    }
                }

                builder.CloseComponent();
            }
            return renderFragment;
        }

        public virtual Task InitializeAsync(PublishContext context)
        {
            return Task.CompletedTask;
        }

        public virtual ImportTemplateProperty MapProperty(string propertyName)
        {
            var mappedPropertyName = propertyName;
            if (ImportTemplatePropertyMap.Count > 0)
            {
                if (ImportTemplatePropertyMap.TryGetValue(propertyName, out var value))
                {
                    mappedPropertyName = value;
                }
            }
            return ImportTemplateProperties.TryGetValue(mappedPropertyName, out var property) ? property : new ImportTemplateProperty(mappedPropertyName);
        }

        public virtual async Task<IServiceResponse<GroupSuccess>> PublishAsync(PublishContext context, ManifestDataSourceType dataSourceType)
        {
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType, $"Begin processing for {dataSourceType.DataSourceTypeName}."));
            var response = await PublishDataSourceTypeAsync(context, ProcessingMode.Publish, dataSourceType, PublishBatchAsync);
            var groupSuccess = response.Result ?? new GroupSuccess();
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType,
                $"Completed migration for {dataSourceType.DataSourceTypeName}. Success: {groupSuccess.SuccessCount}, Errors: {groupSuccess.ErrorCount}, Elapsed time: {groupSuccess.ElapsedTime}"));
            return response;
        }

        public virtual async Task<IServiceResponse<GroupSuccess>> ValidateAsync(PublishContext context, ManifestDataSourceType dataSourceType)
        {
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType, $"Begin validation for {dataSourceType.DataSourceTypeName}."));
            var response = ValidatePropertyNames(context, dataSourceType);
            if (!response.IsSuccessStatusCode)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType,
                    $"Completed validation for {dataSourceType.DataSourceTypeName}"));
                return new ServiceResponse<GroupSuccess>(response);
            }
            var response2 = await PublishDataSourceTypeAsync(context, ProcessingMode.Validate, dataSourceType, ValidateBatchAsync);
            var groupSuccess = response2.Result ?? new GroupSuccess();
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType,
                $"Completed validation for {dataSourceType.DataSourceTypeName}. Errors: {groupSuccess.ErrorCount}, Elapsed time: {groupSuccess.ElapsedTime}"));
            return response2;
        }

        protected Task<bool> MapSourceToImportTemplateAsync(PublishContext context, ImportRow row, ImportTemplate instance)
        {
            const bool success = true;
            for (var i = 0; i < row.ImportRowReference.Import.PropertyNames.Count; i++)
            {
                if (i < row.Data.Count)
                {
                    var value = row.Data[i];
                    if (value is string s)
                        value = s.NullTrim();
                    if (value != null)
                    {
                        var propertyInfo = MapProperty(row.ImportRowReference.Import.PropertyNames[i]);
                        if (propertyInfo.IsOtherColumn)
                        {
                            if (value.GetType().Equals(typeof(byte[])))
                                instance.OtherColumns[propertyInfo.Name] = System.Text.Encoding.Default.GetString((byte[])value);
                            else
                                instance.OtherColumns[propertyInfo.Name] = value.ToString();
                        }
                        else
                        {
                            if (Utility.TryConvert(value, propertyInfo.Type, context.Culture, out var targetValue))
                            {
                                propertyInfo.PropertyInfo.SetValue(instance, targetValue);
                            }
                        }
                    }
                }
            }
            return Task.FromResult(success);
        }

        protected virtual Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSource, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            return Task.CompletedTask;
        }

        protected virtual async Task<IServiceResponse> PublishDataSourceAsync(PublishContext context, ProcessingMode processingMode, DataSourceInfo dataSource, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action, GroupSuccess groupSuccess)
        {
            if (processingMode == ProcessingMode.Publish && IsHarvester)
            {
                var service = ClientFactory.Create(EntityTypeName, context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                await PublishExtractedDataAsync(service, context, dataSource, groupSuccess, GetDataDescription);
            }

            foreach (var import in dataSource.Imports)
            {
                var response = await PublishImportInBatchesAsync(context, processingMode, dataSource, import, action, groupSuccess);
                if (!response.IsSuccessStatusCode || context.CancellationToken.IsCancellationRequested)
                    return response;
            }
            return new ServiceResponse();
        }

        protected virtual async Task<IServiceResponse<GroupSuccess>> PublishDataSourceTypeAsync(PublishContext context, ProcessingMode processingMode, ManifestDataSourceType dataSourceType, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action)
        {
            var groupSuccess = new GroupSuccess();

            foreach (var dataSource in dataSourceType.DataSources)
            {
                var response2 = await PublishDataSourceAsync(context, processingMode, dataSource, action, groupSuccess);
                if (!response2.IsSuccessStatusCode) return new ServiceResponse<GroupSuccess>(response2) { Result = groupSuccess };
                if (context.CancellationToken.IsCancellationRequested)
                    break;
            }
            return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
        }

        protected virtual async Task<IServiceResponse> PublishImportInBatchesAsync(PublishContext context, ProcessingMode processingMode, DataSourceInfo dataSourceInfo, DataSourceImportInfo import, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action, GroupSuccess groupSuccess)
        {
            var actionBlock = new ActionBlock<Func<Task>>(func => func.Invoke(),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
                    BoundedCapacity = context.MaxDegreeOfParallelism * 2 // number of queued batches
                });

            var importBatch = new ImportBatch(import, context.ServiceProvider);

            var limit = importBatch.SourcePageLimit; //source fetch size
            var offset = 0;

            var response = await importBatch.GetPagedResultAsync(offset, limit);
            if (!response.IsSuccessStatusCode)
            {
                return new ServiceResponse<GroupSuccess>(response) { Result = groupSuccess };
            }
            var result = response.Result;
            var batch = new List<ImportRow>();
            while (true)
            {
                foreach (var item in result)
                {
                    batch.Add(item);
                    if (batch.Count >= importBatch.ProcessingBatchSize)
                    {
                        var batch1 = new List<ImportRow>(batch);
                        await actionBlock.SendAsync(() => action(context, dataSourceInfo, batch1, groupSuccess), context.CancellationToken);
                        batch.Clear();
                        if (context.CancellationToken.IsCancellationRequested)
                            return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
                    }
                }
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
                }
                if (!result.HasNext)
                    break;
                offset = result.NextOffset;
                response = await importBatch.GetPagedResultAsync(offset, limit);
                if (!response.IsSuccessStatusCode)
                {
                    return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
                }
                result = response.Result;
            }
            if (batch.Count > 0)
            {
                var batch1 = new List<ImportRow>(batch);
                await actionBlock.SendAsync(() => action(context, dataSourceInfo, batch1, groupSuccess), context.CancellationToken);
            }
            actionBlock.Complete();
            await actionBlock.Completion;
            return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
        }

        protected async Task ValidateBatch<TImportTemplate>(PublishContext context, IList<ImportRow> batch, GroupSuccess groupSuccess, IValidator<TImportTemplate> validator, string[] ruleSet = null)
            where TImportTemplate : ImportTemplate
        {
            foreach (var row in batch)
            {
                var instance = (TImportTemplate)CreateImportTemplateInstance();
                await MapSourceToImportTemplateAsync(context, row, instance);
                var valid = await ValidateRowAsync(context, row, validator, instance, ruleSet);
                if (valid)
                    groupSuccess.IncrementSuccessCount();
                else
                    groupSuccess.IncrementErrorCount();
            }
        }

        protected virtual Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSource, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            return Task.CompletedTask;
        }

        protected virtual IServiceResponse ValidatePropertyNames(PublishContext context, ManifestDataSourceType dataSourceType)
        {
            if (dataSourceType is null) throw new ArgumentNullException(nameof(dataSourceType));

            var serviceResponse = new ServiceResponse();
            foreach (var dataSource in dataSourceType.DataSources)
            {
                foreach (var import in dataSource.Imports)
                {
                    var mappedNames = new List<string>();
                    foreach (var propertyName in import.PropertyNames)
                    {
                        var property = MapProperty(propertyName);
                        if (property != null)
                        {
                            mappedNames.Add(property.Name);
                        }
                    }
                    foreach (var property in ImportTemplateProperties.Where(p => p.Value.IsRequired))
                    {
                        if (!mappedNames.Any(p => p.EqualsOrdinalIgnoreCase(property.Key)))
                        {
                            var message = $"Required property {property.Key} is missing.";
                            serviceResponse.ValidationResults.AddError(message);
                            context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, message));
                        }
                    }
                    foreach (var mappedName in mappedNames.Where(p => p.Contains(" ")))
                    {
                        var message = $"Invalid property format \"{mappedName.Replace(" ","[ ]")}\".";
                        serviceResponse.ValidationResults.AddError(message);
                        context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, message));
                    }
                }
            }

            return serviceResponse;
        }

        protected virtual async Task<bool> ValidateRowAsync<TImportTemplate>(PublishContext context, ImportRow row, IValidator<TImportTemplate> validator, TImportTemplate instance, string[] ruleSet = null)
            where TImportTemplate : ImportTemplate
        {
            if(ruleSet is null)
                ruleSet = new[] { "default" };
            var results = await validator.ValidateAsync(instance, options => options.IncludeRuleSets(ruleSet));
            if (results.IsValid)
                return true;
            await LogValidationErrors(context, row, results);
            return false;
        }

        protected static async Task InsertBatchAsync<TDataContract>(PublishContext context, DataSourceInfo dataSource, GroupSuccess groupSuccess, ICommonServiceAsync<TDataContract> service, IList<InsertUpdateRow<TDataContract>> list)
            where TDataContract : class
        {
            var response = await service.BulkInsertAsync(list.Select(prop => prop.DataContract).ToList());
            if (response.IsSuccessStatusCode)
            {
                if (list.Count == response.Result.Count)
                {
                    groupSuccess.IncrementSuccessCount(list.Count);
                    for (var i = 0; i < response.Result.Count; i++)
                    {
                        list[i].ResultKey = response.Result[i];
                    }
                    return;
                }
                else
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, dataSource, "Bulk insert succeeded, but return count did not match what was sent."));
                    groupSuccess.IncrementErrorCount(list.Count);
                }
            }
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, dataSource, $"Bulk insert failed. Retrying individual inserts. Error: {response.Message}"));
            foreach (var item in list)
            {
                var response2 = await service.AddAsync(item.DataContract);
                if (response2.IsSuccessStatusCode)
                {
                    groupSuccess.IncrementSuccessCount();
                    item.ResultKey = IdentityAttribute.GetIdentity(response2.Result).Id;
                }
                else
                {
                    groupSuccess.IncrementErrorCount();
                    item.ResultKey = null;
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, item.Row, response2.Message));
                }
            }
            return;
        }

        protected static async Task UpdateBatchAsync<TDataContract>(PublishContext context, DataSourceInfo dataSource, GroupSuccess groupSuccess, ICommonServiceAsync<TDataContract> service, IList<InsertUpdateRow<TDataContract>> list)
            where TDataContract : class
        {
            var response = await service.BulkUpdateAsync(list.Select(prop => prop.DataContract).ToList());
            if (response.IsSuccessStatusCode)
            {
                groupSuccess.IncrementSuccessCount(list.Count);
                return;
            }

            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, dataSource, $"Bulk update failed. Retrying individual inserts. Error: {response.Message}"));
            foreach (var item in list)
            {
                var response2 = await service.UpdateAsync(item.DataContract);
                if (response2.IsSuccessStatusCode)
                {
                    groupSuccess.IncrementSuccessCount();
                    item.ResultKey = IdentityAttribute.GetIdentity(response2.Result).Id;
                }
                else
                {
                    groupSuccess.IncrementErrorCount();
                    item.ResultKey = null;
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, item.Row, response2.Message));
                }
            }
            return;
        }

        protected virtual async Task PublishExtractedDataAsync(ICommonServiceAsync service, PublishContext context, DataSourceInfo dataSource, GroupSuccess groupSuccess, Func<SourceDataInfo,string> dataFormatter)
        {
            // first, we process definitions; then we can process optional imports
            var selected = dataSource.Data != null ? JsonConvert.DeserializeObject<IEnumerable<SourceDataInfo>>(dataSource.Data, GlobalSettings.JsonSerializerSettings) : new List<SourceDataInfo>();

            foreach (var sourceDataInfo in selected)
            {
                var newDefinition = sourceDataInfo.Data;
                var response = await service.FindByIdAsync(sourceDataInfo.Id);
                if (response.IsSuccessStatusCode)
                {
                    // replace
                    response = await service.UpdateAsync(newDefinition);
                    if (response.IsSuccessStatusCode)
                    {
                        groupSuccess.IncrementSuccessCount();
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSource, $"Updated {DataSourceTypeName}:{dataFormatter(sourceDataInfo)}."));
                    }
                    else
                    {
                        groupSuccess.IncrementErrorCount();
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, $"Failed to update {DataSourceTypeName}:{dataFormatter(sourceDataInfo)}, {response.Message}"));
                    }
                }
                else if (response.StatusCode == StatusCode.NotFound)
                {
                    // add

                    //if (string.IsNullOrEmpty(sourceDataInfo.Id)) 
                    //{

                    //    ((UrlMappingData)newDefinition).UrlMappingId = sourceDataInfo.Id;
                    //}
                        


                    response = await service.AddAsync(newDefinition);
                    if (response.IsSuccessStatusCode)
                    {
                        groupSuccess.IncrementSuccessCount();
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSource, $"Added {DataSourceTypeName}:{dataFormatter(sourceDataInfo)}"));
                    }
                    else
                    {
                        groupSuccess.IncrementErrorCount();
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, $"Failed to add {DataSourceTypeName}:{dataFormatter(sourceDataInfo)}, {response.Message}"));
                    }
                }
                else
                {
                    groupSuccess.IncrementErrorCount();
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, $"Failed to read {DataSourceTypeName}:{dataFormatter(sourceDataInfo)}, {response.Message}"));
                }
            }
        }
        protected static RenderFragment ExpandData(string elementType, IEnumerable<string> elements)
        {
            return b =>
            {
                foreach (var item in elements)
                {
                    b.OpenElement(2, elementType);
                    if (elementType.Equals("th", StringComparison.InvariantCultureIgnoreCase) && item.Equals("Name", StringComparison.InvariantCultureIgnoreCase)) 
                        b.AddAttribute(0, "style", "width:25%");
                    b.AddContent(3, item);
                    b.CloseElement();
                }
            };
        }

        protected string GetDataDescription(SourceDataInfo sourceInfo)
        {
            var fields = GetDescriptiveFields(sourceInfo);
            return string.Join(": ", fields.Cast<string>().Select(p => fields[p]));
        }

        protected class InsertUpdateRow<TDataContract>
        {
            public ImportRow Row { get; set; }
            public TDataContract DataContract { get; set; }
            public object ResultKey { get; set; }
            public PartyMap PartyMap { get; set; }
        }
    }
}