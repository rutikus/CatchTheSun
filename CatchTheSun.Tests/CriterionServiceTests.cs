using System.ComponentModel.DataAnnotations;
using System.Globalization;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using Xunit;

namespace CatchTheSun.Tests;

public class CriterionServiceTests
{
    [Fact] public async Task ListsAndReturnsEmptyList() { var r = new Repo(); var s = new CriterionService(r); Assert.Empty(await s.GetAllAsync()); r.Items.Add(New("x")); Assert.Single(await s.GetAllAsync()); }
    [Fact] public async Task GetsByIdOrNull() { var c = New("x"); var s = new CriterionService(new Repo(c)); Assert.Equal(c.Id, (await s.GetByIdAsync(c.Id))!.Id); Assert.Null(await s.GetByIdAsync(Guid.NewGuid())); }
    [Theory]
    [InlineData(CriterionDirection.Maximize)]
    [InlineData(CriterionDirection.Minimize)]
    public async Task CreatesNonTargetDirections(CriterionDirection d) { var result = await new CriterionService(new Repo()).CreateAsync(new CreateCriterionDto { Name = " x ", Direction = d, IsActive = true }); Assert.Equal("x", result.Name); Assert.Null(result.TargetValue); }
    [Fact] public async Task CreatesTargetWithValue() { var result = await new CriterionService(new Repo()).CreateAsync(new CreateCriterionDto { Name = "target", Direction = CriterionDirection.Target, TargetValue = 1.25m }); Assert.Equal(1.25m, result.TargetValue); }
    [Fact] public async Task RejectsMissingTargetValue() => await Assert.ThrowsAsync<ValidationException>(() => new CriterionService(new Repo()).CreateAsync(new CreateCriterionDto { Name = "target", Direction = CriterionDirection.Target }));
    [Fact] public async Task RejectsUndefinedDirection() => await Assert.ThrowsAsync<ValidationException>(() => new CriterionService(new Repo()).CreateAsync(new CreateCriterionDto { Name = "bad", Direction = (CriterionDirection)99 }));
    [Fact] public async Task UpdatesCriterion() { var c = New("before"); var result = await new CriterionService(new Repo(c)).UpdateAsync(c.Id, new UpdateCriterionDto { Name = "after", Direction = CriterionDirection.Minimize }); Assert.Equal("after", result!.Name); }
    [Fact] public async Task DeletesWithoutDependencies() { var c = New("delete"); Assert.True(await new CriterionService(new Repo(c)).DeleteAsync(c.Id)); }
    [Fact] public async Task RejectsDeleteWithDependencies() { var c = New("used"); var r = new Repo(c) { HasDependencies = true }; await Assert.ThrowsAsync<CriterionHasDependenciesException>(() => new CriterionService(r).DeleteAsync(c.Id)); }
    [Fact] public async Task RejectsDuplicateName() { var c = New("same"); await Assert.ThrowsAsync<DuplicateCriterionNameException>(() => new CriterionService(new Repo(c)).CreateAsync(new CreateCriterionDto { Name = " same ", Direction = CriterionDirection.Maximize })); }
    [Fact] public async Task RequiresName() => await Assert.ThrowsAsync<ValidationException>(() => new CriterionService(new Repo()).CreateAsync(new CreateCriterionDto { Name = "   " }));
    [Theory]
    [InlineData("ru-RU")]
    [InlineData("en-US")]
    public async Task DecimalValidationIsCultureIndependent(string cultureName)
    {
        var original = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName); var x = await new CriterionService(new Repo()).CreateAsync(new CreateCriterionDto { Name = "target", Direction = CriterionDirection.Target, TargetValue = 0.000001m }); Assert.Equal(0.000001m, x.TargetValue); }
        finally { CultureInfo.CurrentCulture = original; }
    }

    private static Criterion New(string name) => new() { Id = Guid.NewGuid(), Name = name, Direction = CriterionDirection.Maximize, IsActive = true, CreatedAt = DateTime.UtcNow };
    private sealed class Repo(params Criterion[] initial) : ICriterionRepository
    {
        public List<Criterion> Items { get; } = [.. initial];
        public bool HasDependencies { get; init; }
        public Task<IReadOnlyList<Criterion>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Criterion>>(Items.ToArray());
        public Task<Criterion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<bool> ExistsByNameAsync(string name, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(x => x.Name == name && x.Id != excludingId));
        public Task<bool> HasDependenciesAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(HasDependencies);
        public Task AddAsync(Criterion criterion, CancellationToken cancellationToken = default) { Items.Add(criterion); return Task.CompletedTask; }
        public Task UpdateAsync(Criterion criterion, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Criterion criterion, CancellationToken cancellationToken = default) { Items.Remove(criterion); return Task.CompletedTask; }
    }
}
