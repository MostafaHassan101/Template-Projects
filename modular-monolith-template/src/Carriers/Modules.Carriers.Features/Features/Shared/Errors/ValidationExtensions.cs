using FluentValidation.Results;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.Features.Shared.Errors;

internal static class ValidationExtensions
{
    internal static List<Error> ToDomainErrors(this ValidationResult validationResult)
    {
        return validationResult.Errors.Select(x => CarrierErrors.ValidationError(x.PropertyName, x.ErrorMessage)).ToList();
    }
}
