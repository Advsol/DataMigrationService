using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.GiftAid
{
    
    public class GiftAidDataSourcePublisher : DataSourcePublisherBase
    {
        public GiftAidDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "GiftAid";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party" };

        public override string Title => "Gift Aid Declarations Import File";
        public override bool IsHarvester => false;

        public override bool IsValidatable => true;

        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        public override ImportTemplate CreateImportTemplateInstance() => new GiftAidImportTemplate();

        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            foreach (var row in batch)
            {
                var instance = CreateImportTemplateInstance();
                await MapSourceToImportTemplateAsync(context, row, instance);
                var valid = await PublishRowAsync(context, row, instance);
                if (valid)
                    groupSuccess.IncrementSuccessCount();
                else
                    groupSuccess.IncrementErrorCount();
            }
        }

        protected override async Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var validator = new GiftAidValidator(context);
            await ValidateBatch(context, batch, groupSuccess, validator);
        }

        private static async Task<bool> UpdateGiftAidAsync(PublishContext context, GiftAidImportTemplate import, PartyData party, ImportRow row)
        {
            // we will either add or update the first GAD
            if (Enum.TryParse<GiftAidDeclarationMethodOfDeclarationData>(import.MethodOfDeclaration, out var method))
            {
                var declaration = new GiftAidDeclarationData
                {
                    MethodOfDeclaration = method,
                    DeclarationReceived = import.DeclarationReceived,
                    IsOngoing = import.Future,
                    IsPast = import.Past,
                    ConfirmationLetterSent = import.ConfirmationLetterSent
                };
                if (party.FinancialInformation is null)
                    party.FinancialInformation = new FinancialInformationData();
                if (party.FinancialInformation.GiftAidInformation is null)
                    party.FinancialInformation.GiftAidInformation = new GiftAidDeclarationDataCollection();
                if (party.FinancialInformation.GiftAidInformation.Count > 0)
                {
                    var existing = party.FinancialInformation.GiftAidInformation[0];
                    existing.MethodOfDeclaration = declaration.MethodOfDeclaration;
                    existing.DeclarationReceived = declaration.DeclarationReceived;
                    existing.IsOngoing = declaration.IsOngoing;
                    existing.IsPast = declaration.IsPast;
                    existing.ConfirmationLetterSent = declaration.ConfirmationLetterSent;
                }
                else
                {
                    party.FinancialInformation.GiftAidInformation.Add(declaration);
                }

                return true;
            }
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Could not convert GiftAid declaration type."));
            return false;
        }

        private async Task<bool> PublishRowAsync(PublishContext context, ImportRow row, object instance)
        {
            var importTemplate = (GiftAidImportTemplate)instance;
            var importId = importTemplate.Id;
            if (string.IsNullOrEmpty(importId))
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Id is required."));
                return false;
            }
            var partyId = await context.GetPartyIdAsync(importId);
            if (partyId is null)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Id is not found."));
                return false;
            }

            var service = ClientFactory.Create<IPartyService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            var response = await service.FindByIdAsync(partyId);
            if (!response.IsSuccessStatusCode)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Id is not found."));
                return false;
            }
            var party = response.Result;
            await UpdateGiftAidAsync(context, importTemplate, party, row);
            response = await service.UpdateAsync(party);
            if (response.IsSuccessStatusCode) return true;
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, response.Message));
            return false;
        }
    }
}