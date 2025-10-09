using Modules.Common.Domain.Events;
using Modules.Shipments.Domain.Entities;

namespace Modules.Shipments.Features.Features.CreateShipment.Events;

/// <summary>
/// Event that is raised when a shipment is created
/// </summary>
public sealed record ShipmentCreatedEvent(Shipment Shipment) : IEvent;