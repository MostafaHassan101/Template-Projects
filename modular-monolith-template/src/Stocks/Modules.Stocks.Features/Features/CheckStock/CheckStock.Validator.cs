using FluentValidation;
using Modules.Stocks.PublicApi.Contracts;

namespace Modules.Stocks.Features.Features.CheckStock;

public class CheckStockRequestValidator : AbstractValidator<CheckStockRequest>
{
    public CheckStockRequestValidator()
    {
        RuleFor(x => x.Products)
            .NotEmpty()
            .WithMessage("Products list cannot be empty");

        RuleForEach(x => x.Products)
            .SetValidator(new ProductStockValidator());
    }
}

public class ProductStockValidator : AbstractValidator<ProductStock>
{
    public ProductStockValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(255)
            .WithMessage("Product name cannot exceed 255 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero");
    }
}
