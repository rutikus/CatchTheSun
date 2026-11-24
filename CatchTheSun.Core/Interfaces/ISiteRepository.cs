using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.Interfaces;

public interface ISiteRepository
{
    Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Site site);
    void Delete(Site site);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
