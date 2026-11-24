using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.Interfaces;

public interface ICriterionValueRepository
{
    Task<CriterionValue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CriterionValue>> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<bool> VariantExistsAsync(Guid variantId, CancellationToken cancellationToken = default);
    Task<bool> CriterionExistsAsync(Guid criterionId, CancellationToken cancellationToken = default);
    Task<bool> CriterionIsActiveAsync(Guid criterionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid variantId, Guid criterionId, CancellationToken cancellationToken = default);
    Task AddAsync(CriterionValue value, CancellationToken cancellationToken = default);
    Task UpdateAsync(CriterionValue value, CancellationToken cancellationToken = default);
    Task DeleteAsync(CriterionValue value, CancellationToken cancellationToken = default);
}
