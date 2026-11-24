using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using Xunit;

namespace CatchTheSun.Tests;

public class CriterionValueServiceTests
{
    [Fact]
    public async Task ListsValuesForVariantOnly()
    {
        var variantId = Guid.NewGuid();
        var repo = new FakeRepository([variantId], [ActiveCriterion()], [NewValue(variantId), NewValue(Guid.NewGuid())]);
        var result = await new CriterionValueService(repo).GetByVariantIdAsync(variantId);
        Assert.Single(result!);
        Assert.All(result!, x => Assert.Equal(variantId, x.VariantId));
    }

    [Fact]
    public async Task ReturnsEmptyListForVariantWithoutValues()
    {
        var id = Guid.NewGuid();
        Assert.Empty((await new CriterionValueService(new FakeRepository([id])).GetByVariantIdAsync(id))!);
    }

    [Fact]
    public async Task GetsExistingValueAndReturnsNullForMissing()
    {
        var value = NewValue(Guid.NewGuid());
        var service = new CriterionValueService(new FakeRepository([value.VariantId], [ActiveCriterion()], [value]));
        Assert.Equal(value.Id, (await service.GetByIdAsync(value.Id))!.Id);
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreatesValue()
    {
        var variantId = Guid.NewGuid();
        var criterion = ActiveCriterion();
        var repo = new FakeRepository([variantId], [criterion]);
        var created = await new CriterionValueService(repo).CreateAsync(variantId, new CreateCriterionValueDto { CriterionId = criterion.Id, Value = 2.5m });
        Assert.NotNull(created);
        Assert.Equal(variantId, created.VariantId);
        Assert.Equal(criterion.Id, created.CriterionId);
        Assert.Equal(2.5m, created.Value);
    }

    [Fact]
    public async Task RejectsMissingVariantAndCriterion()
    {
        var criterion = ActiveCriterion();
        var service = new CriterionValueService(new FakeRepository([], [criterion]));
        Assert.Null(await service.CreateAsync(Guid.NewGuid(), new CreateCriterionValueDto { CriterionId = criterion.Id, Value = 1m }));
        var variantId = Guid.NewGuid();
        service = new CriterionValueService(new FakeRepository([variantId]));
        Assert.Null(await service.CreateAsync(variantId, new CreateCriterionValueDto { CriterionId = Guid.NewGuid(), Value = 1m }));
    }

    [Fact]
    public async Task RejectsDuplicateVariantCriterionPair()
    {
        var value = NewValue(Guid.NewGuid());
        var service = new CriterionValueService(new FakeRepository([value.VariantId], [ActiveCriterion(value.CriterionId)], [value]));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(value.VariantId, new CreateCriterionValueDto { CriterionId = value.CriterionId, Value = 3m }));
    }

    [Fact]
    public async Task UpdatesValueWithoutChangingItsReferences()
    {
        var value = NewValue(Guid.NewGuid());
        var service = new CriterionValueService(new FakeRepository([value.VariantId], [ActiveCriterion(value.CriterionId)], [value]));
        var result = await service.UpdateAsync(value.Id, new UpdateCriterionValueDto { Value = -12.25m });
        Assert.Equal(-12.25m, result!.Value);
        Assert.Equal(value.VariantId, result.VariantId);
        Assert.Equal(value.CriterionId, result.CriterionId);
    }

