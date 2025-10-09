using FluentValidation;

namespace Modules.Stocks.Features.Features.IncreaseStock;

public class IncreaseStockRequestValidator : AbstractValidator<IncreaseStockRequest>
{
    public IncreaseStockRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(255)
            .WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");
    }
}
