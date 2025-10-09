using FluentValidation.Results;
using Modules.Common.Domain.Results;

namespace Modules.Stocks.Features.Features.Shared.Errors;

internal static class ValidationExtensions
{
    internal static List<Error> ToDomainErrors(this ValidationResult validationResult)
    {
        return validationResult.Errors.Select(x => StockErrors.ValidationError(x.PropertyName, x.ErrorMessage)).ToList();
    }
}
