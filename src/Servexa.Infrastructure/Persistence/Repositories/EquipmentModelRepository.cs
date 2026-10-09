using Microsoft.EntityFrameworkCore;
using Servexa.Application.Assets.Repositories;
using Servexa.Domain.Assets.Entities;

namespace Servexa.Infrastructure.Persistence.Repositories;

public sealed class EquipmentModelRepository(ServexaDbContext dbContext) : IEquipmentModelRepository
{
    public async Task<EquipmentModel?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.EquipmentModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == id, cancellationToken);
    }

    public async Task<EquipmentModel?> GetByCodeAsync(Guid tenantId, string manufacturerName, string modelCode, CancellationToken cancellationToken = default)
    {
        var normMan = manufacturerName.Trim();
        var normCode = modelCode.Trim().ToUpperInvariant();
        return await dbContext.EquipmentModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.ManufacturerName == normMan && m.ModelCode == normCode, cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentModel>> GetEquipmentModelsAsync(Guid tenantId, string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EquipmentModels
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(m => m.ManufacturerName.Contains(trimmed)
                                  || m.ModelCode.Contains(trimmed)
                                  || m.DisplayName.Contains(trimmed)
                                  || m.CategoryCode.Contains(trimmed));
        }

        return await query
            .OrderBy(m => m.DisplayName)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EquipmentModels
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(m => m.ManufacturerName.Contains(trimmed)
                                  || m.ModelCode.Contains(trimmed)
                                  || m.DisplayName.Contains(trimmed)
                                  || m.CategoryCode.Contains(trimmed));
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task AddAsync(EquipmentModel model, CancellationToken cancellationToken = default)
    {
        await dbContext.EquipmentModels.AddAsync(model, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid tenantId, string manufacturerName, string modelCode, CancellationToken cancellationToken = default)
    {
        var normMan = manufacturerName.Trim();
        var normCode = modelCode.Trim().ToUpperInvariant();
        return await dbContext.EquipmentModels
            .AnyAsync(m => m.TenantId == tenantId && m.ManufacturerName == normMan && m.ModelCode == normCode, cancellationToken);
    }
}
