using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Services;
using CatchTheSun.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Xunit;

namespace CatchTheSun.Tests;

public class DataIntegrityTests
{
    [Fact]
    public void TestCultureUsesInvariantDecimalSeparator()
    {
        Assert.Equal("1.5", 1.5m.ToString(CultureInfo.CurrentCulture));
    }

    [Fact]
    public async Task SiteWithCalculationHistoryCannotBeDeleted()
    {
        await using var database = await TestDatabase.CreateAsync();
        var site = NewSite();
        var calculation = NewCalculation(site.Id);
        database.Context.AddRange(site, calculation);
        await database.Context.SaveChangesAsync();

        database.Context.ChangeTracker.Clear();
        database.Context.Remove(await database.Context.Sites.SingleAsync(x => x.Id == site.Id));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task ResultCannotReferToVariantFromDifferentSite()
    {
        await using var database = await TestDatabase.CreateAsync();
        var siteA = NewSite();
        var siteB = NewSite();
        var calculation = NewCalculation(siteA.Id);
        var variantB = NewVariant(siteB.Id);
        database.Context.AddRange(siteA, siteB, calculation, variantB);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        database.Context.CalculationResults.Add(new CalculationResult
        {
            Id = Guid.NewGuid(), CalculationId = calculation.Id, VariantId = variantB.Id,
            SiteId = siteA.Id, Calculation = null!, Variant = null!
        });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task ComparisonCannotUseCriterionOutsideCalculation()
    {
        await using var database = await TestDatabase.CreateAsync();
        var site = NewSite();
        var included = NewCriterion("Included");
        var excluded = NewCriterion("Excluded");
        var calculation = NewCalculation(site.Id);
        calculation.Criteria.Add(NewCalculationCriterion(calculation.Id, included));
        database.Context.AddRange(site, included, excluded, calculation);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var ids = new[] { included.Id, excluded.Id }.OrderBy(x => x.ToString("N"), StringComparer.Ordinal).ToArray();
        database.Context.CriterionComparisons.Add(CriterionComparison.Create(calculation.Id, ids[0], ids[1], 3m));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public void CriterionCannotBeComparedWithItself()
    {
        var criterionId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => CriterionComparison.Create(Guid.NewGuid(), criterionId, criterionId, 3m));
    }

    [Fact]
    public void ReversingAHPComparisonNormalizesOrderAndInvertsValue()
    {
        var first = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var calculationId = Guid.NewGuid();

        var forward = CriterionComparison.Create(calculationId, first, second, 3m);
        var reverse = CriterionComparison.Create(calculationId, second, first, 3m);

        Assert.Equal(first, forward.CriterionAId);
        Assert.Equal(second, forward.CriterionBId);
        Assert.Equal(3m, forward.Value);
        Assert.Equal(forward.CriterionAId, reverse.CriterionAId);
        Assert.Equal(forward.CriterionBId, reverse.CriterionBId);
        Assert.Equal(1m / 3m, reverse.Value);
    }

    [Fact]
    public async Task ChangedCriterionValueDoesNotChangeSavedInputSnapshot()
    {
        await using var database = await TestDatabase.CreateAsync();
        var graph = await SeedCalculationAndVariantAsync(database.Context, 2m, 17m);
        var result = MakeResult(graph.Calculation, graph.Variant);
        result.InputSnapshots = CalculationInputSnapshotFactory.Capture(result).ToList();
        database.Context.CalculationResults.Add(result);
        await database.Context.SaveChangesAsync();

        graph.Variant.CriterionValues.Single().Value = 88m;
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var snapshot = await database.Context.Set<CalculationResultCriterionSnapshot>().SingleAsync();
        Assert.Equal(17m, snapshot.ValueSnapshot);
        Assert.Equal(2m, snapshot.WeightSnapshot);
        Assert.Equal(CriterionDirection.Target, snapshot.DirectionSnapshot);
        Assert.Equal(20m, snapshot.TargetValueSnapshot);
    }

    [Fact]
    public async Task EachCalculationKeepsItsOwnInputSnapshot()
    {
        await using var database = await TestDatabase.CreateAsync();
        var site = NewSite();
        var criterion = NewCriterion("Population");
        var variant = NewVariant(site.Id);
        variant.CriterionValues.Add(new CriterionValue
        {
            Id = Guid.NewGuid(), VariantId = variant.Id, CriterionId = criterion.Id, Value = 25m,
            Variant = variant, Criterion = criterion
        });
        var firstCalculation = NewCalculation(site.Id);
        firstCalculation.Criteria.Add(NewCalculationCriterion(firstCalculation.Id, criterion, 1m));
        var secondCalculation = NewCalculation(site.Id);
        secondCalculation.Criteria.Add(NewCalculationCriterion(secondCalculation.Id, criterion, 5m));
        database.Context.AddRange(site, criterion, variant, firstCalculation, secondCalculation);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();

        var calculations = await database.Context.Calculations
            .Include(x => x.Criteria).ThenInclude(x => x.Criterion)
            .OrderBy(x => x.CreatedAt).ToListAsync();
        var loadedVariant = await database.Context.Variants.Include(x => x.CriterionValues)
            .SingleAsync(x => x.Id == variant.Id);
        var results = calculations.Select(calculation =>
        {
            var result = MakeResult(calculation, loadedVariant);
            result.InputSnapshots = CalculationInputSnapshotFactory.Capture(result).ToList();
            return result;
        }).ToArray();
        database.Context.CalculationResults.AddRange(results);
        await database.Context.SaveChangesAsync();

        var snapshots = await database.Context.Set<CalculationResultCriterionSnapshot>()
            .OrderBy(x => x.WeightSnapshot).ToListAsync();
        Assert.Equal(2, snapshots.Count);
        Assert.NotEqual(snapshots[0].CalculationId, snapshots[1].CalculationId);
        Assert.Equal(new[] { 1m, 5m }, snapshots.Select(x => x.WeightSnapshot));
        Assert.All(snapshots, x => Assert.Equal(25m, x.ValueSnapshot));
    }

    private static Site NewSite() => new()
    {
        Id = Guid.NewGuid(), Name = "Test site", Location = "Test", Area = 1m,
        CreatedAt = DateTime.UtcNow
    };

    private static Variant NewVariant(Guid siteId) => new()
    {
        Id = Guid.NewGuid(), SiteId = siteId, Name = "Test variant", CreatedAt = DateTime.UtcNow
    };

    private static Criterion NewCriterion(string name) => new()
    {
        Id = Guid.NewGuid(), Name = name, Direction = CriterionDirection.Maximize,
        IsRequired = true, IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private static Calculation NewCalculation(Guid siteId) => new()
    {
        Id = Guid.NewGuid(), SiteId = siteId, Method = CalculationMethod.AHP,
        CreatedAt = DateTime.UtcNow
    };

    private static CalculationCriterion NewCalculationCriterion(Calculation calculation, Criterion criterion, decimal weight = 1m) =>
        NewCalculationCriterion(calculation.Id, criterion, weight);

    private static CalculationCriterion NewCalculationCriterion(Guid calculationId, Criterion criterion, decimal weight = 1m) => new()
    {
        Id = Guid.NewGuid(), CalculationId = calculationId, CriterionId = criterion.Id,
        Weight = weight, DirectionSnapshot = criterion.Direction,
        TargetValueSnapshot = criterion.TargetValue, Criterion = criterion
    };

    private static CalculationResult MakeResult(Calculation calculation, Variant variant) => new()
    {
        Id = Guid.NewGuid(), CalculationId = calculation.Id, VariantId = variant.Id,
        SiteId = calculation.SiteId, Calculation = calculation, Variant = variant
    };

    private static async Task<(Calculation Calculation, Variant Variant)> SeedCalculationAndVariantAsync(
        ApplicationDbContext context, decimal weight, decimal inputValue)
    {
        var site = NewSite();
        var criterion = NewCriterion("Test criterion");
        criterion.Direction = CriterionDirection.Target;
        criterion.TargetValue = 20m;
        var variant = NewVariant(site.Id);
        variant.CriterionValues.Add(new CriterionValue
        {
            Id = Guid.NewGuid(), VariantId = variant.Id, CriterionId = criterion.Id,
            Value = inputValue, Variant = variant, Criterion = criterion
        });
        var calculation = NewCalculation(site.Id);
        calculation.Criteria.Add(NewCalculationCriterion(calculation.Id, criterion, weight));
        context.AddRange(site, criterion, variant, calculation);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        calculation = await context.Calculations.Include(x => x.Criteria).ThenInclude(x => x.Criterion)
            .SingleAsync(x => x.Id == calculation.Id);
        variant = await context.Variants.Include(x => x.CriterionValues)
            .SingleAsync(x => x.Id == variant.Id);
        return (calculation, variant);
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public ApplicationDbContext Context { get; }

        private TestDatabase(SqliteConnection connection, ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
