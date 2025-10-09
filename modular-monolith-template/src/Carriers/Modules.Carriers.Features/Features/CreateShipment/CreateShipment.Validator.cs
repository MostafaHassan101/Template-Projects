using FluentValidation;
using Modules.Carriers.PublicApi.Contracts;

namespace Modules.Carriers.Features.Features.CreateShipment;

public class CreateCarrierShipmentRequestValidator : AbstractValidator<CreateCarrierShipmentRequest>
{
    public CreateCarrierShipmentRequestValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID cannot be empty");

        RuleFor(x => x.Carrier)
            .NotEmpty()
            .WithMessage("Carrier name cannot be empty");

        RuleFor(x => x.ReceiverEmail)
            .NotEmpty()
            .WithMessage("Receiver email cannot be empty")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.Address)
            .NotNull()
            .WithMessage("Address cannot be null")
            .SetValidator(new AddressValidator());

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Items list cannot be empty");

        RuleForEach(x => x.Items)
            .SetValidator(new CarrierShipmentItemValidator());
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street cannot be empty");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City cannot be empty");

        RuleFor(x => x.Zip)
            .NotEmpty()
            .WithMessage("ZIP code cannot be empty");
    }
}

public class CarrierShipmentItemValidator : AbstractValidator<CarrierShipmentItem>
{
    public CarrierShipmentItemValidator()
    {
        RuleFor(x => x.Product)
            .NotEmpty()
            .WithMessage("Product name cannot be empty");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");
    }
}
