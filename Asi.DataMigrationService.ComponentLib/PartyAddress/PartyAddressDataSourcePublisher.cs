using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Publisher.DataSource;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Client;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.ComponentLib.PartyAddress
{
    public class PartyAddressDataSourcePublisher : DataSourcePublisherBase
    {
        public PartyAddressDataSourcePublisher(ICommonServiceHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public override string DataSourceTypeName => "PartyAddress";

        public override IList<string> DependentPublisherTypeNames => new List<string> { "Party" };

        public override string Title => "Party Address Import File";
        public override bool IsHarvester => false;

        public override bool IsValidatable => true;

        public override Type UIComponentType => typeof(StandardImportDataSourceComponent);

        public static void UpdateAddress(PublishContext context, PartyAddressImportTemplate import, PartyData party)
        {
            if (!string.IsNullOrWhiteSpace(import.AddressLine1 + import.AddressLine2 + import.AddressLine3 + import.CityName + import.CountryName))
            {
                if (party.Addresses is null)
                    party.Addresses = new FullAddressDataCollection();
                if (import.AddressPurpose != null)
                {
                    var purpose = context.AddressPurposes.FirstOrDefault(p => p.AddressPurposeId.Equals(import.AddressPurpose, StringComparison.InvariantCultureIgnoreCase));
                    if (purpose != null && !purpose.AllowMultiple.GetValueOrDefault())
                    {
                        var match = party.Addresses.FirstOrDefault(p => import.AddressPurpose.Equals(p.AddressPurpose, StringComparison.InvariantCultureIgnoreCase));
                        if (match != null)
                        {
                            UpdateFullAddress(context, import, match);
                        }
                        else
                        {
                            var fullAddress = new FullAddressData();
                            UpdateFullAddress(context, import, fullAddress);
                            party.Addresses.Add(fullAddress);
                        }
                    }
                    else
                    {
                        var fullAddress = new FullAddressData();
                        UpdateFullAddress(context, import, fullAddress);
                        party.Addresses.Add(fullAddress);
                    }
                }
                else
                {
                    var fullAddress = new FullAddressData();
                    UpdateFullAddress(context, import, fullAddress);
                    party.Addresses.Add(fullAddress);
                }
            }
            ProcessCommunicationTypePreferences(context, import, party);
        }

        public override ImportTemplate CreateImportTemplateInstance() => new PartyAddressImportTemplate();


        protected override async Task PublishBatchAsync(PublishContext context, DataSourceInfo dataSourceInfo, IList<ImportRow> batch, GroupSuccess groupSuccess)
        {
            foreach (var row in batch)
            {
                var instance = Activator.CreateInstance<PartyAddressImportTemplate>();
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
            var validator = new PartyAddressValidator(context);
            await ValidateBatch(context, batch, groupSuccess, validator,new[] { "default", "AddressOnly" });
        }

        private static bool ProcessCommunicationTypePreferences(PublishContext context, ImportTemplate import, PartyData party)
        {
            var cpTypes = context.CommunicationTypes.Where(p => !p.CannotOptOut);
            foreach (var item in cpTypes)
            {
                var propertyName = $"CP:{item.ReasonCode}";
                if (import.OtherColumns.TryGetValue(propertyName, out var value))
                {
                    if (party.CommunicationTypePreferences is null)
                        party.CommunicationTypePreferences = new PartyCommunicationTypePreferenceDataCollection();

                    if (bool.TryParse(value, out var boolValue))
                    {
                        var communicationTypePreference = new PartyCommunicationTypePreferenceData
                        {
                            CommunicationTypeId = item.CommunicationTypeId,
                            OptInFlag = boolValue
                        };
                        var old = party.CommunicationTypePreferences.FirstOrDefault(p => p.CommunicationTypeId == communicationTypePreference.CommunicationTypeId);
                        if (old != null)
                            party.CommunicationTypePreferences.Remove(old);
                        party.CommunicationTypePreferences.Add(communicationTypePreference);
                    }
                }
            }
            return true;
        }

        private static void UpdateFullAddress(PublishContext context, PartyAddressImportTemplate import, FullAddressData fullAddress)
        {
            fullAddress.AddressPurpose = import.AddressPurpose;
            if (fullAddress.Address is null) fullAddress.Address = new AddressData();

            var address = fullAddress.Address;
            if (import.AddressLine1 != null)
            {
                address.AddressLines = new AddressLineDataCollection();
                if (import.AddressLine1 != null) address.AddressLines.Add(import.AddressLine1);
                if (import.AddressLine2 != null) address.AddressLines.Add(import.AddressLine2);
                if (import.AddressLine3 != null) address.AddressLines.Add(import.AddressLine3);
            }

            address.CityName = import.CityName;
            address.CountrySubEntityCode = import.CountrySubEntityCode;
            address.CountrySubEntityName = import.CountrySubEntityName;
            address.PostalCode = import.PostalCode;

            if (import.Email != null) fullAddress.Email = import.Email;
            if (import.Phone != null) fullAddress.Phone = import.Phone;
            if (import.Fax != null) fullAddress.Fax = import.Fax;

            var reasons = import.CommunicationReasons;
            if (reasons != null)
            {
                var cps = fullAddress.CommunicationPreferences = new CommunicationPreferenceDataCollection();
                foreach (var reason in reasons.Split(",", StringSplitOptions.RemoveEmptyEntries))
                {
                    cps.Add(new CommunicationPreferenceData { Reason = reason.Trim() });
                }
            }

            if (!string.IsNullOrWhiteSpace(import.CityName + import.CountryName + import.CountryCode + import.CountrySubEntityCode + import.CountrySubEntityName + import.PostalCode))
            {
                // if address components are specified, check and assign county and country sub entity codes and names.
                // SOA will kick back an error if the country name or code is not in the database, so check each component
                // (code, then name) and indicate errors as appropriate.
                CountryData country;

                if (import.CountryCode != null)
                {
                    // country code provided
                    country = context.Countries.FirstOrDefault(p => p.CountryCode.EqualsOrdinalIgnoreCase(import.CountryCode));
                }
                else if (import.CountryName != null)
                {
                    // country name provided
                    country = context.Countries.FirstOrDefault(p => p.CountryName.EqualsOrdinalIgnoreCase(import.CountryName));
                }
                else
                {
                    // default country
                    country = context.MembershipSettings.DefaultCountry;
                }
                if (country != null)
                {
                    address.CountryCode = country.CountryCode;
                    address.CountryName = country.CountryName;
                    var cse =
                        country.CountrySubEntities?.FirstOrDefault(
                            p => (p.Code != null && p.Code.EqualsOrdinalIgnoreCase(import.CountrySubEntityCode))
                                 || (p.Name != null && p.Name.EqualsOrdinalIgnoreCase(import.CountrySubEntityName)));
                    if (cse != null)
                    {
                        address.CountrySubEntityCode = cse.Code;
                        address.CountrySubEntityName = cse.Name;
                    }
                }
            }
        }

        private async Task<bool> PublishRowAsync(PublishContext context, ImportRow row, object instance)
        {
            var importTemplate = (PartyAddressImportTemplate)instance;
            var sourceId = importTemplate.Id;
            if (string.IsNullOrEmpty(sourceId))
            {
                await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, row, "Id is required."));
                return false;
            }
            var partyId = await context.GetPartyIdAsync(sourceId);
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
            UpdateAddress(context, importTemplate, party);
            response = await service.UpdateAsync(party);
            if (response.IsSuccessStatusCode) return true;
            await context.LogMessageAsync(new PublishMessage(PublishMessageType.Error, response.Message));
            return false;
        }
    }
}