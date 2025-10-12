using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Features.Features.CreateShipment;

namespace Modules.Shipments.Features.Saga;

/// <summary>
/// Data passed through the CreateShipment saga
/// </summary>
public class CreateShipmentSagaData
{
    public CreateShipmentRequest Request { get; set; } = null!;
    public Shipment? CreatedShipment { get; set; }
    public string? ShipmentNumber { get; set; }
}
