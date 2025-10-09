using Modules.Carriers.Domain.Entities;
using Modules.Carriers.PublicApi.Contracts;
using Address = Modules.Carriers.Domain.ValueObjects.Address;

namespace Modules.Carriers.Features.Features.CreateShipment;

internal static class CreateCarrierShipmentMappingExtensions
{
    public static CarrierShipment MapToCarrierShipment(this CreateCarrierShipmentRequest request, Carrier carrier)
    {
        return new CarrierShipment
        {
            Id = Guid.NewGuid(),
            CarrierId = carrier.Id,
            OrderId = request.OrderId,
            CreatedAt = DateTime.UtcNow,
            ShippingAddress = new Address
            {
                Street = request.Address.Street,
                City = request.Address.City,
                Zip = request.Address.Zip
            }
        };
    }
}
