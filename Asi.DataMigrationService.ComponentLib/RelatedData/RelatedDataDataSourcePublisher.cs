using Asi.DataMigrationService.Core;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using Asi.Soa.Membership.ServiceContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.RelatedData
{
    
    public class RelatedDataDataSourcePublisher : DataSourcePublisherBase
    {
        public static string[] IdNames = new[] { "ID", "ContactKey", "PartyId", "GroupKey", "Ordinal" };

        private readonly ConcurrentDictionary<string, string> _groupIds = new ConcurrentDictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        private readonly ConcurrentDictionary<string, BOEntityDefinitionData> EntityDefinitions = new ConcurrentDictionary<string, BOEntityDefinitionData>(StringComparer.OrdinalIgnoreCase);

        public RelatedDataDataSourcePublisher(Core.Client.ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "RelatedData";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party", "PanelDefinition" };

        public override bool IsHarvester => false;
        public override bool IsValidatable => true;
        public override string Title => "Panel Data Import File";
        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        public override ImportTemplate CreateImportTemplateInstance() => new ImportTemplate();

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSource, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var inserts = new List<InsertUpdateRow<GenericEntityData>>();
            var updates = new List<InsertUpdateRow<GenericEntityData>>();
            var entityTypeName = dataSource.Name;
            var ed = await GetEntityDefinitionDataAsync(context, dataSource.Name);
            var service = (ICommonServiceAsync<GenericEntityData>)ClientFactory.Create(entityTypeName, context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            var partySummaryService = ClientFactory.Create<IPartySummaryService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            var identityName = GetIdentityName(ed);
            var objectType = ed.ObjectType;
            foreach (var row in batch)
            {
                var instance = CreateImportTemplateInstance();
                await MapSourceToImportTemplateAsync(context, row, instance);

                var entity = new GenericEntityData(ed.EntityTypeName);
                var isUpdate = false;

                switch (ed.PrimaryParentEntityTypeName.ToLowerInvariant())
                {
                    case "party":
                        if (instance.OtherColumns.TryGetValue("Id", out var id) && id != null)
                        {
                            var partyId = await context.GetPartyIdAsync(id);
                            if (partyId is null)
                            {
                                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Property Id {id} can not be found."));
                                groupSuccess.IncrementErrorCount();
                                continue;
                            }
                            entity[identityName] = partyId;
                            entity.Identity = new IdentityData(ed.EntityTypeName, partyId);

                            if (objectType == BOObjectType.Single)
                            {
                                var response = await service.FindByIdAsync(partyId);
                                if (response.IsSuccessStatusCode)
                                {
                                    isUpdate = true;
                                    entity = response.Result;
                                }
                            }
                        }
                        else
                        {
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Could not find Id"));
                            groupSuccess.IncrementErrorCount();
                            continue;
                        }
                        break;

                    case "standalone":
                        id = instance.OtherColumns[identityName];
                        entity[identityName] = id;
                        entity.Identity = new IdentityData(ed.EntityTypeName, id);
                        break;

                    case "group":
                        id = instance.OtherColumns[identityName];
                        var groupId = await GetGroupIdAsync(context, row, id);
                        if (groupId is null)
                        {
                            groupSuccess.IncrementErrorCount();
                            continue;
                        }
                        entity[identityName] = groupId;
                        entity.Identity = new IdentityData(ed.EntityTypeName, groupId);

                        if (ed.ObjectType == BOObjectType.Single)
                        {
                            var response2 = await service.FindByIdAsync(groupId);
                            if (response2.IsSuccessStatusCode)
                            {
                                isUpdate = true;
                                entity = response2.Result;
                            }
                        }
                        break;

                    default:
                        break;
                }
                var abortRow = false;
                foreach (var column in instance.OtherColumns)
                {
                    if (!ed.Properties.Contains(column.Key))
                        continue; // bad column data s/b caught in validation
                    EntityPropertyDefinitionData property = ed.Properties[column.Key];
                    var stringValue = column.Value;
                    if (stringValue is null)
                        continue;
                    object value = null;

                    if (context.Platform == Platform.V10
                             && entityTypeName.EqualsOrdinalIgnoreCase("Activity")
                             && (property.Name.EqualsOrdinalIgnoreCase("CO_ID")
                                 || property.Name.EqualsOrdinalIgnoreCase("SOLICITOR_ID")
                                 || property.Name.EqualsOrdinalIgnoreCase("OTHER_ID")))
                    {
                        // If one of the Activity *_ID columns has been specified then map it to the correct party ID based on the worksheet ID
                        // on this row, and use that instead.
                        var pid = await context.GetPartyIdAsync(stringValue);
                        if (pid is null)
                        {
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Id {value} can not be found. Row will be ignored."));
                            groupSuccess.IncrementErrorCount();
                            abortRow = true;
                            continue;
                        }
                        value = pid;
                    }
                    else
                    {
                        if (property is PropertyTypeStringData stringProperty)
                        {
                            value = stringProperty.MaxLength > 0 ? stringValue.Truncate(stringProperty.MaxLength) : stringValue;
                            if (context.Platform == Platform.V100)
                            {
                                //For v100 SchoolName requires the guid as the reference value.
                                if (stringProperty.Name.EqualsOrdinalIgnoreCase("SchoolName"))
                                {
                                    var resultFind = await partySummaryService.FindSingleAsync(
                                                CriteriaData.Equal("FullName", stringValue),
                                                CriteriaData.IsTrue("IsOrganization"));
                                    value = resultFind.IsSuccessStatusCode ? resultFind.Result.PartyId : null;
                                }
                            }
                        }
                        else
                        {
                            if (!Utility.TryConvert(stringValue, property.PropertyType, context.Culture, out value))
                            {
                                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, row, $"Invalid data in column {property.Name}, value: {stringValue}"));
                            }
                        }
                    }
                    if (value != null) entity[property.Name] = value;
                }
                if (abortRow)
                    continue;
                var iuRow = new InsertUpdateRow<GenericEntityData>
                {
                    Row = row,
                    DataContract = entity
                };
                if (isUpdate)
                {
                    updates.Add(iuRow);
                }
                else
                {
                    inserts.Add(iuRow);
                }
            }
            if (inserts.Count > 0)
            {
                await InsertBatchAsync(context, dataSource, groupSuccess, service, inserts);
            }
            if (updates.Count > 0)
            {
                await UpdateBatchAsync(context, dataSource, groupSuccess, service, updates);
            }
        }

        protected override async Task<IServiceResponse> PublishDataSourceAsync(PublishContext context, ProcessingMode processingMode, DataSourceInfo dataSource, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action, GroupSuccess groupSuccess)
        {
            var ed = await GetEntityDefinitionDataAsync(context, dataSource.Name);
            if (ed is null)
            {
                if (ed is null)
                {
                    var message = $"Can't find entity defintion {dataSource.Name}";
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, dataSource, message));
                    return new ServiceResponse(StatusCode.ValidationError) { Message = message };
                }
            }
            return await base.PublishDataSourceAsync(context, processingMode, dataSource, action, groupSuccess);
        }

        protected override async Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var ed = await GetEntityDefinitionDataAsync(context, dataSourceInfo.Name);
            var validator = new RelatedDataValidator(context, ed);
            await ValidateBatch(context, batch, groupSuccess, validator);
        }

        private static string GetIdentityName(EntityDefinitionData ed)
        {
            // If, for some weird reason, both identity keys are in the properties, ID wins.
            // We don't look in EntityDefinitionData.IdentityPropertyDefinitions because in some cases, like v10 Activity processing,
            // PartyId is not there, but is in the Properties collection, and when validating, we don't want a 'false negative' that causes
            // an error.
            return IdNames.FirstOrDefault(p => ed.Properties.Any(x => p.EqualsOrdinalIgnoreCase(x.Name)));
        }

        private async Task<BOEntityDefinitionData> GetEntityDefinitionDataAsync(PublishContext context, string entityName)
        {
            if (EntityDefinitions.TryGetValue(entityName, out var ed)) return ed;
            var service = ClientFactory.Create<IBOEntityDefinitionService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            var response = await service.FindByIdAsync(entityName);
            if (response.IsSuccessStatusCode)
            {
                ed = response.Result;
                EntityDefinitions.TryAdd(entityName, ed);
                return ed;
            }
            return null;
        }

        private async Task<string> GetGroupIdAsync(PublishContext context, ImportRow row, string groupName)
        {
            if (_groupIds.TryGetValue(groupName, out var id))
                return id;
            var service = ClientFactory.Create<IGroupSummaryService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            var response = await service.FindAllAsync(CriteriaData.Equal("Name", groupName));
            if (response.IsSuccessStatusCode)
            {
                if (response.Result.Count == 1)
                {
                    id = response.Result[0].GroupId;
                    _groupIds.TryAdd(groupName, id);
                    return id;
                }
                if (response.Result.Count == 0)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Group does not exist."));
                    return null;
                }
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Multiple Groups exist with the same name."));
                return null;
            }
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, response.Message));
            return null;
        }
    }
}