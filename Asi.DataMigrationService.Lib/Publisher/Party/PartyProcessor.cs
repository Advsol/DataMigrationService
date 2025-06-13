using Asi.Core.Client;
using Asi.DataMigrationService.Lib.Services;
using Asi.Soa.Membership.DataContracts;
using Asi.Soa.Membership.DataContracts.Organization;
using Asi.Soa.Membership.ServiceContracts;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Lib.Processor.Party
{

    public class PartyProcessor : ProcessorBase
    {
        private readonly ICommonServiceHttpClientFactory _commonServiceHttpClientFactory;

        public override string ProcessorType => "Party";
        public override Type ImportTemplateType => typeof(PartyImportTemplate);
        public override IList<string> DependentProcessorTypes
        {
            get
            {
                return new List<string>(base.DependentProcessorTypes)
                {
                    "MembershipSettings",
                    "PartyPrefix",
                    "PartySuffix"
                };
            }
        }

        public PartyProcessor(ICommonServiceHttpClientFactory commonServiceHttpClientFactory)
        {
            _commonServiceHttpClientFactory = commonServiceHttpClientFactory;
        }


        protected override async Task PublishBatchAsync(ProcessorContext context, IDataSource dataSource, IList<DataSourceRow> batch)
        {
            var output = new List<PartyRow>();
            foreach (var row in batch)
            {
                var instance = Activator.CreateInstance(ImportTemplateType);
                var success = await MapSourceToImportTemplateAsync(context, dataSource, row, instance);
                success = success && await ValidateRowAsync(context, dataSource, row, instance);
                if (success)
                    output.Add(new PartyRow { Row = row, Party = await ConvertRowAsync(context, dataSource, row, instance) });

                var partyService = _commonServiceHttpClientFactory.Create<IPartyService>(context.TargetUri, context.TargetUserCredenntials);
                foreach (var partyRow in output)
                {
                    var response = partyService.Add(partyRow.Party);
                    if (!response.IsSuccessStatusCode)
                        await context.LogErrorAsync(new ProcessorMessage(ProcessMessageType.Error, this, partyRow.Row, response.Message));
                }
            }
        }
        private class PartyRow
        {
            public DataSourceRow Row { get; set; }
            public PartyData Party { get; set; }
        }

        private string GetMappedPartyId(string id)
        {
            return id;
        }

        private Task<PartyData> ConvertRowAsync(ProcessorContext context, IDataSource dataSource, DataSourceRow row, object instance)
        {
            PartyData party = null;

            var importTemplate = (PartyImportTemplate)instance;
            var partyType = GetPartyTypeCode(importTemplate.PartyType);
            if (partyType != null)
            {
                if (partyType == "P")
                {
                    var person = new PersonData
                    {
                        PersonName = new PersonNameData
                        {
                            NamePrefix = importTemplate.NamePrefix,
                            FirstName = importTemplate.FirstName,
                            MiddleName = importTemplate.MiddleName,
                            LastName = importTemplate.LastName,
                            NameSuffix = importTemplate.NameSuffix,
                            InformalName = importTemplate.InformalName,
                            Designation = importTemplate.Designation
                        },
                        BirthDate = importTemplate.BirthData,
                        //FunctionalTitle = importTemplate.
                        Gender = importTemplate.Gender != null ? new GenderData() { Code = importTemplate.Gender } : null,
                        PrimaryOrganization = new PrimaryOrganizationInformationData
                        {
                            OrganizationPartyId = GetMappedPartyId(importTemplate.PrimaryOrganizationId),
                            Title = importTemplate.PrimaryOrganizationTitle
                        },
                        PrimaryClubPartyId = GetMappedPartyId(importTemplate.PrimaryClubId)
                    };
                    party = person;
                }
                else
                {
                    var organization = new OrganizationData
                    {
                        OrganizationName = importTemplate.OrganizationName
                    };

                    if (context.Platform == Platform.v10)
                    {
                        if (partyType.Equals("CLUB", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var org = party as IOrganizationData;
                            org.OrganizationType = new OrganizationTypeData
                            {
                                OrganizationTypeCode = 2,
                                Name = "Club",
                                GroupTypeKey = new Guid("7926FED7-DA4D-4606-BF08-0A507B6263A6") // System default Club GroupType key
                            };
                        }
                        //If the partyType is CHAPTER set the organization to be a Chapter
                        else if (partyType.Equals("CHAPTER", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var org = party as IOrganizationData;
                            org.OrganizationType = new OrganizationTypeData
                            {
                                OrganizationTypeCode = 1,
                                Name = "Chapter",
                                GroupTypeKey = new Guid("FB12E0D1-D9E3-45F4-A4B0-3322691E9191") // System default Chapter GroupType key
                            };
                        }
                    }

                    party = organization;
                }
            }
            return Task.FromResult(party);
        }

        private async Task<bool> ValidateRowAsync(ProcessorContext context, IDataSource dataSource, DataSourceRow row, object instance)
        {
            var validator = new PartyValidator();
            var importTemplate = (PartyImportTemplate)instance;
            var partyType = GetPartyTypeCode(importTemplate.PartyType);
            var ruleSet = "default," + context.Platform.ToString();
            if (partyType != null)
            {
                if (partyType.Equals("P"))
                    ruleSet += ",person";
                else 
                    ruleSet += ",organization";
            }
            var results = await validator.ValidateAsync(importTemplate, ruleSet: ruleSet);
            if (results.IsValid)
            {
                return true;
            }
            foreach (var error in results.Errors)
            {
                await context.LogErrorAsync(new ProcessorMessage(ProcessMessageType.Error, this, row, error.ErrorMessage));
            }
            return false;
        }

        private class PartyValidator : AbstractValidator<PartyImportTemplate>
        {
            public PartyValidator()
            {
                RuleFor(p => GetPartyTypeCode(p.PartyType)).NotNull().WithMessage("{PropertyName} must be P, Party, O, Organization, CHAPTER or CLUB");
                When(p => p.Status != null, () => { RuleFor(p => p.Status).In("a,active"); });
                RuleSet("V10", () =>
                {
                    RuleFor(p => p.MemberType);
                    RuleFor(p => p.Category);
                });
                RuleSet("person", () =>
                {
                    RuleFor(p => p.NamePrefix).MaximumLength(25);
                    RuleFor(p => p.FirstName).NotEmpty().MaximumLength(50);
                    RuleFor(p => p.MiddleName).MaximumLength(50);
                    RuleFor(p => p.LastName).NotEmpty().MaximumLength(50);
                    RuleFor(p => p.NameSuffix).MaximumLength(10);
                    RuleFor(p => p.InformalName).MaximumLength(50);
                    RuleFor(p => p.Designation).MaximumLength(100);

                });
            }

        }

        private static string GetPartyTypeCode(string partyType)
        {
            partyType = partyType?.ToUpperInvariant();
            switch (partyType)
            {
                case "P":
                case "PERSON":
                    partyType = "P";
                    break;
                case "O":
                case "ORGANIZATION":
                    partyType = "O";
                    break;
                case "CLUB":
                    partyType = "CLUB";
                    break;
                case "CHAPTER":
                    partyType = "CHAPTER";
                    break;
                default:
                    return null;
            }
            return partyType;
        }


        /// <summary>   Validates the batch asynchronous. </summary>
        ///
        /// <param name="context">      The context. </param>
        /// <param name="dataSource">   The data source. </param>
        /// <param name="batch">        The batch. </param>
        ///
        /// <returns>   An asynchronous result. </returns>
        protected override async Task ValidateBatchAsync(ProcessorContext context, IDataSource dataSource, IList<DataSourceRow> batch)
        {
            foreach (var row in batch)
            {
                var instance = Activator.CreateInstance<PartyImportTemplate>();
                await MapSourceToImportTemplateAsync(context, dataSource, row, instance);
                await ValidateRowAsync(context, dataSource, row, instance);
            }
        }

    }
}
