using Servexa.Domain.Assets.Entities;

namespace Servexa.Application.Assets.Repositories;

public interface IEquipmentModelRepository
{
    Task<EquipmentModel?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
    Task<EquipmentModel?> GetByCodeAsync(Guid tenantId, string manufacturerName, string modelCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EquipmentModel>> GetEquipmentModelsAsync(Guid tenantId, string? search, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default);
    Task AddAsync(EquipmentModel model, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid tenantId, string manufacturerName, string modelCode, CancellationToken cancellationToken = default);
}
