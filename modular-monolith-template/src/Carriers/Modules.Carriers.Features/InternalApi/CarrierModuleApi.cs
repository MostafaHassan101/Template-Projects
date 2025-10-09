using Modules.Carriers.Features.Features.CreateShipment;
using Modules.Carriers.PublicApi;
using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.InternalApi;

internal sealed class CarrierModuleApi(
    ICreateCarrierShipmentHandler createShipmentHandler) : ICarrierModuleApi
{
    public async Task<Result<Success>> CreateShipmentAsync(
        CreateCarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        return await createShipmentHandler.HandleAsync(request, cancellationToken);
    }
}
