using Asi.DataMigrationService.Core;
using Asi.DataMigrationService.Core.Extensions;
using Asi.DataMigrationService.Lib.Publisher;
using Asi.Soa.Core.DataContracts;
using FluentValidation;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.ComponentLib.RelatedData
{
    //Class dependent on RelatedDataDataSourcePublisher, which is unused
    public class RelatedDataValidator : AbstractValidator<ImportTemplate>
    {
        private readonly BOEntityDefinitionData _bo;
        private readonly PublishContext _context;

        public RelatedDataValidator(PublishContext context, BOEntityDefinitionData entityDefinition)
        {
            _context = context;
            _bo = entityDefinition;
            RuleFor(p => p).CustomAsync(ValidateData);
        }

        private async Task ValidateData(ImportTemplate import, ValidationContext<ImportTemplate> customContext, CancellationToken cancelationToken)
        {
            if (import.OtherColumns.TryGetValue("Id", out var id) && id != null)
            {
                if (await _context.GetPartyIdAsync(id) == null)
                {
                    customContext.AddFailure("Id", $"Property Id {id} can not be found.");
                }
            }
            else
            {
                customContext.AddFailure("Id", $"Property Id is a required field.");
            }

            foreach (var column in import.OtherColumns)
            {
                if (!_bo.Properties.Contains(column.Key))
                {
                    customContext.AddFailure(column.Key, $"Property {column.Key} is not defined.");
                    continue;
                }
                var property = _bo.Properties[column.Key];
                if (column.Value is null)
                {
                    continue;
                }
                var value = column.Value;
                switch (property.PropertyTypeName.ToUpperInvariant())
                {
                    case ("INTEGER"):
                        if (!int.TryParse(value, out _))
                        {
                            customContext.AddFailure(property.Name, $"{value} is not an integer.");
                        }
                        break;

                    case ("BOOLEAN"):
                        if (!value.EqualsOrdinalIgnoreCase("True")
                            && !value.EqualsOrdinalIgnoreCase("False")
                            && !value.EqualsOrdinalIgnoreCase("1")
                            && !value.EqualsOrdinalIgnoreCase("0")
                            && !string.IsNullOrEmpty(value))
                        {
                            customContext.AddFailure(property.Name, $"{value} must be \"True\" or \"False\".");
                        }
                        break;

                    case ("DATE"):
                        if (!Utility.TryConvert<DateTime>(value, _context.Culture, out _))
                        {
                            customContext.AddFailure(property.Name, $"{value} is a valid date.");
                        }
                        break;

                    case ("DECIMAL"):
                    case ("MONETARY"): // Could be from a predefined table, i.e., LegacyActivity
                        if (!decimal.TryParse(value, out _))
                        {
                            customContext.AddFailure(property.Name, $"{value} is a valid decimal.");
                        }
                        break;

                    default:
                        // Assume it's a string.
                        var stringProperty = property as PropertyTypeStringData;
                        if (stringProperty != null)
                        {
                            var maxLength = stringProperty.MaxLength == -1 ? 65535 : stringProperty.MaxLength; // -1 means varchar(max)
                            if (!stringProperty.MaxLength.Equals(0) && value.Length > maxLength)
                            {
                                customContext.AddFailure(property.Name, $"{value} exceeds the maximum string length of {maxLength}.");
                            }
                        }
                        else
                        {
                            customContext.AddFailure(property.Name, $"{value} unexpected property type.");
                        }
                        break;
                }
            }
        }
    }
}