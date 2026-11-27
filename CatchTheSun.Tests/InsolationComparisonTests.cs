using CatchTheSun.Core.Services;
using CatchTheSun.API.Controllers;
using CatchTheSun.Core.Entities;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class InsolationComparisonTests
{
    [Fact] public void EqualValuesHaveZeroErrors() => Assert.Equal((0d, 0d), InsolationComparisonMath.Errors(4, 4));
    [Fact] public void ErrorsUseAbsoluteReferenceForRelativePercent()
    {
        var result = InsolationComparisonMath.Errors(4, 6);
        Assert.Equal(2, result.Absolute);
        Assert.Equal(50, result.Relative);
    }
    [Fact] public void ZeroReferenceHasNoRelativeError() => Assert.Null(InsolationComparisonMath.Errors(0, 7).Relative);
    [Fact] public void AggregatesMaeRmseAndMapeAndSkipsZeroReference()
    {
        var result = InsolationComparisonMath.Aggregate(new[] { (2d, (double?)50), (4d, (double?)null) });
        Assert.Equal(3, result.Mae);
        Assert.Equal(Math.Sqrt(10), result.Rmse);
        Assert.Equal(50, result.Mape);
    }
    [Fact] public void EmptyOrOnlyZeroReferenceHasNullMape()
    {
        Assert.Equal((null, null, null), InsolationComparisonMath.Aggregate(Array.Empty<(double, double?)>()));
        Assert.Null(InsolationComparisonMath.Aggregate(new[] { (3d, (double?)null) }).Mape);
    }

    [Fact] public async Task ComparisonPersistsOnlyExactSensorMatchesAndCanBePaged()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var site = new Site { Id = Guid.NewGuid(), Name = "site", Location = "test", Area = 1, CreatedAt = DateTime.UtcNow };
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "variant", CreatedAt = DateTime.UtcNow };
        var reference = new DirectSunImport { Id = Guid.NewGuid(), VariantId = variant.Id, SourceCsv = "ref", ImportedAt = DateTime.UtcNow };
        var test = new UrbanSketchImport { Id = Guid.NewGuid(), VariantId = variant.Id, SourceCsv = "test", ImportedAt = DateTime.UtcNow };
        reference.Measurements.Add(new DirectSunMeasurementRecord { Id = Guid.NewGuid(), SensorId = "full/sensor-1", DirectSunHoursTestDay = 2 });
        reference.Measurements.Add(new DirectSunMeasurementRecord { Id = Guid.NewGuid(), SensorId = "not-matched", DirectSunHoursTestDay = 4 });
        test.Measurements.Add(new UrbanSketchMeasurementRecord { Id = Guid.NewGuid(), SensorId = "full/sensor-1", DirectSunHoursTestDay = 3 });
        test.Measurements.Add(new UrbanSketchMeasurementRecord { Id = Guid.NewGuid(), SensorId = "only-test", DirectSunHoursTestDay = 7 });
        db.AddRange(site, variant, reference, test);
        await db.SaveChangesAsync();

        var controller = new InsolationComparisonsController(db);
        var created = Assert.IsType<CreatedAtActionResult>((await controller.Create(variant.Id,
            new CreateComparisonRequest("ladybug", reference.Id, "urbanSketch", test.Id), default)).Result);
        var dto = Assert.IsType<ComparisonDto>(created.Value);
        Assert.Equal(1, dto.ComparedPoints);
        Assert.Equal(2, dto.ReferencePoints);
        Assert.Equal(2, dto.TestPoints);
        Assert.Equal(1, dto.UnmatchedReferencePoints);
        Assert.Equal(1, dto.UnmatchedTestPoints);
        Assert.Equal(1, dto.Mae);
        var page = Assert.IsType<OkObjectResult>((await controller.GetPoints(dto.Id, 1, 1, default)).Result);
        Assert.Equal(1, Assert.IsType<ComparisonPointPage>(page.Value).TotalCount);
    }
}
