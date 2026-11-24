using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class CalculationServiceTests
{
    [Theory]
    [InlineData(CalculationMethod.AHP)]
    [InlineData(CalculationMethod.TOPSIS)]
    [InlineData(CalculationMethod.Pareto)]
    public async Task CreateAsyncPersistsCompleteSnapshotsForEveryAlgorithm(CalculationMethod method)
    {
        var source = TestData.Create();
        var repo = new FakeCalculationRepository(source);
        var service = CreateService(repo);
        var command = TestData.Command(source, method);

        var result = await service.CreateAsync(command);

        Assert.Equal(source.Site!.Id, result.SiteId);
        Assert.Equal(method, result.Method);
        Assert.Equal("1.0.0", result.AlgorithmVersion);
        Assert.Equal(2, result.Results.Count);
        Assert.All(result.Results, x => Assert.Single(x.Inputs));
        Assert.Single(result.Criteria);
        Assert.Equal("Solar exposure", result.Criteria[0].Name);
        Assert.Equal("kWh", result.Criteria[0].Unit);
        Assert.Equal(source.CriterionValues[0].Value, result.Results.Single(x => x.VariantId == source.Variants[0].Id).Inputs[0].Value);
        Assert.True(repo.Session.Saved);
        Assert.True(repo.Session.Committed);
        Assert.Equal(0, repo.Session.RollbackCount);
    }

    [Fact]
    public async Task CreateAsyncReturnsNotFoundWhenSiteDoesNotExist()
    {
        var data = TestData.Create();
        var command = TestData.Command(data, CalculationMethod.TOPSIS);
        var source = data with { Site = null };
        var repository = new FakeCalculationRepository(source);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal(CalculationServiceErrorKind.NotFound, exception.Kind);
        Assert.Equal("calculation.site_not_found", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRejectsVariantFromAnotherSite()
    {
        var source = TestData.Create();
        source.Variants[1].SiteId = Guid.NewGuid();
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() =>
            service.CreateAsync(TestData.Command(source, CalculationMethod.TOPSIS)));

        Assert.Equal(CalculationServiceErrorKind.Validation, exception.Kind);
        Assert.Equal("calculation.variant_site_mismatch", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncReturnsNotFoundWhenVariantDoesNotExist()
    {
        var data = TestData.Create();
        var command = TestData.Command(data, CalculationMethod.TOPSIS);
        var source = data with { Variants = [data.Variants[0]] };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal(CalculationServiceErrorKind.NotFound, exception.Kind);
        Assert.Equal("calculation.variant_not_found", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncReturnsNotFoundWhenCriterionDoesNotExist()
    {
        var source = TestData.Create();
        var command = TestData.Command(source, CalculationMethod.TOPSIS) with
        {
            Criteria = [new CalculationCriterionSelection(Guid.NewGuid(), 1m)]
        };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal(CalculationServiceErrorKind.NotFound, exception.Kind);
        Assert.Equal("calculation.criterion_not_found", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncPersistsAHPComparisonAndConsistencyDiagnostics()
    {
        var source = TestData.Create(criterionCount: 2);
        var repository = new FakeCalculationRepository(source);
        var service = CreateService(repository);

        var result = await service.CreateAsync(TestData.Command(source, CalculationMethod.AHP));

        Assert.Single(result.Comparisons);
        Assert.Equal(1m, result.Comparisons[0].Value);
        Assert.NotNull(result.ConsistencyRatio);
        Assert.NotNull(result.LambdaMax);
        Assert.True(repository.Session.Saved);
        Assert.True(repository.Session.Committed);
    }

    [Fact]
    public async Task CreateAsyncRejectsDuplicateVariantIds()
    {
        var source = TestData.Create();
        var command = TestData.Command(source, CalculationMethod.TOPSIS) with
        {
            VariantIds = [source.Variants[0].Id, source.Variants[0].Id]
        };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal("calculation.variant_ids_invalid", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRejectsFewerThanTwoVariants()
    {
        var source = TestData.Create();
        var command = TestData.Command(source, CalculationMethod.TOPSIS) with
        {
            VariantIds = [source.Variants[0].Id]
        };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal("calculation.variants_too_few", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRequiresAllActiveRequiredCriteria()
    {
        var source = TestData.Create();
        var required = TestData.NewCriterion(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Required");
        required.IsRequired = true;
        source = source with { Criteria = [.. source.Criteria, required] };
        var command = TestData.Command(source, CalculationMethod.TOPSIS) with
        {
            Criteria = [new CalculationCriterionSelection(source.Criteria[0].Id, 1m)]
        };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal("calculation.required_criteria_missing", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRejectsIncompleteValueMatrix()
    {
        var source = TestData.Create();
        source = source with { CriterionValues = source.CriterionValues.Take(1).ToArray() };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() =>
            service.CreateAsync(TestData.Command(source, CalculationMethod.TOPSIS)));

        Assert.Equal("calculation.matrix_incomplete", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRequiresTargetValueForTargetCriterion()
    {
        var source = TestData.Create();
        source.Criteria[0].Direction = CriterionDirection.Target;
        source.Criteria[0].TargetValue = null;
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() =>
            service.CreateAsync(TestData.Command(source, CalculationMethod.Pareto)));

        Assert.Equal("calculation.target_value_required", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRejectsInvalidTopsisWeights()
    {
        var source = TestData.Create();
        var command = TestData.Command(source, CalculationMethod.TOPSIS) with
        {
            Criteria = [new CalculationCriterionSelection(source.Criteria[0].Id, 0.7m)]
        };
        var service = CreateService(new FakeCalculationRepository(source));

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal(CalculationServiceErrorKind.Validation, exception.Kind);
        Assert.StartsWith("calculation.topsis.", exception.Code);
    }

    [Fact]
    public async Task CreateAsyncRejectsInconsistentAhpComparisons()
    {
        var source = TestData.Create(criterionCount: 3);
        var ids = source.Criteria.Select(x => x.Id).OrderBy(x => x.ToString("N"), StringComparer.Ordinal).ToArray();
        var command = TestData.Command(source, CalculationMethod.AHP) with
        {
            Criteria = source.Criteria.Select(x => new CalculationCriterionSelection(x.Id, null)).ToArray(),
            Comparisons =
            [
                new(ids[0], ids[1], 9m),
                new(ids[1], ids[2], 9m),
                new(ids[0], ids[2], 1m)
            ]
        };
        var repository = new FakeCalculationRepository(source);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CalculationServiceException>(() => service.CreateAsync(command));

        Assert.Equal("calculation.ahp.inconsistent_matrix", exception.Code);
        Assert.Equal(0, repository.Session.SaveCount);
    }

    [Fact]
    public async Task CreateAsyncRollsBackWhenSaveFails()
    {
        var source = TestData.Create();
        var repo = new FakeCalculationRepository(source) { FailOnSave = true };
        var service = CreateService(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(TestData.Command(source, CalculationMethod.TOPSIS)));

        Assert.Equal(1, repo.Session.RollbackCount);
        Assert.False(repo.Session.Committed);
    }

    private static CalculationService CreateService(FakeCalculationRepository repository) =>
        new(repository, new CalculationAlgorithmFactory(
        [new AhpWeightedSumAlgorithm(), new TopsisAlgorithm(), new ParetoAlgorithm()]));

    private sealed class FakeCalculationRepository(CalculationSourceData source) : ICalculationRepository
    {
        public FakeCalculationSession Session { get; } = new(source);
        public bool FailOnSave { get; init; }

        public Task<Calculation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Calculation?>(null);
        }

        public Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(source.Site?.Id == siteId);
        }

        public Task<IReadOnlyList<CalculationHistoryItem>> GetBySiteIdAsync(
            Guid siteId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<CalculationHistoryItem>>([]);
        }

        public Task<ICalculationCreationSession> BeginCreationAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Session.FailOnSave = FailOnSave;
            return Task.FromResult<ICalculationCreationSession>(Session);
        }

        public sealed class FakeCalculationSession(CalculationSourceData source) : ICalculationCreationSession
        {
            public bool FailOnSave { get; set; }
            public bool Saved { get; private set; }
            public int SaveCount { get; private set; }
            public bool Committed { get; private set; }
            public int RollbackCount { get; private set; }

            public Task<CalculationSourceData> LoadSourceAsync(
                Guid siteId,
                IReadOnlyCollection<Guid> variantIds,
                IReadOnlyCollection<Guid> criterionIds,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(source);
            }

            public Task SaveAsync(Calculation calculation, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SaveCount++;
                if (FailOnSave)
                    throw new InvalidOperationException("Persistence failed.");
                Saved = true;
                return Task.CompletedTask;
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Committed = true;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                if (!Committed)
                    RollbackCount++;
                return ValueTask.CompletedTask;
            }
        }
    }

    private static class TestData
    {
        public static CalculationSourceData Create(int criterionCount = 1)
        {
            var site = new Site
            {
                Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                Name = "Test site", Location = "Test", Area = 10m, CreatedAt = DateTime.UtcNow
            };
            var variants = new[]
            {
                new Variant { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), SiteId = site.Id, Name = "Variant A", ExternalId = "ext-a", CreatedAt = DateTime.UtcNow },
                new Variant { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), SiteId = site.Id, Name = "Variant B", ExternalId = "ext-b", CreatedAt = DateTime.UtcNow }
            };
            var criteria = Enumerable.Range(1, criterionCount)
                .Select(index => NewCriterion(Guid.Parse($"30000000-0000-0000-0000-{index:D12}"), index == 1 ? "Solar exposure" : $"Criterion {index}"))
                .ToArray();
            var values = variants.SelectMany((variant, variantIndex) => criteria.Select((criterion, criterionIndex) =>
                new CriterionValue
                {
                    Id = Guid.NewGuid(), VariantId = variant.Id, CriterionId = criterion.Id,
                    Value = 20m + variantIndex * 10m + criterionIndex
                })).ToArray();
            return new CalculationSourceData(site, variants, criteria, values);
        }

        public static Criterion NewCriterion(Guid id, string name) => new()
        {
            Id = id, Name = name, Unit = "kWh", Direction = CriterionDirection.Maximize,
            IsActive = true, IsRequired = true, CreatedAt = DateTime.UtcNow
        };

        public static CreateCalculationCommand Command(CalculationSourceData source, CalculationMethod method)
        {
            var selections = source.Criteria.Select(x => new CalculationCriterionSelection(
                x.Id, method == CalculationMethod.TOPSIS ? 1m / source.Criteria.Count : null)).ToArray();
            var comparisons = method == CalculationMethod.AHP && source.Criteria.Count == 2
                ? new[] { new CalculationCriterionComparisonInput(source.Criteria[0].Id, source.Criteria[1].Id, 1m) }
                : [];
            return new CreateCalculationCommand(
                source.Site!.Id, source.Variants.Select(x => x.Id).ToArray(), method, selections, comparisons);
        }
    }
}
