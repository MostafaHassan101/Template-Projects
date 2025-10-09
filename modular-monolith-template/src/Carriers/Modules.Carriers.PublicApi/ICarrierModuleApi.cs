using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.PublicApi;

public interface ICarrierModuleApi
{
    Task<Result<Success>> CreateShipmentAsync(CreateCarrierShipmentRequest request, CancellationToken cancellationToken);
}
