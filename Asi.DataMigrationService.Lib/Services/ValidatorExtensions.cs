using Asi.DataMigrationService.Core.Extensions;
using Asi.Soa.Core.DataContracts;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Asi.DataMigrationService.Lib.Services
{
    public static class ValidatorExtensions
    {
        public static IServiceResponse ToServiceResponse(this FluentValidation.Results.ValidationResult result)
        {
            var sr = new ServiceResponse();
            foreach (var error in result.Errors)
            {
                var value = error.AttemptedValue?.ToString();
                sr.ValidationResults.AddError(new ValidationResultData(error.ErrorMessage, null, error.PropertyName, value));
            }
            return sr;
        }

        public static async Task<bool> ExistsAsync<T>(this DbSet<T> dbSet, object id) where T : class
        {
            return (await dbSet.FindAsync(id)) != null;
        }

        public static IRuleBuilderOptions<T, string> In<T>(this IRuleBuilder<T, string> ruleBuilder, string list)
        {
            var items = list.Split(',');
            return ruleBuilder.Must((rootObject, s, context) =>
            {
                context.MessageFormatter.AppendArgument("List", list);
                return items.Any(item => item.EqualsOrdinalIgnoreCase(s));
            }).WithMessage("{PropertyName} must be one of {List}");
        }
    }
}
