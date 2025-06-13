using Asi.DataMigrationService.ComponentLib.GroupMembership;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.GroupDefinition
{

    public class GroupDefinitionDataSourcePublisher : DataSourcePublisherBase
    {
        public GroupDefinitionDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "GroupDefinition";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party" };

        public override string Title => "Group Definition Import File";
        public override bool IsHarvester => false;
        public override bool IsValidatable => true;
        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        public override ImportTemplate CreateImportTemplateInstance() => new GroupMembershipImportTemplate();

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSource, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            using var scope = context.ServiceProvider.CreateScope();
            var groupService = scope.ServiceProvider.GetRequiredService<IGroupService>();
            var groupClassService = scope.ServiceProvider.GetRequiredService<IGroupClassService>();
            foreach (var row in batch)
            {
                var instance = (GroupDefinitionImportTemplate)CreateImportTemplateInstance();
                await MapSourceToImportTemplateAsync(context, row, instance);

                var response = await groupService.ExistsAsync(CriteriaData.Equal("Name", instance.GroupName));
                if (!response.IsSuccessStatusCode)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, response.Message));
                    groupSuccess.IncrementErrorCount();
                    continue;
                }

                var response2 = await groupClassService.FindSingleAsync(CriteriaData.Equal("Name", instance.GroupClass));
                if (!response2.IsSuccessStatusCode)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"GroupClass: {response2.Message}"));
                    groupSuccess.IncrementErrorCount();
                    continue;
                }

                var group = new GroupData
                {
                    GroupClass = response2.Result,
                    Name = instance.GroupName,
                    Description = instance.Description,
                    IsSimpleGroup = false
                };
                var groupOwnerId = instance.GroupOwnerId;
                if (string.IsNullOrWhiteSpace(groupOwnerId))
                {
                    group.ParentIdentity = new IdentityData("FinancialEntity", "673A2ED2-EC66-4E5A-8453-D16844186C71");
                }
                else
                {
                    var partyId = await context.GetPartyIdAsync(groupOwnerId);
                    if (partyId is null)
                    {
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Could not find GroupOwnerId: {groupOwnerId}"));
                        groupSuccess.IncrementErrorCount();
                        continue;
                    }
                    group.ParentIdentity = group.ParentIdentity = new IdentityData(PartySummaryData.EntityTypeName, partyId);
                }
                var response3 = await groupService.AddAsync(group);
                if (response3.IsSuccessStatusCode)
                {
                    groupSuccess.IncrementSuccessCount();
                    context.Groups.AddOrUpdate(group.Name, group, (key, value) => group);
                }
                else
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Could not add group: {response3.Message}"));
                    groupSuccess.IncrementErrorCount();
                    continue;
                }
            }
        }

        protected override async Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var validator = new GroupDefinitionValidator(context);
            await ValidateBatch(context, batch, groupSuccess, validator);
        }

        protected override async Task<bool> ValidateRowAsync<TImportTemplate>(PublishContext context, ImportRow row, IValidator<TImportTemplate> validator, TImportTemplate instance, string[] ruleSet = null)

        {
            var result = await base.ValidateRowAsync(context, row, validator, instance, ruleSet);
            if (result)
            {
                var groupInstance = instance as GroupDefinitionImportTemplate;
                var groupName = groupInstance.GroupName;
                var group = new GroupData
                {
                    Name = groupInstance.GroupName
                };
                context.Groups.TryAdd(groupName, group);
            }
            return result;
        }
    }
}