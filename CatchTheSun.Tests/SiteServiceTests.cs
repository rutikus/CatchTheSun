using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using Xunit;

namespace CatchTheSun.Tests;

public class SiteServiceTests
{
    [Fact]
    public async Task CreateAsyncCreatesAndReturnsSite()
    {
        var repository = new InMemorySiteRepository();
        var service = new SiteService(repository);

        var created = await service.CreateAsync(new CreateSiteDto
        {
            Name = "  River District  ", Location = "  East Side  ", Area = 125.5m
        });

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("River District", created.Name);
        Assert.Equal("East Side", created.Location);
        Assert.Equal(125.5m, created.Area);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.Single(repository.Sites);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsExistingSite()
    {
        var site = NewSite();
        var service = new SiteService(new InMemorySiteRepository(site));

        var result = await service.GetByIdAsync(site.Id);

        Assert.NotNull(result);
        Assert.Equal(site.Id, result.Id);
        Assert.Equal(site.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullForMissingSite()
    {
        var service = new SiteService(new InMemorySiteRepository());

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsyncUpdatesExistingSite()
    {
        var site = NewSite();
        var repository = new InMemorySiteRepository(site);
        var service = new SiteService(repository);

        var result = await service.UpdateAsync(site.Id, new UpdateSiteDto
        {
            Name = "Updated site", Location = "New location", Area = 92m
        });

        Assert.NotNull(result);
        Assert.Equal("Updated site", result.Name);
        Assert.Equal("New location", result.Location);
        Assert.Equal(92m, result.Area);
        Assert.Equal(site.CreatedAt, result.CreatedAt);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task DeleteAsyncRemovesExistingSite()
    {
        var site = NewSite();
        var repository = new InMemorySiteRepository(site);
        var service = new SiteService(repository);

        var deleted = await service.DeleteAsync(site.Id);

        Assert.True(deleted);
        Assert.Empty(repository.Sites);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task CreateAsyncRejectsNonPositiveArea()
    {
        var repository = new InMemorySiteRepository();
        var service = new SiteService(repository);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateSiteDto
        {
            Name = "Valid name", Location = "Valid location", Area = 0m
        }));

        Assert.Empty(repository.Sites);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task CreateAsyncValidatesAreaUnderRussianCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            var service = new SiteService(new InMemorySiteRepository());

            var created = await service.CreateAsync(new CreateSiteDto
            {
                Name = "Valid name", Location = "Valid location", Area = 12.5m
            });

            Assert.Equal(12.5m, created.Area);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public async Task CreateAsyncRequiresNameAndLocation()
    {
        var repository = new InMemorySiteRepository();
        var service = new SiteService(repository);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(new CreateSiteDto
        {
            Name = "   ", Location = "", Area = 10m
        }));

        Assert.Empty(repository.Sites);
        Assert.Equal(0, repository.SaveCount);
    }

    private static Site NewSite() => new()
    {
        Id = Guid.NewGuid(), Name = "Existing site", Location = "Existing location",
        Area = 50m, CreatedAt = DateTime.UtcNow
    };

    private sealed class InMemorySiteRepository(params Site[] initialSites) : ISiteRepository
    {
        private readonly List<Site> _sites = [.. initialSites];

        public IReadOnlyList<Site> Sites => _sites;
        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Site>>(_sites.OrderBy(x => x.Name).ToArray());
        }

        public Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_sites.SingleOrDefault(x => x.Id == id));
        }

        public void Add(Site site) => _sites.Add(site);

        public void Delete(Site site) => _sites.Remove(site);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SaveCount++;
            return Task.CompletedTask;
        }
    }
}
