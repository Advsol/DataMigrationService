using Asi.DataMigrationService.ComponentLib.PartyAddress;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.DataMigrationService.Lib.Publisher.Party;
using Asi.DataMigrationService.Lib.Services;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Core.ServiceContracts;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.DataContracts.Organization;
using Asi.Soa.Membership.ServiceContracts;
using FluentValidation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.ComponentLib.Party
{
    public class PartyDataSourcePublisher : DataSourcePublisherBase
    {
        public IList<GenTableData> Categories = new List<GenTableData>();
        public IList<LegacyCustomerTypeData> MemberTypes = new List<LegacyCustomerTypeData>();
        public ConcurrentDictionary<string, string> Prefixes = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> Suffixes = new ConcurrentDictionary<string, string>();

        public PartyDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "Party";
        public override IList<string> DependentPublisherTypeNames => new List<string> { "MembershipSettings", "PartyPrefix", "PartySuffix" };
        public override string Title => "Party Import File";
        public override bool IsHarvester => false;

        public override bool IsValidatable => true;

        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        public override ImportTemplate CreateImportTemplateInstance() => new PartyImportTemplate();

        public override async Task InitializeAsync(PublishContext context)
        {
            await base.InitializeAsync(context);

            if (context.Platform == Platform.V10)
            {
                var memberTypeService = ClientFactory.Create<ILegacyCustomerTypeService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                var response4 = await memberTypeService.FindAllAsync();
                if (response4.IsSuccessStatusCode)
                    MemberTypes = response4.Result;

                var genTableService = ClientFactory.Create<IGenTableService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                var response5 = await genTableService.FindAllAsync(CriteriaData.Equal("TableName", "CATEGORY"));
                if (response5.IsSuccessStatusCode)
                    Categories = response5.Result;
            }
        }

        public override async Task<IServiceResponse<GroupSuccess>> PublishAsync(PublishContext context, ManifestDataSourceType dataSourceType)
        {
            var groupSuccess = new GroupSuccess();

            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType, $"Begin processing for {dataSourceType.DataSourceTypeName}."));

            await UpdatePrefixReferenceAsync(context);
            await UpdateSuffixReferenceAsync(context);

            // for Party, we have to process in levels; generally to process organizations and and related first
            var depths = context.PartyMaps.Values.Select(p => p.Depth).Distinct().OrderBy(p => p);
            foreach (var depth in depths)
            {
                var batch = new List<PartyMap>();
                var actionBlock = new ActionBlock<Func<Task>>(action => action.Invoke()
                   , new ExecutionDataflowBlockOptions
                   {
                       MaxDegreeOfParallelism = context.MaxDegreeOfParallelism,
                       BoundedCapacity = context.MaxDegreeOfParallelism * 2 // number of queued batches
                   });
                foreach (var partyMap in context.PartyMaps.Values)
                {
                    if (partyMap.Depth == depth)
                    {
                        batch.Add(partyMap);
                        if (batch.Count >= context.BatchSize)
                        {
                            var batch1 = new List<PartyMap>(batch);
                            await actionBlock.SendAsync(() => PublishBatchAsync(context, batch1, groupSuccess), context.CancellationToken);
                            batch.Clear();
                            if (context.CancellationToken.IsCancellationRequested)
                                return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
                        }
                    }
                }
                if (batch.Count > 0)
                {
                    var batch1 = new List<PartyMap>(batch);
                    await actionBlock.SendAsync(() => PublishBatchAsync(context, batch1, groupSuccess), context.CancellationToken);
                }
                actionBlock.Complete();
                await actionBlock.Completion;
            }
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Information, dataSourceType,
                $"Completed processing for {dataSourceType.DataSourceTypeName}.Success: {groupSuccess.SuccessCount}, Errors: {groupSuccess.ErrorCount}, Elapsed time: {groupSuccess.ElapsedTime}"));
            return new ServiceResponse<GroupSuccess> { Result = groupSuccess };
        }

        public override async Task<IServiceResponse<GroupSuccess>> ValidateAsync(PublishContext processorContext, ManifestDataSourceType dataSourceType)
        {
            var response = await base.ValidateAsync(processorContext, dataSourceType);
            if (!response.IsSuccessStatusCode)
                return response;
            var response2 = await ValidateOrgIdsAndDepthAsync(processorContext);
            if (!response2)
                response.Result.ErrorCount++;
            return response;
        }

        protected override Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            throw new NotSupportedException();
        }

        protected override async Task ValidateBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            var validator = new PartyValidator(context, this);
            foreach (var row in batch)
            {
                var instance = Activator.CreateInstance<PartyImportTemplate>();
                await MapSourceToImportTemplateAsync(context, row, instance);
                var valid = await ValidateRowAsync(validator, context, row, instance);
                if (valid)
                    groupSuccess.IncrementSuccessCount();
                else
                    groupSuccess.IncrementErrorCount();
            }
        }

        private static async Task LinkToRelatedContactAsync(PublishContext context, PartyMap partyMap, PartyData party)
        {
            if (context.PartyMaps.TryGetValue(partyMap.RelatedSourceId, out var linkedMap))
            {
                var linkedPartyId = linkedMap.PartyId;
                if (string.IsNullOrWhiteSpace(linkedPartyId))
                {
                    var meessage = $"Skipping linking of a contact (ID={partyMap.SourceId}, to an organization (ID={partyMap.RelatedSourceId}), the related Organization has not been imported yet.";

                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Warning, meessage));
                    //linksSkipped++;
                }
                else
                {
                    if (party is IPersonData person)
                    {
                        if (person.PrimaryOrganization == null)
                            person.PrimaryOrganization = new PrimaryOrganizationInformationData();
                        person.PrimaryOrganization.OrganizationPartyId = linkedPartyId;

                        if (context.Platform == Platform.V100 && partyMap.IsClubMember && linkedPartyId != null)
                        {
                            person.PrimaryOrganization.OrganizationPartyId = string.Empty;
                            ((IPersonData)party).PrimaryClubPartyId = linkedPartyId;
                        }

                        //links++;
                        if (context.Platform == Platform.V10)
                        {
                            party.AdditionalAttributes["ParentPartyId"].Value = linkedPartyId;
                        }
                    }
                }
            }
        }

        private static void ProcessV10AdditionalAttributes(PublishContext context, PartyImportTemplate import, PartyData party)
        {
            if (party.AdditionalAttributes == null) party.AdditionalAttributes = new GenericPropertyDataCollection();
            var memberType = import.MemberType;
            if (memberType != null)
                party.AdditionalAttributes.SetPropertyValue("CustomerTypeCode", memberType.ToUpperInvariant());

            var category = import.Category;
            if (category != null)
                party.AdditionalAttributes.SetPropertyValue("BillingCategory", category);

            if (context.ProjectInfo.AutoAssignPartyId)
                party.AdditionalAttributes.SetPropertyValue("MajorKey", import.Id);
        }

        private static async Task UpdatePartyFromImportAsync(PublishContext context, PartyImportTemplate import, PartyData party, PartyMap partyMap)
        {
            var partyType = PartyValidator.GetPartyTypeCode(import.PartyType);
            if (party is IPersonData person)
            {
                if (person.PersonName == null) person.PersonName = new PersonNameData();
                var personName = person.PersonName;
                if (import.NamePrefix != null) personName.NamePrefix = import.NamePrefix;
                if (import.FirstName != null) personName.FirstName = import.FirstName;
                if (import.MiddleName != null) personName.MiddleName = import.MiddleName;
                if (import.LastName != null) personName.LastName = import.LastName;
                if (import.NameSuffix != null) personName.NameSuffix = import.NameSuffix;
                if (import.Designation != null) personName.Designation = import.Designation;
                if (import.InformalName != null) personName.InformalName = import.InformalName;
                if (import.Gender != null) person.Gender = new GenderData() { Code = import.Gender };
                if (import.BirthDate != null) person.BirthDate = import.BirthDate;

                if (import.PrimaryOrganizationId != null)
                {
                    if (person.PrimaryOrganization == null) person.PrimaryOrganization = new PrimaryOrganizationInformationData();
                    person.PrimaryOrganization.OrganizationPartyId = context.GetMappedPartyId(import.PrimaryOrganizationId);
                }
                if (import.PrimaryOrganizationTitle != null)
                {
                    if (person.PrimaryOrganization == null) person.PrimaryOrganization = new PrimaryOrganizationInformationData();
                    person.PrimaryOrganization.Title = import.PrimaryOrganizationTitle;
                }
                if (import.PrimaryClubId != null)
                    person.PrimaryClubPartyId = context.GetMappedPartyId(import.PrimaryClubId);
            }

            if (party is IOrganizationData organization)
            {
                if (import.OrganizationName != null) organization.OrganizationName = import.OrganizationName;

                if (context.Platform == Platform.V10)
                {
                    if (partyType.Equals("CLUB", StringComparison.InvariantCultureIgnoreCase))
                    {
                        organization.OrganizationType = new OrganizationTypeData
                        {
                            OrganizationTypeCode = 2,
                            Name = "Club",
                            GroupTypeKey = new Guid("7926FED7-DA4D-4606-BF08-0A507B6263A6") // System default Club GroupType key
                        };
                    }
                    //If the partyType is CHAPTER set the organization to be a Chapter
                    else if (partyType.Equals("CHAPTER", StringComparison.InvariantCultureIgnoreCase))
                    {
                        organization.OrganizationType = new OrganizationTypeData
                        {
                            OrganizationTypeCode = 1,
                            Name = "Chapter",
                            GroupTypeKey = new Guid("FB12E0D1-D9E3-45F4-A4B0-3322691E9191") // System default Chapter GroupType key
                        };
                    }
                }
            }

            PartyAddressDataSourcePublisher.UpdateAddress(context, import, party);

            if (context.Platform == Platform.V10)
                ProcessV10AdditionalAttributes(context, import, party);
            if (import.MobilePhone != null)
            {
                (party.Phones ??= new PhoneDataCollection()).SetPhone(new PhoneData { PhoneType = "Mobile", Number = import.MobilePhone });
            }
            if (context.Platform == Platform.V100 && import.ReceiptPreference != null)
            {
                if (party.FinancialInformation is null)
                    party.FinancialInformation = new FinancialInformationData();
                if (PartyValidator.TryGetReceiptPreference(import.ReceiptPreference, out var receiptPreference))
                    party.FinancialInformation.DonorReceiptPreference = receiptPreference;
            }
            if (context.Platform == Platform.V10)
            {
                var primaryAddress = party.GetPreferredAddress("mail") ?? party.GetPreferredAddress("default");
                if (primaryAddress?.Email != null)
                {
                    (party.Emails ??= new EmailDataCollection()).SetEmail(new EmailData { Address = primaryAddress.Email, EmailType = "_Primary", IsPrimary = true });
                }
            }
            if (partyMap.RelatedSourceId != null)
            {
                await LinkToRelatedContactAsync(context, partyMap, party);
            }
        }

        private static async Task<bool> ValidateOrgIdsAndDepthAsync(PublishContext context)
        {
            var fatalError = false; // Assume all is well
            const int maxDepth = 10;

            // Pre-load the party map collection with possibly existing organizations.
            var relatedMaps =
                context.PartyMaps.Values.Where(m => m.RelatedSourceId != null).ToList();

            foreach (var relatedMap in relatedMaps)
            {
                var partyId = await context.GetPartyIdAsync(relatedMap.RelatedSourceId);      // Don't care about return value. Mainly just after populating missing PartyMaps
                if (partyId is null)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, relatedMap.DataSourceRowReference, $"RelatedId {relatedMap.RelatedSourceId} not fond in import or destination system."));
                    fatalError = true;
                }
            }
            foreach (var partyMap in context.PartyMaps.Values)
            {
                partyMap.Depth = 0;
                var relatedId = partyMap.RelatedSourceId;
                if (relatedId != null)
                {
                    if (context.PartyMaps.ContainsKey(relatedId))
                    {
                        // calculate depth
                        while (!string.IsNullOrWhiteSpace(relatedId) && partyMap.Depth < maxDepth)
                        {
                            partyMap.Depth++;
                            relatedId = context.PartyMaps.TryGetValue(relatedId, out var relatedMap) ? relatedMap.RelatedSourceId : null;
                        }
                        if (partyMap.Depth == maxDepth)
                        {
                            var message = $"PrimaryOrganizationId \"{partyMap.SourceId}\" refers to an Organization in a circular hierarchy.";
                            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, message));
                            fatalError = true;
                        }
                    }
                    else
                    {
                        var message = $"PrimaryOrganizationId {partyMap.SourceId} refers to an Organization that has not been defined.";
                        await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, message));
                        fatalError = true;
                    }
                }
            }
            return !fatalError;
        }

        private static async Task<bool> ValidateRowAsync(PartyValidator validator, PublishContext context, ImportRow row, object instance)
        {
            var importTemplate = (PartyImportTemplate)instance;
            var sourceId = importTemplate.Id;
            if (string.IsNullOrEmpty(sourceId))
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Id is required."));
                return false;
            }

            var existingParty = await context.GetExistingPartyAsync(importTemplate.Id);

            if (existingParty != null && !context.ProjectInfo.AllowUpdates)
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Party already exists and AllowUpdates not specified."));
                return false;
            }

            if (context.PartyMaps.ContainsKey(sourceId))
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Duplicate Id."));
                return false;
            }

            var partyType = PartyValidator.GetPartyTypeCode(importTemplate.PartyType);
            var isPerson = partyType?.EqualsOrdinalIgnoreCase("P") ?? true;

            var partyMap = new PartyMap
            {
                SourceId = sourceId,
                DataSourceRowReference = row.ImportRowReference,
                PartyId = existingParty?.PartyId,
                IsOrganization = !isPerson,
                RelatedSourceId = importTemplate.PrimaryClubId.NullTrim()?.ToUpperInvariant()
            };

            var ruleSet =  new List<string> { "default" + context.Platform.ToString() };
            if (partyType != null)
            {
                if (isPerson)
                {
                    ruleSet.Add("person");
                }
                else
                {
                    ruleSet.Add( "organization");
                    partyMap.IsOrganization = true;
                }
            }

            if (context.Platform == Platform.V100 && !string.IsNullOrWhiteSpace(importTemplate.PrimaryClubId)
                && !importTemplate.PrimaryClubId.EqualsOrdinalIgnoreCase(sourceId))
            {
                partyMap.RelatedSourceId = importTemplate.PrimaryClubId.ToUpperInvariant();
                if (isPerson)
                    partyMap.IsClubMember = true;
            }
            context.PartyMaps.TryAdd(partyMap.SourceId, partyMap);

            var results = await validator.ValidateAsync(importTemplate, options => options.IncludeRuleSets(ruleSet.ToArray()));

            if (results.IsValid)
                return true;
            await LogValidationErrors(context, row, results);

            return false;
        }

        private async Task<PartyData> ConvertRowAsync(PublishContext context, ImportRow row, ImportTemplate instance)
        {
            var importTemplate = (PartyImportTemplate)instance;
            PartyData party = null;

            if (!context.PartyMaps.TryGetValue(importTemplate.Id, out var partyMap))
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Could not retrieve PartyMap."));
                return null;
            }

            var isNew = partyMap.PartyId == null;
            if (!isNew)
            {
                var partyService = ClientFactory.Create<IPartyService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                var partyResponse = await partyService.FindByIdAsync(partyMap.PartyId);
                if (partyResponse.IsSuccessStatusCode)
                    party = partyResponse.Result;
            }
            else
            {
                var partyType = PartyValidator.GetPartyTypeCode(importTemplate.PartyType);
                if (partyType != null)
                {
                    if (partyType == "P")
                    {
                        var person = new PersonData { PersonName = new PersonNameData() };
                        party = person;
                    }
                    else
                    {
                        var organization = new OrganizationData();
                        party = organization;
                    }
                }
            }
            await UpdatePartyFromImportAsync(context, importTemplate, party, partyMap);

            return party;
        }

        private async Task CreateAccountAsync(PublishContext context, PartyData party)
        {
            var primaryAddress = party.GetPreferredAddress("mail") ?? party.GetPreferredAddress("default");
            if (primaryAddress?.Email != null)
            {
                var email = primaryAddress.Email.ToUpperInvariant();
                var service = ClientFactory.Create<IUserService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
                var response = await service.AddAsync(new UserData { UserId = party.PartyId, UserName = email });
                if (!response.IsSuccessStatusCode)
                {
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, response.Message));
                }
            }
        }

        private async Task PublishBatchAsync(PublishContext context, IList<PartyMap> batch, GroupSuccess groupSuccess)
        {
            var inserts = new List<InsertUpdateRow<PartyData>>();
            var updates = new List<InsertUpdateRow<PartyData>>();
            foreach (var partyMap in batch)
            {
                var row = await context.GetRowAsync(partyMap.DataSourceRowReference);
                if (row != null)
                {
                    var instance = CreateImportTemplateInstance();

                    var success = await MapSourceToImportTemplateAsync(context, row, instance);
                    if (success)
                    {
                        var party = await ConvertRowAsync(context, row, instance);
                        var isNew = party.PartyId is null;
                        var insertUpdateRow = new InsertUpdateRow<PartyData> { PartyMap = partyMap, DataContract = party, Row = row };
                        if (isNew)
                            inserts.Add(insertUpdateRow);
                        else
                            updates.Add(insertUpdateRow);
                    }
                    if (context.CancellationToken.IsCancellationRequested)
                        return;
                }
            }

            // use bulk if available. Separate insert from update
            var partyService = ClientFactory.Create<IPartyService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);
            if (inserts.Count > 0)
            {
                await InsertBatchAsync(context, null, groupSuccess, partyService, inserts);
                foreach (var item in inserts)
                {
                    item.PartyMap.PartyId = item.ResultKey?.ToString();
                    if (context.ProjectInfo.CreateAccounts)
                    {
                        var party = item.DataContract;
                        party.PartyId = item.PartyMap.PartyId;
                        if (party.PartyId != null)
                        {
                            await CreateAccountAsync(context, party);
                        }
                    }
                }
            }
            if (updates.Count > 0)
            {
                await UpdateBatchAsync(context, null, groupSuccess, partyService, inserts);
            }
        }

        /// <summary>
        /// Updates the prefix reference.
        /// </summary>
        /// <param name="context">The instance to use for party maps, related processing, etc.</param>
        /// <returns><c>true</c> if there are no errors, <c>false</c> otherwise.</returns>
        private async Task<bool> UpdatePrefixReferenceAsync(PublishContext context)
        {
            var fatalError = false;
            if (context.Platform == Platform.V10)
                return true;

            var prefixService = ClientFactory.Create<INamePrefixService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);

            foreach (var prefix in Prefixes.Values)
            {
                var prefixCode = prefix.Length > 25 ? prefix.Substring(0, 25) : prefix;
                var prefixDesc = prefix.Length > 30 ? prefix.Substring(0, 30) : prefix;
                var response = await prefixService.FindSingleAsync(CriteriaData.Equal("Code", prefixCode));
                if (response.StatusCode == StatusCode.NotFound)
                {
                    response = await prefixService.AddAsync(new NamePrefixData { Code = prefixCode, Description = prefixDesc });
                }
                if (!response.IsSuccessStatusCode)
                {
                    fatalError = true;
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, $"Error adding prefix {prefixCode}, Error: {response.Message}"));
                }
            }
            return !fatalError;
        }

        /// <summary>
        /// Updates the suffix reference.
        /// </summary>
        /// <param name="context">The instance to use for party maps, related processing, etc.</param>
        /// <returns><c>true</c> if there are no errors, <c>false</c> otherwise.</returns>
        private async Task<bool> UpdateSuffixReferenceAsync(PublishContext context)
        {
            var fatalError = false;
            if (context.Platform == Platform.V10)
                return true;

            var suffixService = ClientFactory.Create<INameSuffixService>(context.TargetLoginInformation.Uri, context.TargetLoginInformation.UserCredentials);

            foreach (var suffix in Suffixes.Values)
            {
                var suffixCode = suffix.Length > 10 ? suffix.Substring(0, 10) : suffix;
                var suffixDesc = suffix.Length > 30 ? suffix.Substring(0, 30) : suffix;
                var response = await suffixService.FindByIdAsync(suffixCode);
                if (response.StatusCode == StatusCode.NotFound)
                {
                    response = await suffixService.AddAsync(new NameSuffixData { Code = suffixCode, Description = suffixDesc });
                }
                if (!response.IsSuccessStatusCode)
                {
                    fatalError = true;
                    await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, $"Error adding suffix {suffixCode}, Error: {response.Message}"));
                }
            }
            return !fatalError;
        }
    }
}