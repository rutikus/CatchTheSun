using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.Data.Repositories;

public sealed class CriterionValueRepository(ApplicationDbContext db) : ICriterionValueRepository
{
    public Task<CriterionValue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => db.CriterionValues.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<CriterionValue>> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default) => await db.CriterionValues.AsNoTracking().Where(x => x.VariantId == variantId).OrderBy(x => x.CriterionId).ToListAsync(cancellationToken);
    public Task<bool> VariantExistsAsync(Guid id, CancellationToken cancellationToken = default) => db.Variants.AnyAsync(x => x.Id == id, cancellationToken);
    public Task<bool> CriterionExistsAsync(Guid id, CancellationToken cancellationToken = default) => db.Criteria.AnyAsync(x => x.Id == id, cancellationToken);
    public Task<bool> CriterionIsActiveAsync(Guid id, CancellationToken cancellationToken = default) => db.Criteria.Where(x => x.Id == id).Select(x => x.IsActive).SingleOrDefaultAsync(cancellationToken);
    public Task<bool> ExistsAsync(Guid variantId, Guid criterionId, CancellationToken cancellationToken = default) => db.CriterionValues.AnyAsync(x => x.VariantId == variantId && x.CriterionId == criterionId, cancellationToken);
    public async Task AddAsync(CriterionValue value, CancellationToken cancellationToken = default) { await db.CriterionValues.AddAsync(value, cancellationToken); await db.SaveChangesAsync(cancellationToken); }
    public async Task UpdateAsync(CriterionValue value, CancellationToken cancellationToken = default) => await db.SaveChangesAsync(cancellationToken);
    public async Task DeleteAsync(CriterionValue value, CancellationToken cancellationToken = default) { db.CriterionValues.Remove(value); await db.SaveChangesAsync(cancellationToken); }
}
