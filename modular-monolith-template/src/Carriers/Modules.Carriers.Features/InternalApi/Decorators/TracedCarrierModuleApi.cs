using System.Diagnostics;
using Modules.Carriers.Features.Tracing;
using Modules.Carriers.PublicApi;
using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.InternalApi.Decorators;

public class TracedCarrierModuleApi(ICarrierModuleApi inner) : ICarrierModuleApi
{
    public async Task<Result<Success>> CreateShipmentAsync(
        CreateCarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        using var activity = CarriersActivitySource.Instance.StartActivity($"{CarriersActivitySource.Instance.Name}.create-shipment");

        activity?.SetTag("module", CarriersActivitySource.Instance.Name);
        activity?.SetTag("operation", "CreateShipment");
        activity?.SetTag("carrier", request.Carrier);
        activity?.SetTag("shipment.count", request.Items.Count);

        try
        {
            var response = await inner.CreateShipmentAsync(request, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
