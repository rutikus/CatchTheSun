using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CatchTheSun.Data.Repositories;

public sealed class CriterionRepository(ApplicationDbContext dbContext) : ICriterionRepository
{
    public async Task<IReadOnlyList<Criterion>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Criteria.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Criterion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Criteria.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
        dbContext.Criteria.AnyAsync(x => x.Name == name && (excludingId == null || x.Id != excludingId), cancellationToken);

    public async Task<bool> HasDependenciesAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.CriterionValues.AnyAsync(x => x.CriterionId == id, cancellationToken) ||
        await dbContext.CalculationCriteria.AnyAsync(x => x.CriterionId == id, cancellationToken);

    public async Task AddAsync(Criterion criterion, CancellationToken cancellationToken = default)
    {
        await dbContext.Criteria.AddAsync(criterion, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Criterion criterion, CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Criterion criterion, CancellationToken cancellationToken = default)
    {
        dbContext.Criteria.Remove(criterion);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation
        })
        {
            throw new CriterionHasDependenciesException(criterion.Id, exception);
        }
    }
}
