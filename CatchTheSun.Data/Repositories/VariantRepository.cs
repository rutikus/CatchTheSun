using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CatchTheSun.Data.Repositories;

public sealed class VariantRepository(ApplicationDbContext dbContext) : IVariantRepository
{
    public Task<Variant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Variants.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Variant>> GetBySiteIdAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        await dbContext.Variants.AsNoTracking()
            .Where(x => x.SiteId == siteId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        dbContext.Sites.AnyAsync(x => x.Id == siteId, cancellationToken);

    public async Task AddAsync(Variant variant, CancellationToken cancellationToken = default)
    {
        await dbContext.Variants.AddAsync(variant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Variant variant, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Variant variant, CancellationToken cancellationToken = default)
    {
        dbContext.Variants.Remove(variant);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation
        })
        {
            throw new VariantHasDependenciesException(variant.Id, exception);
        }
    }
}
