using Modules.Common.Domain.Results;

namespace Modules.Stocks.Features.Features.Shared.Errors;

internal static class StockErrors
{
    private const string ErrorPrefix = "Stocks";

    internal static Error ProductNotFound(string productName) =>
        Error.Failure($"{ErrorPrefix}.{nameof(ProductNotFound)}", $"Product {productName} not found in stock");

    internal static Error ProductAlreadyExists(string productName) =>
        Error.Conflict($"{ErrorPrefix}.{nameof(ProductAlreadyExists)}", $"Product {productName} already exists in stock");

    internal static Error InsufficientStocks(string productName, int requiredQuantity, int availableQuantity) =>
        Error.Failure($"{ErrorPrefix}.{nameof(InsufficientStocks)}",
            $"Insufficient stock for product {productName}. Required: {requiredQuantity}, Available: {availableQuantity}"
        );

    internal static Error ValidationError(string propertyName, string errorMessage) =>
        Error.Validation($"{ErrorPrefix}.{nameof(ValidationError)}", $"{propertyName}: {errorMessage}");
}
