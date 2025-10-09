using Modules.Shipments.Domain.Enums;

namespace Modules.Shipments.Tests.Integration.Contracts.Responses;

public sealed record ShipmentResponse(
    string Number,
    string OrderId,
    AddressResponse Address,
    string Carrier,
    string ReceiverEmail,
    ShipmentStatus Status,
    List<ShipmentItemResponse> Items);
