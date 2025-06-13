using Asi.DataMigrationService.ComponentLib.PartyAddress;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.DataMigrationService.Lib.Services;
using Asi.Soa.Membership.DataContracts;
using FluentValidation;
using System;
using System.Linq;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.ComponentLib.Party
{
    public class PartyValidator : AbstractValidator<PartyImportTemplate>
    {
        private readonly PublishContext _context;
        private readonly PartyDataSourcePublisher _partyDataSourceProcessor;

        public PartyValidator(PublishContext processorContext, PartyDataSourcePublisher partyDataSourceProcessor)
        {
            _context = processorContext;
            _partyDataSourceProcessor = partyDataSourceProcessor;
            RuleFor(p => GetPartyTypeCode(p.PartyType)).NotNull().WithMessage("{PropertyName} must be P, Party, O, Organization, CHAPTER or CLUB");
            When(p => p.Status != null, () => { RuleFor(p => p.Status).In("a,active"); });

            // Address
            RuleFor(party => party).SetValidator(new PartyAddressValidator(_context));
            RuleSet("V10", () =>
            {
                RuleFor(p => p.MemberType).Must(BeValidV10MemberType);
                RuleFor(p => p.Category).Must(BeValidV10Category);
                RuleFor(p => p.PostalCode).Must(p => p is null || p.Length <= 10 && p.Length >= 3).WithMessage("{PropertyName} must be 3 to 10 characters long.");
            });

            RuleSet("V100", () =>
            {
                RuleFor(p => p.ReceiptPreference).Must(pref => TryGetReceiptPreference(pref, out _))
                .WithMessage("{PropertyName} must be between 0-4 or None, Immediate, Annual, Quarterly or Monthly.");
                RuleFor(p => p.Category);
            });

            RuleSet("person", () =>
            {
                RuleFor(p => p.NamePrefix).MaximumLength(25).Must(BeInPrefixTable);
                RuleFor(p => p.FirstName).NotEmpty().MaximumLength(50);
                RuleFor(p => p.MiddleName).MaximumLength(50);
                RuleFor(p => p.LastName).NotEmpty().MaximumLength(50);
                RuleFor(p => p.NameSuffix).MaximumLength(10).Must(BeInSuffixTable);
                RuleFor(p => p.InformalName).MaximumLength(50);
                RuleFor(p => p.Designation).MaximumLength(100);
            });
        }

        public static string GetPartyTypeCode(string partyType)
        {
            partyType = partyType?.NullTrim()?.ToUpperInvariant();
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

        public static bool TryGetReceiptPreference(string numberOrCode, out ReceiptPreferenceData receiptPreference)
        {
            receiptPreference = default;
            if (numberOrCode is null) return true;
            string code;
            var isNumber = int.TryParse(numberOrCode, out var numberValue);
            if (isNumber)
            {
                switch (numberValue)
                {
                    case 0:
                        code = "None";
                        break;

                    case 1:
                        code = "Immediate";
                        break;

                    case 2:
                        code = "Annual";
                        break;

                    case 3:
                        code = "Quarterly";
                        break;

                    case 4:
                        code = "Monthly";
                        break;

                    default:
                        return false;
                }
            }
            else
            {
                if (numberOrCode.EqualsOrdinalIgnoreCase("None")
                    || numberOrCode.EqualsOrdinalIgnoreCase("Immediate")
                    || numberOrCode.EqualsOrdinalIgnoreCase("Annual")
                    || numberOrCode.EqualsOrdinalIgnoreCase("Quarterly")
                    || numberOrCode.EqualsOrdinalIgnoreCase("Monthly"))
                {
                    code = char.ToUpperInvariant(numberOrCode[0]) + numberOrCode.Substring(1);
                }
                else
                {
                    return false;
                }
            }

            receiptPreference = (ReceiptPreferenceData)Enum.Parse(typeof(ReceiptPreferenceData), code, true);
            return true;
        }

        private bool BeInPrefixTable(string prefix)
        {
            prefix = prefix.NullTrim();
            if (prefix != null && !_partyDataSourceProcessor.Prefixes.ContainsKey(prefix))
                _partyDataSourceProcessor.Prefixes[prefix] = prefix;
            return true;
        }

        private bool BeInSuffixTable(string suffix)
        {
            if (suffix != null && !_partyDataSourceProcessor.Suffixes.ContainsKey(suffix))
                _partyDataSourceProcessor.Prefixes[suffix] = suffix;
            return true;
        }

        private bool BeValidV10Category(string category)
        {
            return category is null
                || _partyDataSourceProcessor.Categories.Any(p => p.Code.EqualsOrdinalIgnoreCase(category));
        }

        private bool BeValidV10MemberType(PartyImportTemplate import, string memberTypeCode)
        {
            if (memberTypeCode is null) return false;
            var memberType = _partyDataSourceProcessor.MemberTypes.FirstOrDefault(p => p.CustomerTypeId.Equals(memberTypeCode, StringComparison.OrdinalIgnoreCase));
            if (memberType is null)
                return false;
            var partyType = GetPartyTypeCode(import.PartyType);

            if (partyType != null)
            {
                if (partyType == "P" && memberType.IsCompanyRecord) return false;
                if (partyType != "P" && !memberType.IsCompanyRecord) return false;
            }
            return true;
        }
    }
}