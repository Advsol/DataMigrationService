using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.GroupMembership
{

    public class GroupMembershipDataSourcePublisher : DataSourcePublisherBase
    {
        public GroupMembershipDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "GroupMembership";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party", "GroupDefinition" };

        public override string Title => "Group Membership Import File";
        public override bool IsHarvester => false;
        public override bool IsValidatable => true;
        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        // Grabbed and modified from GroupMemberDetailController
        public static bool DatesOverlap(GroupMemberDetailData existingDetail, DateTime? effectiveDate, DateTime? expirationDate)
        {
            var detailEffectiveDate = existingDetail.EffectiveDate ?? DateTime.Today.Date;
            var detailExpirationDate = existingDetail.ExpirationDate ?? DateTime.MaxValue.Date;
            return DateInRange(effectiveDate.Value, detailEffectiveDate, detailExpirationDate)
                   || DateInRange(expirationDate.Value, detailEffectiveDate, detailExpirationDate)
                   || (DateInRange(detailEffectiveDate, effectiveDate.Value, expirationDate.Value)
                        && DateInRange(detailExpirationDate, effectiveDate.Value, expirationDate.Value));
        }

        public override ImportTemplate CreateImportTemplateInstance() => new GroupMembershipImportTemplate();

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSource, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var inserts = new List<InsertUpdateRow<GroupMemberData>>();
            var updates = new List<InsertUpdateRow<GroupMemberData>>();
            var groupMemberService = ClientFactory.Create<IGroupMemberService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            foreach (var row in batch)
            {
                var instance = (GroupMembershipImportTemplate)CreateImportTemplateInstance();
                await MapSourceToImportTemplateAsync(context, row, instance);
                var partyId = context.GetMappedPartyId(instance.Id);
                if (partyId == null)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Could not find Id {instance.Id}"));
                    groupSuccess.IncrementErrorCount();
                    continue;
                }

                if (context.Groups.TryGetValue(instance.GroupName, out var group))
                {
                    var response = await groupMemberService.FindSingleAsync(CriteriaData.Equal("PartyId", partyId),
                        CriteriaData.Equal("GroupId", group.GroupId));
                    var isInsert = !response.IsSuccessStatusCode;
                    GroupMemberData gm;
                    if (isInsert)
                    {
                        gm = new GroupMemberData
                        {
                            Group = @group,
                            Party = new PartySummaryData { PartyId = partyId },
                            MembershipDetails = new GroupMemberDetailDataCollection()
                        };
                        inserts.Add(new InsertUpdateRow<GroupMemberData> { Row = row, DataContract = gm });
                    }
                    else
                    {
                        gm = response.Result;
                        updates.Add(new InsertUpdateRow<GroupMemberData> { Row = row, DataContract = gm });
                    }

                    var role = instance.Role;
                    if (!group.IsSimpleGroup && @group.Roles != null && @group.Roles.Count > 0)
                    {
                        GroupRoleData gr = role == null ? @group.Roles.FirstOrDefault() :
                            @group.Roles.FirstOrDefault(r => r.Name.EqualsOrdinalIgnoreCase(role));
                        if (gr == null)
                        {
                            gr = @group.Roles[0];
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, row, string.Format(CultureInfo.CurrentCulture,
                                "Role \"{0}\" does not exist for group \"{1}.\"  The role \"{2}\" will be used.",
                                role,
                                @group.Name,
                                gr.Name)));
                        }

                        await CheckTermsOfDetailsWithSameRoleAsync(context,
                            gm,
                            gr.Name,
                            instance.EffectiveDate,
                            instance.ExpirationDate,
                            row);

                        gm.MembershipDetails.Add(new GroupMemberDetailData
                        {
                            Role = gr,
                            EffectiveDate = instance.EffectiveDate,
                            ExpirationDate = instance.ExpirationDate
                        });
                    }
                }
            }
            if (inserts.Count > 0)
            {
                await InsertBatchAsync(context, dataSource, groupSuccess, groupMemberService, inserts);
            }
            if (updates.Count > 0)
            {
                await UpdateBatchAsync(context, dataSource, groupSuccess, groupMemberService, updates);
            }
        }

        protected override async Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var validator = new GroupMembershipValidator(context);
            await ValidateBatch(context, batch, groupSuccess, validator);
        }

        private static async Task CheckTermsOfDetailsWithSameRoleAsync(PublishContext context, GroupMemberData groupMemberData, string role, DateTime? effectiveDate, DateTime? expirationDate, ImportRow row)
        {
            var effective = effectiveDate ?? DateTime.Today.Date;
            var expiration = expirationDate ?? DateTime.MaxValue.Date;
            var detailsWithRoleWithOverlappingDates = groupMemberData.MembershipDetails
                .Where(d => !string.IsNullOrWhiteSpace(d.GroupMemberDetailId)
                       && d.Role.Name.EqualsOrdinalIgnoreCase(role)
                       && DatesOverlap(d, effective, expiration))
                .ToArray();

            const string noEndDate = "'no end date'";

            if (detailsWithRoleWithOverlappingDates.Length <= 0)
                return;

            var existingTerms = detailsWithRoleWithOverlappingDates
                .OrderBy(d => d.EffectiveDate)
                .Aggregate("",
                    (first, next) => string.Format(context.Culture,
                        "{0} [{1:d}-{2}]",
                        first,
                        next.EffectiveDate,
                        next.ExpirationDate == null || next.ExpirationDate >= DateTime.MaxValue.Date
                            ? noEndDate
                            : next.ExpirationDate.Value.ToShortDateString()));

            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, row, string.Format(CultureInfo.CurrentCulture,
                "For ID '{0}' group '{1}' role '{2}', the membership term [{3:d}-{4}] overlaps with the following existing term(s): {5}. These will be merged into a single membership term.",
                groupMemberData.Party.Id,
                groupMemberData.Group.Name,
                role,
                effective,
                expiration == DateTime.MaxValue.Date ? noEndDate : expiration.ToShortDateString(),
                existingTerms)));
        }

        // Grabbed and modified from GroupMemberDetailController
        private static bool DateInRange(DateTime date, DateTime start, DateTime end)
        {
            return start <= date && date <= end;
        }
    }
}