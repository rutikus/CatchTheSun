using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using Xunit;

namespace CatchTheSun.Tests;

public class VariantServiceTests
{
    [Fact]
    public async Task GetBySiteIdAsyncReturnsOnlyVariantsForRequestedSite()
    {
        var siteId = Guid.NewGuid();
        var otherSiteId = Guid.NewGuid();
        var repository = new InMemoryVariantRepository(
            [siteId, otherSiteId], [NewVariant(siteId, "A"), NewVariant(otherSiteId, "B")]);
        var service = new VariantService(repository);

        var variants = await service.GetBySiteIdAsync(siteId);

        var variant = Assert.Single(variants!);
        Assert.Equal(siteId, variant.SiteId);
        Assert.Equal("A", variant.Name);
    }

    [Fact]
    public async Task GetBySiteIdAsyncReturnsEmptyListForSiteWithoutVariants()
    {
        var siteId = Guid.NewGuid();
        var service = new VariantService(new InMemoryVariantRepository([siteId]));

        var variants = await service.GetBySiteIdAsync(siteId);

        Assert.NotNull(variants);
        Assert.Empty(variants);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsExistingVariant()
    {
        var variant = NewVariant(Guid.NewGuid(), "Existing");
        var service = new VariantService(new InMemoryVariantRepository([variant.SiteId], [variant]));

        var result = await service.GetByIdAsync(variant.Id);

        Assert.NotNull(result);
        Assert.Equal(variant.Id, result.Id);
        Assert.Equal(variant.SiteId, result.SiteId);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsNullForMissingVariant()
    {
        var service = new VariantService(new InMemoryVariantRepository([Guid.NewGuid()]));

        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsyncCreatesVariantForExistingSite()
    {
        var siteId = Guid.NewGuid();
        var repository = new InMemoryVariantRepository([siteId]);
        var service = new VariantService(repository);

        var created = await service.CreateAsync(siteId, new CreateVariantDto
        {
            Name = "  Option 1 ", ExternalId = "  urban-1  "
        });

        Assert.NotNull(created);
        Assert.Equal(siteId, created.SiteId);
        Assert.Equal("Option 1", created.Name);
        Assert.Equal("urban-1", created.ExternalId);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.Single(repository.Variants);
    }

    [Fact]
    public async Task CreateAsyncReturnsNullForMissingSite()
    {
        var service = new VariantService(new InMemoryVariantRepository());

        var created = await service.CreateAsync(Guid.NewGuid(), new CreateVariantDto { Name = "Variant" });

        Assert.Null(created);
    }

    [Fact]
    public async Task UpdateAsyncUpdatesVariantWithoutChangingSite()
    {
        var siteId = Guid.NewGuid();
        var variant = NewVariant(siteId, "Before");
        var repository = new InMemoryVariantRepository([siteId], [variant]);
        var service = new VariantService(repository);

        var updated = await service.UpdateAsync(variant.Id, new UpdateVariantDto
        {
            Name = "After", ExternalId = "urban-2"
        });

        Assert.NotNull(updated);
        Assert.Equal("After", updated.Name);
        Assert.Equal("urban-2", updated.ExternalId);
        Assert.Equal(siteId, updated.SiteId);
    }

    [Fact]
    public async Task DeleteAsyncDeletesVariantWithoutDependencies()
    {
        var variant = NewVariant(Guid.NewGuid(), "Disposable");
        var repository = new InMemoryVariantRepository([variant.SiteId], [variant]);
        var service = new VariantService(repository);

        Assert.True(await service.DeleteAsync(variant.Id));
        Assert.Empty(repository.Variants);
    }

    [Fact]
    public async Task DeleteAsyncSurfacesDependencyConflict()
    {
        var variant = NewVariant(Guid.NewGuid(), "In use");
        var repository = new InMemoryVariantRepository([variant.SiteId], [variant])
        {
            ThrowOnDelete = true
        };
        var service = new VariantService(repository);

        await Assert.ThrowsAsync<VariantHasDependenciesException>(() => service.DeleteAsync(variant.Id));
        Assert.Single(repository.Variants);
    }

    [Fact]
    public async Task CreateAsyncRejectsEmptyName()
    {
        var service = new VariantService(new InMemoryVariantRepository([Guid.NewGuid()]));

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            Guid.NewGuid(), new CreateVariantDto { Name = "   " }));
    }

    private static Variant NewVariant(Guid siteId, string name) => new()
    {
        Id = Guid.NewGuid(), SiteId = siteId, Name = name, CreatedAt = DateTime.UtcNow
    };

    private sealed class InMemoryVariantRepository : IVariantRepository
    {
        private readonly List<Guid> _siteIds;
        private readonly List<Variant> _variants;

        public InMemoryVariantRepository(IEnumerable<Guid>? siteIds = null, IEnumerable<Variant>? variants = null)
        {
            _siteIds = siteIds?.ToList() ?? [];
            _variants = variants?.ToList() ?? [];
        }

        public IReadOnlyList<Variant> Variants => _variants;
        public bool ThrowOnDelete { get; init; }

        public Task<Variant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_variants.SingleOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<Variant>> GetBySiteIdAsync(Guid siteId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<Variant>>(_variants.Where(x => x.SiteId == siteId).OrderBy(x => x.Name).ToArray());
        }

        public Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_siteIds.Contains(siteId));
        }

        public Task AddAsync(Variant variant, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _variants.Add(variant);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Variant variant, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Variant variant, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ThrowOnDelete)
                throw new VariantHasDependenciesException(variant.Id);
            _variants.Remove(variant);
            return Task.CompletedTask;
        }
    }
}
