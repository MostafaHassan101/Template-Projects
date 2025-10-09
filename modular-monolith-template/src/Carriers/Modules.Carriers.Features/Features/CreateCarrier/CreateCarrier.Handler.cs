using Microsoft.Extensions.Logging;
using Modules.Carriers.Domain.Entities;
using Modules.Carriers.Features.Features.Shared.Responses;
using Modules.Carriers.Infrastructure.Database;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.Features.CreateCarrier;

internal interface ICreateCarrierHandler : IHandler
{
    Task<Result<CarrierResponse>> HandleAsync(CreateCarrierRequest request, CancellationToken cancellationToken);
}

internal sealed class CreateCarrierHandler(
    CarriersDbContext dbContext,
    ILogger<CreateCarrierHandler> logger)
    : ICreateCarrierHandler
{
    public async Task<Result<CarrierResponse>> HandleAsync(
        CreateCarrierRequest request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating carrier with name {Name}", request.Name);

        var carrier = new Carrier
        {
            Name = request.Name,
            IsActive = true
        };

        dbContext.Carriers.Add(carrier);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created carrier with ID {CarrierId}", carrier.Id);

        return MapToResponse(carrier);
    }

    private static CarrierResponse MapToResponse(Carrier carrier)
    {
        return new CarrierResponse(carrier.Id, carrier.Name, carrier.IsActive);
    }
}