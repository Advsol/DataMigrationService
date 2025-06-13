using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Membership.DataContracts;
using FluentValidation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.GiftAid
{
    //Class dependent on GiftAidDataSourcePublisher, which is unused
    public class GiftAidValidator : AbstractValidator<GiftAidImportTemplate>
    {
        private readonly PublishContext _context;

        public GiftAidValidator(PublishContext context)
        {
            _context = context;
            RuleFor(p => p.Id).NotEmpty().MustAsync(BeValidIdAsync);
            RuleFor(p => p.MethodOfDeclaration).IsEnumName(typeof(GiftAidDeclarationMethodOfDeclarationData));
            RuleFor(p => p.DeclarationReceived).NotNull().InclusiveBetween(new DateTime(1990, 1, 1), DateTime.UtcNow.AddDays(1));
        }

        private async Task<bool> BeValidIdAsync(string importId, CancellationToken arg2)
        {
            return (await _context.GetPartyIdAsync(importId)) != null;
        }
    }
}