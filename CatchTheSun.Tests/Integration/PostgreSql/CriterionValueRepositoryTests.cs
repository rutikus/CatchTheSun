using CatchTheSun.Core.Entities;
using CatchTheSun.Data;
using CatchTheSun.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CatchTheSun.Tests.Integration.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class CriterionValueRepositoryTests(PostgreSqlFixture fixture)
{
    [PostgreSqlFact]
    public async Task ExistingMigrationsApplyAndMatchPostgreSqlSchema()
    {
        await using var db = fixture.CreateDbContext();
        var applied = await db.Database.GetAppliedMigrationsAsync();
        Assert.Equal(new[] { "20261124134048_InitialCreate", "20261124175506_StrengthenDataIntegrity" }, applied);

        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT format_type(a.atttypid, a.atttypmod) FROM pg_attribute a JOIN pg_class t ON t.oid = a.attrelid JOIN pg_namespace n ON n.oid = t.relnamespace WHERE n.nspname = current_schema() AND t.relname = 'CriterionValues' AND a.attname = 'Value' AND NOT a.attisdropped";
        await db.Database.OpenConnectionAsync();
        Assert.Equal("numeric(20,6)", await command.ExecuteScalarAsync());
    }

    [PostgreSqlFact]
    public async Task RepositorySavesAndReadsCriterionValue()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (variant, criterion) = await CreateParentsAsync(db);
        var value = NewValue(variant.Id, criterion.Id, 12.345678m);
        var repository = new CriterionValueRepository(db);

        await repository.AddAsync(value);
        var saved = await repository.GetByIdAsync(value.Id);

        Assert.NotNull(saved);
        Assert.Equal(variant.Id, saved.VariantId);
        Assert.Equal(criterion.Id, saved.CriterionId);
        Assert.Equal(12.345678m, saved.Value);
    }

    [PostgreSqlFact]
    public async Task UniqueIndexRejectsDuplicateVariantAndCriterionPair()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (variant, criterion) = await CreateParentsAsync(db);
        db.CriterionValues.Add(NewValue(variant.Id, criterion.Id, 1m));
        await db.SaveChangesAsync();
        db.CriterionValues.Add(NewValue(variant.Id, criterion.Id, 2m));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)exception.InnerException!).SqlState);
    }

    [PostgreSqlFact]
    public async Task NumericScaleRoundsToSixDecimalPlaces()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (variant, criterion) = await CreateParentsAsync(db);
        var value = NewValue(variant.Id, criterion.Id, 1.1234567m);
        db.CriterionValues.Add(value);
        await db.SaveChangesAsync();
        db.Entry(value).State = EntityState.Detached;

        var saved = await db.CriterionValues.AsNoTracking().SingleAsync(x => x.Id == value.Id);

        Assert.Equal(1.123457m, saved.Value);
    }

    [PostgreSqlFact]
    public async Task NumericAcceptsMaximumPositiveAndNegativeBoundaryValues()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (variant, criterion) = await CreateParentsAsync(db);
        var positive = NewValue(variant.Id, criterion.Id, 99999999999999.999999m);
        var otherCriterion = NewCriterion();
        db.Criteria.Add(otherCriterion);
        var negative = NewValue(variant.Id, otherCriterion.Id, -99999999999999.999999m);
        db.CriterionValues.AddRange(positive, negative);

        await db.SaveChangesAsync();

        Assert.Equal(99999999999999.999999m, (await db.CriterionValues.AsNoTracking().SingleAsync(x => x.Id == positive.Id)).Value);
        Assert.Equal(-99999999999999.999999m, (await db.CriterionValues.AsNoTracking().SingleAsync(x => x.Id == negative.Id)).Value);
    }

    [PostgreSqlFact]
    public async Task NumericRejectsValueAboveItsDeclaredRange()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var (variant, criterion) = await CreateParentsAsync(db);
        db.CriterionValues.Add(NewValue(variant.Id, criterion.Id, 100000000000000m));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.NumericValueOutOfRange, ((PostgresException)exception.InnerException!).SqlState);
    }

    private static async Task<(Variant Variant, Criterion Criterion)> CreateParentsAsync(ApplicationDbContext db)
    {
        var site = new Site { Id = Guid.NewGuid(), Name = $"Integration {Guid.NewGuid():N}", Location = "Test fixture", Area = 1m, CreatedAt = DateTime.UtcNow };
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Test variant", CreatedAt = DateTime.UtcNow };
        var criterion = NewCriterion();
        db.AddRange(site, variant, criterion);
        await db.SaveChangesAsync();
        return (variant, criterion);
    }

    private static Criterion NewCriterion() => new()
    {
        Id = Guid.NewGuid(), Name = $"Integration {Guid.NewGuid():N}", Direction = CriterionDirection.Maximize,
        IsRequired = true, IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private static CriterionValue NewValue(Guid variantId, Guid criterionId, decimal value) => new()
    {
        Id = Guid.NewGuid(), VariantId = variantId, CriterionId = criterionId, Value = value
    };
}
