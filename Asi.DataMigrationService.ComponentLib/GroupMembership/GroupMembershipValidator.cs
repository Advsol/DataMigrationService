using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;
using Asi.Soa.Membership.ServiceContracts;
using FluentValidation;
using System.Threading;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.GroupMembership
{
    public class GroupMembershipValidator : AbstractValidator<GroupMembershipImportTemplate>
    {
        private readonly PublishContext _context;

        public GroupMembershipValidator(PublishContext context)
        {
            _context = context;
            RuleFor(p => p.Id).NotEmpty().MustAsync(BeValidId);
            RuleFor(p => p.GroupName).NotEmpty().MustAsync(BeValidGroupNameAsync);
        }

        private async Task<bool> BeValidGroupNameAsync(string groupName, CancellationToken arg2)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return false;
            if (_context.Groups.TryGetValue(groupName, out var group))
                return group != null;
            var service = _context.ClientFactory.Create<IGroupService>(_context.TargetLoginInformation.Uri, _context.TargetLoginInformation.UserCredentials);
            var response = await service.FindSingleAsync(CriteriaData.Equal("Name", groupName));
            if (!response.IsSuccessStatusCode)
                return false;
            _context.Groups.TryAdd(groupName, response.Result);
            return true;
        }

        private async Task<bool> BeValidId(string importId, CancellationToken arg2)
        {
            return (await _context.GetPartyIdAsync(importId)) != null;
        }
    }
}