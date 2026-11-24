using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.Interfaces;

public interface ICriterionRepository
{
    Task<IReadOnlyList<Criterion>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Criterion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> HasDependenciesAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Criterion criterion, CancellationToken cancellationToken = default);
    Task UpdateAsync(Criterion criterion, CancellationToken cancellationToken = default);
    Task DeleteAsync(Criterion criterion, CancellationToken cancellationToken = default);
}
