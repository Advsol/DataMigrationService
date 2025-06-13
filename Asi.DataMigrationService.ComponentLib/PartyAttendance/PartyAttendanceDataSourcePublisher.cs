using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Membership.DataContracts.Groups;
using Asi.Soa.Membership.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.ComponentLib.PartyAttendance
{
    public class PartyAttendanceDataSourcePublisher : DataSourcePublisherBase
    {
        public PartyAttendanceDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public IList<AttendanceTypeRefData> AttendanceTypeRefs { get; set; }
        public override string DataSourceTypeName => "PartyAttendance";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party" };

        public override string Title => "Party Attendance Import File";
        public override bool IsHarvester => false;
        public override bool IsValidatable => true;
        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        public override ImportTemplate CreateImportTemplateInstance() => new PartyAttendanceImportTemplate();

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSource, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var list = new List<InsertUpdateRow<PartyAttendanceData>>();
            foreach (var row in batch)
            {
                var instance = (PartyAttendanceImportTemplate)CreateImportTemplateInstance();
                await MapSourceToImportTemplateAsync(context, row, instance);
                var partyId = await context.GetPartyIdAsync(instance.Id);
                if (partyId == null)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, $"Could not find Id {instance.Id}"));
                    groupSuccess.IncrementErrorCount();
                    continue;
                }
                var attendance = new PartyAttendanceData
                {
                    PartyId = partyId.ToGuid(),
                    AttendanceTypeCode = instance.AttendanceTypeCode.NullTrim(),
                    AttendanceDate = instance.AttendanceDate.GetValueOrDefault(),
                    Description = instance.Description.NullTrim(),
                    IsCheckedIn = instance.IsCheckedIn,
                    OrganizationKey = (await context.GetOrganizationIdByNameAsync(instance.OrganizationName)).ToGuid()
                };
                list.Add(new InsertUpdateRow<PartyAttendanceData> { Row = row, DataContract = attendance });
            }
            if (list.Count > 0)
            {
                var service = ClientFactory.Create<IPartyAttendanceService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                await InsertBatchAsync(context, dataSource, groupSuccess, service, list);
            }
        }

        protected override async Task<IServiceResponse> PublishDataSourceAsync(PublishContext context, ProcessingMode processingMode, DataSourceInfo dataSource, Func<PublishContext, DataSourceInfo, IList<ImportRow>, GroupSuccess, Task> action, GroupSuccess groupSuccess)
        {
            if (AttendanceTypeRefs is null)
            {
                var service = ClientFactory.Create<IAttendanceTypeRefService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                var response = await service.FindAllAsync();
                if (response.IsSuccessStatusCode)
                    AttendanceTypeRefs = response.Result;
            }
            return await base.PublishDataSourceAsync(context, processingMode, dataSource, action, groupSuccess);
        }

        protected override async Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var validator = new PartyAttendanceValidator(context, AttendanceTypeRefs);
            await ValidateBatch(context, batch, groupSuccess, validator);
        }
    }
}