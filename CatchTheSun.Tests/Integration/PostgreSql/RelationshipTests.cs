using CatchTheSun.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CatchTheSun.Tests.Integration.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class RelationshipTests(PostgreSqlFixture fixture)
{
    [PostgreSqlFact]
    public async Task CriterionValueCannotReferenceMissingVariant()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var criterion = NewCriterion();
        db.Criteria.Add(criterion);
        await db.SaveChangesAsync();
        db.CriterionValues.Add(NewValue(Guid.NewGuid(), criterion.Id));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ((PostgresException)exception.InnerException!).SqlState);
    }

    [PostgreSqlFact]
    public async Task CriterionValueCannotReferenceMissingCriterion()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var variant = await CreateVariantAsync(db);
        db.CriterionValues.Add(NewValue(variant.Id, Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ((PostgresException)exception.InnerException!).SqlState);
    }

    [PostgreSqlFact]
    public async Task DeletingVariantCascadesToCriterionValues()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var variant = await CreateVariantAsync(db);
        var criterion = NewCriterion();
        db.Criteria.Add(criterion);
        await db.SaveChangesAsync();
        var value = NewValue(variant.Id, criterion.Id);
        db.CriterionValues.Add(value);
        await db.SaveChangesAsync();

        db.Variants.Remove(variant);
        await db.SaveChangesAsync();

        Assert.False(await db.CriterionValues.AsNoTracking().AnyAsync(x => x.Id == value.Id));
    }

    [PostgreSqlFact]
    public async Task DeletingCriterionUsedByCriterionValueIsRestricted()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var variant = await CreateVariantAsync(db);
        var criterion = NewCriterion();
        db.Criteria.Add(criterion);
        await db.SaveChangesAsync();
        db.CriterionValues.Add(NewValue(variant.Id, criterion.Id));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Criteria.Where(x => x.Id == criterion.Id).ExecuteDeleteAsync());

        Assert.Equal("23001", exception.SqlState);
        await transaction.RollbackAsync();
    }

    [PostgreSqlFact]
    public async Task DeletingCriterionUsedByCalculationCriterionIsRestricted()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (site, calculation) = await CreateCalculationAsync(db);
        var criterion = NewCriterion();
        db.Criteria.Add(criterion);
        await db.SaveChangesAsync();
        db.CalculationCriteria.Add(new CalculationCriterion
        {
            Id = Guid.NewGuid(), CalculationId = calculation.Id, CriterionId = criterion.Id, Weight = 1m,
            DirectionSnapshot = CriterionDirection.Maximize
        });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Criteria.Where(x => x.Id == criterion.Id).ExecuteDeleteAsync());

        Assert.Equal("23001", exception.SqlState);
        await transaction.RollbackAsync();
    }

    [PostgreSqlFact]
    public async Task DeletingVariantUsedByCalculationResultIsRestricted()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (site, calculation) = await CreateCalculationAsync(db);
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Test variant", CreatedAt = DateTime.UtcNow };
        db.Variants.Add(variant);
        await db.SaveChangesAsync();
        db.CalculationResults.Add(new CalculationResult
        {
            Id = Guid.NewGuid(), CalculationId = calculation.Id, VariantId = variant.Id, SiteId = site.Id, Score = 1m
        });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Variants.Where(x => x.Id == variant.Id).ExecuteDeleteAsync());

        Assert.Equal("23001", exception.SqlState);
        await transaction.RollbackAsync();
    }

    private static async Task<Variant> CreateVariantAsync(CatchTheSun.Data.ApplicationDbContext db)
    {
        var site = NewSite();
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Test variant", CreatedAt = DateTime.UtcNow };
        db.AddRange(site, variant);
        await db.SaveChangesAsync();
        return variant;
    }

    private static async Task<(Site Site, Calculation Calculation)> CreateCalculationAsync(CatchTheSun.Data.ApplicationDbContext db)
    {
        var site = NewSite();
        var calculation = new Calculation { Id = Guid.NewGuid(), SiteId = site.Id, Method = CalculationMethod.AHP, CreatedAt = DateTime.UtcNow };
        db.AddRange(site, calculation);
        await db.SaveChangesAsync();
        return (site, calculation);
    }

    private static Site NewSite() => new()
    {
        Id = Guid.NewGuid(), Name = $"Integration {Guid.NewGuid():N}", Location = "Test fixture", Area = 1m, CreatedAt = DateTime.UtcNow
    };

    private static Criterion NewCriterion() => new()
    {
        Id = Guid.NewGuid(), Name = $"Integration {Guid.NewGuid():N}", Direction = CriterionDirection.Maximize,
        IsRequired = true, IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private static CriterionValue NewValue(Guid variantId, Guid criterionId) => new()
    {
        Id = Guid.NewGuid(), VariantId = variantId, CriterionId = criterionId, Value = 1.234567m
    };
}
