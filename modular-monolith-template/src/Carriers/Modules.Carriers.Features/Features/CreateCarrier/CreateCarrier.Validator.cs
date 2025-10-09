using FluentValidation;

namespace Modules.Carriers.Features.Features.CreateCarrier;

public class CreateCarrierRequestValidator : AbstractValidator<CreateCarrierRequest>
{
    public CreateCarrierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Carrier name cannot be empty")
            .MaximumLength(100)
            .WithMessage("Carrier name cannot exceed 100 characters");
    }
}