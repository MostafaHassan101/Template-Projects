using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Modules.Carriers.Infrastructure.Database;
using Modules.Common.Domain.Handlers;
using Modules.Common.Domain.Results;

namespace Modules.Carriers.Features.Features.GetActiveCarriers;

public record CarrierListItem(Guid Id, string Name);

internal interface IGetActiveCarriersHandler : IHandler
{
    Task<Result<List<CarrierListItem>>> HandleAsync(CancellationToken cancellationToken);
}

internal sealed class GetActiveCarriersHandler(
    CarriersDbContext dbContext,
    ILogger<GetActiveCarriersHandler> logger)
    : IGetActiveCarriersHandler
{
    public async Task<Result<List<CarrierListItem>>> HandleAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting active carriers");

        var carriers = await dbContext.Carriers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new CarrierListItem(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Retrieved {Count} active carriers", carriers.Count);

        return carriers;
    }
}