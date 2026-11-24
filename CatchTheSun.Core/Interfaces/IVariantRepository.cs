using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.Interfaces;

public interface IVariantRepository
{
    Task<Variant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Variant>> GetBySiteIdAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task AddAsync(Variant variant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Variant variant, CancellationToken cancellationToken = default);
    Task DeleteAsync(Variant variant, CancellationToken cancellationToken = default);
}
