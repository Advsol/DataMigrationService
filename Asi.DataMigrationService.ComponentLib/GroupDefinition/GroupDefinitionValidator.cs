using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.GroupDefinition
{
    //Class dependent on GroupDefinitionDataSourcePublisher, which is unused
    public class GroupDefinitionValidator : AbstractValidator<GroupDefinitionImportTemplate>
    {
        private readonly PublishContext _context;

        public GroupDefinitionValidator(PublishContext context)
        {
            _context = context;
            RuleFor(p => p.GroupName).NotEmpty();
            RuleFor(p => p.GroupClass).NotEmpty().MustAsync(BeValidGroupClassAsync);
            RuleFor(p => p.GroupOwnerId).NotEmpty().MustAsync(BeValidGroupOwnerAsync);
        }

        private async Task<bool> BeValidGroupClassAsync(string groupClassName, CancellationToken cancellationToken)
        {
            var service = _context.ClientFactory.Create<IGroupClassService>(_context.TargetLoginInformation.Uri, _context.TargetLoginInformation.UserCredentials);
            var response = await service.FindSingleAsync(CriteriaData.Equal("Name", groupClassName));
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> BeValidGroupOwnerAsync(string groupOwnerId, CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(groupOwnerId)) return true;
            var response = await _context.GetPartyIdAsync(groupOwnerId);
            return response != null;
        }
    }
}