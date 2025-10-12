using Modules.Common.Domain.Saga;
using Modules.Shipments.Features.Saga.Steps;

namespace Modules.Shipments.Features.Saga;

/// <summary>
/// Saga for creating a shipment with distributed transaction support
/// </summary>
public class CreateShipmentSaga : ISaga<CreateShipmentSagaData>
{
    public string SagaType => "CreateShipment";

    public List<ISagaStep<CreateShipmentSagaData>> Steps { get; } =
    [
        new ValidateStockStep(),
        new CreateShipmentStep(),
        new DecreaseStockStep(),
        new CreateCarrierShipmentStep()
    ];
}
