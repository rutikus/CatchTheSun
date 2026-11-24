using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using CatchTheSun.Data;
using CatchTheSun.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CatchTheSun.Tests.Integration.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class CalculationServiceIntegrationTests(PostgreSqlFixture fixture)
{
    [PostgreSqlFact]
    public async Task CreationPersistsCalculationAndHistoricalSnapshotsAtomically()
    {
        var site = new Site
        {
            Id = Guid.NewGuid(), Name = "Calculation integration site", Location = "Test",
            Area = 42m, CreatedAt = DateTime.UtcNow
        };
        var variants = new[]
        {
            NewVariant(site.Id, "Variant A", "a-" + Guid.NewGuid().ToString("N")),
            NewVariant(site.Id, "Variant B", "b-" + Guid.NewGuid().ToString("N"))
        };
        var criterion = new Criterion
        {
            Id = Guid.NewGuid(), Name = "Integration criterion", Unit = "kWh",
            Direction = CriterionDirection.Maximize, IsRequired = true, IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        Guid? calculationId = null;
        var rolledBackCalculationId = Guid.NewGuid();

        try
        {
            await using (var seedDb = fixture.CreateDbContext())
            {
                seedDb.AddRange(site, variants[0], variants[1], criterion);
                seedDb.CriterionValues.AddRange(
                    new CriterionValue { Id = Guid.NewGuid(), VariantId = variants[0].Id, CriterionId = criterion.Id, Value = 12.25m },
                    new CriterionValue { Id = Guid.NewGuid(), VariantId = variants[1].Id, CriterionId = criterion.Id, Value = 18.5m });
                await seedDb.SaveChangesAsync();
            }

            await using (var db = fixture.CreateDbContext())
            {
                ICalculationRepository repository = new CalculationRepository(db);
                ICalculationService service = new CalculationService(repository, new CalculationAlgorithmFactory(
                [new AhpWeightedSumAlgorithm(), new TopsisAlgorithm(), new ParetoAlgorithm()]));
                var details = await service.CreateAsync(new CreateCalculationCommand(
                    site.Id,
                    variants.Select(x => x.Id).ToArray(),
                    CalculationMethod.TOPSIS,
                    [new CalculationCriterionSelection(criterion.Id, 1m)],
                    []));
                calculationId = details.Id;
            }

            await using (var failedSaveDb = fixture.CreateDbContext())
            {
                var repository = new CalculationRepository(failedSaveDb);
                await using var session = await repository.BeginCreationAsync();
                var invalidCalculation = new Calculation
                {
                    Id = rolledBackCalculationId,
                    SiteId = site.Id,
                    Method = CalculationMethod.TOPSIS,
                    CreatedAt = DateTime.UtcNow
                };
                invalidCalculation.Criteria.Add(new CalculationCriterion
                {
                    Id = Guid.NewGuid(),
                    CalculationId = rolledBackCalculationId,
                    CriterionId = Guid.NewGuid(),
                    Weight = 1m,
                    NameSnapshot = "Invalid FK row",
                    DirectionSnapshot = CriterionDirection.Maximize
                });

                var exception = await Assert.ThrowsAsync<CalculationServiceException>(
                    () => session.SaveAsync(invalidCalculation));
                Assert.Equal(CalculationServiceErrorKind.Conflict, exception.Kind);
            }

            await using (var verifyRollbackDb = fixture.CreateDbContext())
            {
                Assert.False(await verifyRollbackDb.Calculations.AnyAsync(x => x.Id == rolledBackCalculationId));
            }

            await using (var verifyDb = fixture.CreateDbContext())
            {
                var calculation = await verifyDb.Calculations
                    .Include(x => x.Criteria)
                    .Include(x => x.Results).ThenInclude(x => x.InputSnapshots)
                    .SingleAsync(x => x.Id == calculationId);

                Assert.Equal("1.0.0", calculation.AlgorithmVersion);
                Assert.Contains("Vector", calculation.OptionsJson);
                Assert.Single(calculation.Criteria);
                Assert.Equal("Integration criterion", calculation.Criteria.Single().NameSnapshot);
                Assert.Equal("kWh", calculation.Criteria.Single().UnitSnapshot);
                Assert.True(calculation.Criteria.Single().IsRequiredSnapshot);
                Assert.Equal(2, calculation.Results.Count);
                Assert.All(calculation.Results, result =>
                {
                    var variant = variants.Single(x => x.Id == result.VariantId);
                    Assert.Equal(variant.Name, result.VariantNameSnapshot);
                    Assert.Equal(variant.ExternalId, result.ExternalIdSnapshot);
                    var input = Assert.Single(result.InputSnapshots);
                    Assert.Equal(criterion.Id, input.CriterionId);
                    Assert.Equal(result.VariantId == variants[0].Id ? 12.25m : 18.5m, input.ValueSnapshot);
                    Assert.Equal(1m, input.WeightSnapshot);
                });
            }
        }
        finally
        {
            await using var cleanupDb = fixture.CreateDbContext();
            if (calculationId is { } id)
            {
                await cleanupDb.CalculationResults.Where(x => x.CalculationId == id).ExecuteDeleteAsync();
                await cleanupDb.Calculations.Where(x => x.Id == id).ExecuteDeleteAsync();
            }

            await cleanupDb.Variants.Where(x => variants.Select(v => v.Id).Contains(x.Id)).ExecuteDeleteAsync();
            await cleanupDb.Criteria.Where(x => x.Id == criterion.Id).ExecuteDeleteAsync();
            await cleanupDb.Sites.Where(x => x.Id == site.Id).ExecuteDeleteAsync();
        }
    }

    private static Variant NewVariant(Guid siteId, string name, string externalId) => new()
    {
        Id = Guid.NewGuid(), SiteId = siteId, Name = name, ExternalId = externalId, CreatedAt = DateTime.UtcNow
    };
}
