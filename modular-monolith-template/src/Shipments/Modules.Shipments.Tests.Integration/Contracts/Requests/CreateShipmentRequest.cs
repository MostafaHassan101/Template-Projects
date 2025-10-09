using Modules.Shipments.Tests.Integration.Contracts.Responses;

namespace Modules.Shipments.Tests.Integration.Contracts.Requests;

public sealed record CreateShipmentRequest(
    string OrderId,
    AddressResponse Address,
    string Carrier,
    string ReceiverEmail,
    List<ShipmentItemRequest> Items);
