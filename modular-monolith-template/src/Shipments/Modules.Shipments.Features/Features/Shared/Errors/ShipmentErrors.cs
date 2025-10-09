using Modules.Common.Domain.Results;

namespace Modules.Shipments.Features.Features.Shared.Errors;

internal static class ShipmentErrors
{
    private const string ErrorPrefix = "Shipments";

    internal static Error NotFound(string shipmentNumber) =>
        Error.NotFound($"{ErrorPrefix}.{nameof(NotFound)}", $"Shipment with number '{shipmentNumber}' not found");

    internal static Error AlreadyExists(string orderId) =>
        Error.Conflict($"{ErrorPrefix}.{nameof(AlreadyExists)}", $"Shipment for order '{orderId}' already exists");
}
