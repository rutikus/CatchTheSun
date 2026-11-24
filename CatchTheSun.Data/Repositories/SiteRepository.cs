using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.Data.Repositories;

public sealed class SiteRepository(ApplicationDbContext dbContext) : ISiteRepository
{
    public async Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Sites.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Sites.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Add(Site site) => dbContext.Sites.Add(site);

    public void Delete(Site site) => dbContext.Sites.Remove(site);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