    [Fact]
    public async Task MissingUpdateReturnsNullAndDeleteReturnsFalse()
    {
        var service = new CriterionValueService(new FakeRepository());
        Assert.Null(await service.UpdateAsync(Guid.NewGuid(), new UpdateCriterionValueDto { Value = 1m }));
        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteRemovesValue()
    {
        var value = NewValue(Guid.NewGuid());
        var repo = new FakeRepository([value.VariantId], [ActiveCriterion(value.CriterionId)], [value]);
        Assert.True(await new CriterionValueService(repo).DeleteAsync(value.Id));
        Assert.Empty(repo.Values);
    }

    [Fact]
    public async Task InactiveCriterionCannotBeUsedForCreationButExistingValueCanBeEdited()
    {
        var inactive = ActiveCriterion();
        inactive.IsActive = false;
        var variantId = Guid.NewGuid();
        var repo = new FakeRepository([variantId], [inactive]);
        var service = new CriterionValueService(repo);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(variantId, new CreateCriterionValueDto { CriterionId = inactive.Id, Value = 1m }));

        var existing = NewValue(variantId, inactive.Id);
        repo.Values.Add(existing);
        Assert.Equal(5m, (await service.UpdateAsync(existing.Id, new UpdateCriterionValueDto { Value = 5m }))!.Value);
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public async Task AcceptsNumericPrecisionBoundariesForCultures(string cultureName)
    {
        await WithCulture(cultureName, async () =>
        {
            var variantId = Guid.NewGuid();
            var criterion = ActiveCriterion();
            var service = new CriterionValueService(new FakeRepository([variantId], [criterion]));
            Assert.Equal(-99999999999999.999999m, (await service.CreateAsync(variantId, new CreateCriterionValueDto { CriterionId = criterion.Id, Value = -99999999999999.999999m }))!.Value);

            var otherCriterion = ActiveCriterion();
            var existing = NewValue(variantId, otherCriterion.Id);
            var update = new CriterionValueService(new FakeRepository([variantId], [otherCriterion], [existing]));
            Assert.Equal(99999999999999.999999m, (await update.UpdateAsync(existing.Id, new UpdateCriterionValueDto { Value = 99999999999999.999999m }))!.Value);
        });
    }

    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public async Task RejectsPrecisionBeyondSixFractionalDigits(string cultureName)
    {
        await WithCulture(cultureName, async () =>
        {
            var variantId = Guid.NewGuid(); var criterion = ActiveCriterion();
            var service = new CriterionValueService(new FakeRepository([variantId], [criterion]));
            await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(variantId, new CreateCriterionValueDto { CriterionId = criterion.Id, Value = 1.0000001m }));
        });
    }

    private static Criterion ActiveCriterion(Guid? id = null) => new() { Id = id ?? Guid.NewGuid(), Name = "Criterion", IsActive = true };
    private static CriterionValue NewValue(Guid variantId, Guid? criterionId = null) => new() { Id = Guid.NewGuid(), VariantId = variantId, CriterionId = criterionId ?? Guid.NewGuid(), Value = 1m };

    private static async Task WithCulture(string name, Func<Task> action)
    {
        var culture = CultureInfo.CurrentCulture; var uiCulture = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name); CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name); await action(); }
        finally { CultureInfo.CurrentCulture = culture; CultureInfo.CurrentUICulture = uiCulture; }
    }

    private sealed class FakeRepository(IEnumerable<Guid>? variants = null, IEnumerable<Criterion>? criteria = null, IEnumerable<CriterionValue>? values = null) : ICriterionValueRepository
    {
        public List<Guid> VariantIds { get; } = variants?.ToList() ?? [];
        public List<Criterion> Criteria { get; } = criteria?.ToList() ?? [];
        public List<CriterionValue> Values { get; } = values?.ToList() ?? [];
        public Task<CriterionValue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Values.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<CriterionValue>> GetByVariantIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CriterionValue>>(Values.Where(x => x.VariantId == id).ToArray());
        public Task<bool> VariantExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(VariantIds.Contains(id));
        public Task<bool> CriterionExistsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Criteria.Any(x => x.Id == id));
        public Task<bool> CriterionIsActiveAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Criteria.Single(x => x.Id == id).IsActive);
        public Task<bool> ExistsAsync(Guid v, Guid c, CancellationToken cancellationToken = default) => Task.FromResult(Values.Any(x => x.VariantId == v && x.CriterionId == c));
        public Task AddAsync(CriterionValue value, CancellationToken cancellationToken = default) { Values.Add(value); return Task.CompletedTask; }
        public Task UpdateAsync(CriterionValue value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(CriterionValue value, CancellationToken cancellationToken = default) { Values.Remove(value); return Task.CompletedTask; }
    }
}
