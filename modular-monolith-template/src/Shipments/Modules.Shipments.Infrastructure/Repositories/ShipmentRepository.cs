using Microsoft.EntityFrameworkCore;
using Modules.Common.Infrastructure.Repositories;
using Modules.Shipments.Domain.Entities;
using Modules.Shipments.Domain.Enums;
using Modules.Shipments.Infrastructure.Database;

namespace Modules.Shipments.Infrastructure.Repositories;

/// <summary>
/// Repository for Shipment entity with custom query methods.
/// Inherits common CRUD operations from base Repository.
/// </summary>
public class ShipmentRepository : Repository<Shipment, ShipmentsDbContext>
{
    public ShipmentRepository(ShipmentsDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets a shipment by order ID.
    /// </summary>
    public async Task<Shipment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
    }

    /// <summary>
    /// Gets all shipments for a specific carrier.
    /// </summary>
    public async Task<IReadOnlyList<Shipment>> GetByCarrierAsync(
        string carrier, 
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Items)
            .Where(s => s.Carrier == carrier)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets shipments by status.
    /// </summary>
    public async Task<IReadOnlyList<Shipment>> GetByStatusAsync(
        ShipmentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Items)
            .Where(s => s.Status == status)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a shipment exists for the given order ID.
    /// </summary>
    public async Task<bool> ExistsByOrderIdAsync(string orderId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(s => s.OrderId == orderId, cancellationToken);
    }

    /// <summary>
    /// Gets shipments created within a date range.
    /// </summary>
    public async Task<IReadOnlyList<Shipment>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Items)
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
