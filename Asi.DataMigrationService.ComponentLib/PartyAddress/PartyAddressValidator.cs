using Asi.DataMigrationService.Lib.Publisher;
using FluentValidation;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.ComponentLib.PartyAddress
{
    public class PartyAddressValidator : AbstractValidator<PartyAddressImportTemplate>
    {
        private readonly PublishContext _context;
        private readonly string[] _validCommunicationsReasons = new[] { "default", "mail", "bill", "ship" };

        public PartyAddressValidator(PublishContext context)
        {
            _context = context;

            // Address
            RuleSet("AddressOnly", () =>
            {
                RuleFor(p => p.Id).NotEmpty().MustAsync(BeValidId);
            });
            RuleFor(p => p.CommunicationReasons).Must(BeValidCommunicationReasons).WithMessage($"{{PropertyName}} must bo one or more of {string.Join(", ", _validCommunicationsReasons)}");
            When(p => !string.IsNullOrWhiteSpace(p.CityName + p.CountryName + p.CountryCode + p.CountrySubEntityCode + p.CountrySubEntityName + p.PostalCode),
                () =>
                {
                    RuleFor(p => p.CountryCode).Must(BeValidCountryCode);
                    RuleFor(p => p.CountryName).Must(BeValidCountryName);
                }
                );
            RuleFor(p => p.AddressPurpose).Must(BeValidAddressPurpose);
            RuleFor(p => p.AddressPurpose).Must(BeValidAddressPurposeV10).WithMessage("Cannot set communication reason(s) for non-primary Address Purpose {PropertyName}");
            When(address => address.Email != null, () =>
            {
                RuleFor(p => p.Email).EmailAddress();
            });
        }

        private bool BeValidAddressPurpose(PartyAddressImportTemplate template, string preference)
        {
            return string.IsNullOrWhiteSpace(preference)
                || preference.EqualsOrdinalIgnoreCase("default")
                || _context.AddressPurposes.Any(p => p.Name.EqualsOrdinalIgnoreCase(preference));
        }

        private bool BeValidAddressPurposeV10(PartyAddressImportTemplate template, string preference)
        {
            if (string.IsNullOrWhiteSpace(preference) || preference.EqualsOrdinalIgnoreCase("default"))
                return true;

            if (_context.Platform == Platform.V10)
            {
                var purpose = template.AddressPurpose;

                if (!string.IsNullOrWhiteSpace(purpose))
                {
                    return _context.AddressPurposes.Any(p => p.AllowMultiple == false && p.Name.EqualsOrdinalIgnoreCase(purpose));
                }
            }
            return true;
        }

        private bool BeValidCommunicationReasons(string reasons)
        {
            if (reasons is null) return true;
            var r = reasons.Split(',');
            foreach (var item in r)
            {
                if (!_validCommunicationsReasons.Contains(item.Trim())) return false;
            }
            return true;
        }

        private bool BeValidCountryCode(string countryCode)
        {
            return countryCode is null
                || _context.Countries.Any(p => p.CountryCode.EqualsOrdinalIgnoreCase(countryCode));
        }

        private bool BeValidCountryName(string countryName)
        {
            return countryName is null
                || _context.Countries.Any(p => p.CountryName.EqualsOrdinalIgnoreCase(countryName));
        }

        private async Task<bool> BeValidId(string importId, CancellationToken arg2)
        {
            return (await _context.GetPartyIdAsync(importId)) != null;
        }
    }
}