using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Carriers.Features.Features.Shared.Errors;
using Modules.Carriers.Infrastructure.Database;
using Modules.Carriers.PublicApi.Contracts;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.Features.CreateShipment;

internal interface ICreateCarrierShipmentHandler : IHandler
{
    Task<Result<Success>> HandleAsync(CreateCarrierShipmentRequest request, CancellationToken cancellationToken);
}

internal sealed class CreateCarrierShipmentHandler(
    CarriersDbContext dbContext,
    IValidator<CreateCarrierShipmentRequest> validator,
    ILogger<CreateCarrierShipmentHandler> logger)
    : ICreateCarrierShipmentHandler
{
    public async Task<Result<Success>> HandleAsync(
        CreateCarrierShipmentRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating shipment for order {OrderId}", request.OrderId);

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToDomainErrors();
        }

        var carrier = await dbContext.Carriers.FirstOrDefaultAsync(x => x.Name == request.Carrier && x.IsActive, cancellationToken);
        if (carrier is null)
        {
            logger.LogWarning("Active carrier with Name {Carrier} not found", request.Carrier);

            return CarrierErrors.NotFound(request.Carrier);
        }

        var shipment = request.MapToCarrierShipment(carrier);

        dbContext.CarrierShipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created shipment {ShipmentId} for order {OrderId}", shipment.Id, request.OrderId);

        return Result.Success;
    }
}
