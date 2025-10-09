using FluentValidation;

namespace Modules.Stocks.Features.Features.GetStocksByProductName;

public class GetStocksByProductNameRequestValidator : AbstractValidator<GetStocksByProductNameRequest>
{
    public GetStocksByProductNameRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MaximumLength(255)
            .WithMessage("Product name cannot exceed 255 characters");
    }
}
