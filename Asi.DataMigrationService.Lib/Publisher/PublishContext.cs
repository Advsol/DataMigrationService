using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Data;
using Asi.DataMigrationService.Lib.Data.Models;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.DataMigrationService.Lib.Publisher.Party;
using Asi.Soa.Communications.DataContracts;
using Asi.Soa.Communications.ServiceContracts;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Asi.DataMigrationService.Lib.Publisher
{
    public enum Platform
    {
        V10,
        V100
    }

    public class PublishContext
    {
        private readonly Func<PublishMessage, Task> _errorLogger;
        private string _culture;

        public PublishContext(IServiceProvider serviceProvider, Func<PublishMessage, Task> errorLogger, CancellationToken cancellationToken)
        {
            ServiceProvider = serviceProvider;
            _errorLogger = errorLogger;
            CancellationToken = cancellationToken;
            ClientFactory = serviceProvider.GetRequiredService<ICommonServiceHttpClientFactory>();
        }

        public CancellationToken CancellationToken { get; }
        public ICommonServiceHttpClientFactory ClientFactory { get; }
        public string Culture { get => _culture ?? string.Empty; set => _culture = value; }
        public Platform Platform { get; set; }
        public RunType RunType { get; set; }
        public IServiceProvider ServiceProvider { get; }
        public LoginInformation TargetLoginInformation { get; set; }
        public int MaxDegreeOfParallelism { get; } = 3;
        public int BatchSize { get; } = 100;

        public Task LogMessageAsync(PublishMessage message)
        {
            return _errorLogger(message);
        }

        #region Toggles
        public ProjectInfo ProjectInfo { get; set; }
        #endregion Toggles

        #region Party Support

        public IList<AddressPurposeData> AddressPurposes;
        public IList<CommunicationTypeData> CommunicationTypes;
        public MembershipSettingsData MembershipSettings;
        public ConcurrentDictionary<string, PartyMap> PartyMaps = new ConcurrentDictionary<string, PartyMap>(StringComparer.OrdinalIgnoreCase);
        public IList<CountryData> Countries;

        public async Task<PartySummaryData> GetExistingPartyAsync(string importId)
        {
            if (ProjectInfo.AutoAssignPartyId)
            {
                if (ProjectInfo.AllowUpdates)
                {
                    if (Platform == Platform.V10)
                    {
                        var partyService = ClientFactory.Create<IPartyService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
                        var response = partyService.FindByAlternateId("MajorKey", importId);
                        if (response.IsSuccessStatusCode)
                        {
                            if (response.Result.Count > 0)
                                return response.Result[0];
                        }
                        else
                        {
                            // error
                        }
                    }
                }
            }
            else
            {
                // no auto-create. data source Id = party.Id for insert or update
                var partySummaryService = ClientFactory.Create<IPartySummaryService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
                return (await partySummaryService.FindSingleAsync(CriteriaData.Equal(nameof(PartyData.Id), importId))).Result;
            }
            return null;
        }

        public string GetMappedPartyId(string id)
        {
            return PartyMaps.TryGetValue(id, out var partyMap) ? partyMap.PartyId : null;
        }

        /// <summary>
        /// Given a workbook ID, look for the corresponding PartyId in <see cref="PartyMaps"/>.  If the
        /// PartyId is not found, see if the workbook ID is an alternate ID for an existing contact in
        /// the system.  If so, add to the <see cref="PartyMaps"/>
        /// collection.
        /// </summary>
        ///
        /// <param name="importId"> The ID of a contact as specified in a worksheet. </param>
        ///
        /// <returns>
        /// <code>Null</code> if <paramref name="importId"/> is not found
        /// either in the workbook or as an alternateId of the an existing contact.
        /// Otherwise, the PartyId for the contact is returned.
        /// </returns>
        public async Task<string> GetPartyIdAsync(string importId)
        {
            if (PartyMaps.TryGetValue(importId, out var partyMap))
                return partyMap.PartyId;

            var party = await GetExistingPartyAsync(importId);
            if (party != null)
            {
                PartyMaps.TryAdd(importId, new PartyMap
                {
                    SourceId = importId,
                    PartyId = party.PartyId,
                    IsOrganization = party is IOrganizationData
                });

                return party.PartyId;
            }
            return null;
        }

        public async Task<ImportRow> GetRowAsync(ImportRowReference dataSourceRowReference)
        {
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var query = dbContext.ProjectImportDatas.Where(p => p.ProjectImportDataId == dataSourceRowReference.ProjectImportDataId);
            var result = (await query.ToListAsync(CancellationToken)).FirstOrDefault();
            return result != null ? new ImportRow(dataSourceRowReference, result.GetDataList()) : null;
        }

        internal async Task<bool> InitializeAsync()
        {
            var addressPurposeService = ClientFactory.Create<IAddressPurposeService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
            var response = await addressPurposeService.FindAllAsync();
            if (response.IsSuccessStatusCode)
            {
                AddressPurposes = response.Result;
            }
            else
            {
                await LogMessageAsync(new PublishMessage(PublishMessageType.Error, response.Message));
                return false;
            }

            var membershipSettingsService = ClientFactory.Create<IMembershipSettingsService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
            var response2 = await membershipSettingsService.FindByIdAsync("0");
            if (response2.IsSuccessStatusCode)
            {
                MembershipSettings = response2.Result;
            }
            else
            {
                await LogMessageAsync(new PublishMessage(PublishMessageType.Error, response2.Message));
                return false;
            }

            var communicationTypeService = ClientFactory.Create<ICommunicationTypeService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
            var response3 = await communicationTypeService.FindAllAsync();
            if (response3.IsSuccessStatusCode)
            {
                CommunicationTypes = response3.Result;
            }
            else
            {
                await LogMessageAsync(new PublishMessage(PublishMessageType.Error, response3.Message));
                return false;
            }

            var countryService = ClientFactory.Create<ICountryService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
            var response4 = await countryService.FindAllAsync();
            if (response.IsSuccessStatusCode)
            {
                Countries = response4.Result;
            }
            else
            {
                await LogMessageAsync(new PublishMessage(PublishMessageType.Error, response4.Message));
                return false;
            }
            return true;
        }

        public ConcurrentDictionary<string, string> _organizationCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public async Task<string> GetOrganizationIdByNameAsync(string organizationName)
        {
            if (_organizationCache.TryGetValue(organizationName, out var partyId)) return partyId;
            var service = ClientFactory.Create<IPartyService>(TargetLoginInformation.Uri, TargetLoginInformation.UserCredentials);
            var response = await service.FindAllAsync(CriteriaData.Equal(nameof(OrganizationData.OrganizationName), organizationName));
            if (response.IsSuccessStatusCode)
            {
                var party = response.Result.FirstOrDefault();
                if (party is OrganizationData)
                {
                    _organizationCache.TryAdd(organizationName, party.PartyId);
                    return party.PartyId;
                }
            }
            return null;
        }
        public ConcurrentDictionary<string, GroupData> Groups = new ConcurrentDictionary<string, GroupData>(StringComparer.OrdinalIgnoreCase);
        #endregion Party Support
    }
}