using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Membership.DataContracts.Groups;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Asi.DataMigrationService.Core.Extensions;

namespace Asi.DataMigrationService.ComponentLib.PartyAttendance
{
    public class PartyAttendanceValidator : AbstractValidator<PartyAttendanceImportTemplate>
    {
        private readonly IList<AttendanceTypeRefData> _attendanceTypeRefs;
        private readonly PublishContext _context;

        public PartyAttendanceValidator(PublishContext context, IList<AttendanceTypeRefData> attendanceTypeRefs)
        {
            _context = context;
            _attendanceTypeRefs = attendanceTypeRefs;
            RuleFor(p => p.Id).NotEmpty().MustAsync(BeValidId);
            RuleFor(p => p.AttendanceTypeCode).NotEmpty().Must(BeValidTypeCode);
            RuleFor(p => p.AttendanceDate).NotNull();
            RuleFor(p => p.OrganizationName).NotEmpty().MustAsync(BeValidOrganizationAsync);
        }

        private async Task<bool> BeValidId(string importId, CancellationToken arg2)
        {
            return (await _context.GetPartyIdAsync(importId)) != null;
        }

        private async Task<bool> BeValidOrganizationAsync(string organizationName, CancellationToken arg2)
        {
            return (await _context.GetOrganizationIdByNameAsync(organizationName)) != null;
        }

        private bool BeValidTypeCode(string AttendanceTypeCode)
        {
            return _attendanceTypeRefs.Any(p => p.AttendanceTypeCode.EqualsOrdinalIgnoreCase(AttendanceTypeCode));
        }
    }
}