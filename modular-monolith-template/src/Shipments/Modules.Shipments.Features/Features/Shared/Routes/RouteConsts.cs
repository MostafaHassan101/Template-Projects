namespace Modules.Shipments.Features.Features.Shared.Routes;

internal static class RouteConsts
{
    internal const string BaseRoute = "/api/shipments";
    
    internal const string GetByNumber = $"{BaseRoute}/{{shipmentNumber}}";
    
    internal const string CancelShipment = $"{BaseRoute}/cancel/{{shipmentNumber}}";

    internal const string DeliverShipment = $"{BaseRoute}/deliver/{{shipmentNumber}}";

    internal const string DispatchShipment = $"{BaseRoute}/dispatch/{{shipmentNumber}}";

    internal const string ProcessShipment = $"{BaseRoute}/process/{{shipmentNumber}}";

    internal const string ReceiveShipment = $"{BaseRoute}/receive/{{shipmentNumber}}";

    internal const string TransitShipment = $"{BaseRoute}/transit/{{shipmentNumber}}";
}