using CatchTheSun.API.Controllers;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Sun;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class LadybugInsolationApiTests
{
    [Fact]
    public async Task GetRoutesReturnImportedRealCsvAndMeasurementsInPages()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "direct_sun_hours_points.csv");
        var csv = await File.ReadAllTextAsync(path);
        var parsed = new DirectSunCsvParser().Parse(csv);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var site = new Site { Id = Guid.NewGuid(), Name = "Audit", Location = "Test", Area = 10m, CreatedAt = DateTime.UtcNow };
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Ladybug test", CreatedAt = DateTime.UtcNow };
        var import = new DirectSunImport { Id = Guid.NewGuid(), VariantId = variant.Id, SourceCsv = parsed.SourceCsv, ImportedAt = DateTime.UtcNow };
        foreach (var x in parsed.Measurements)
            import.Measurements.Add(new DirectSunMeasurementRecord {
                Id = Guid.NewGuid(), SensorId = x.SensorId, FeatureId = x.FeatureId, RecreationalType = x.RecreationalType,
                XMetres = x.XMetres, YMetres = x.YMetres, ZMetres = x.ZMetres,
                HeightAboveZoneMetres = x.HeightAboveZoneMetres, GridSpacingMetres = x.GridSpacingMetres,
                Longitude = x.Longitude, Latitude = x.Latitude, DirectSunHoursTestDay = x.DirectSunHoursTestDay,
                SunlitSamples = x.SunlitSamples, SampleCountDaylight = x.SampleCountDaylight, RawRow = x.RawRow
            });
        db.Sites.Add(site);
        db.Variants.Add(variant);
        db.DirectSunImports.Add(import);
        await db.SaveChangesAsync();

        var controller = new LadybugInsolationController(db);
        var variantsResult = await controller.GetForVariant(variant.Id, default);
        var variantsResponse = Assert.IsType<OkObjectResult>(variantsResult.Result);
        var variants = Assert.IsAssignableFrom<IReadOnlyList<LadybugImportDto>>(variantsResponse.Value);
        Assert.Single(variants);
        Assert.Equal(347, variants[0].MeasurementCount);

        var importResult = await controller.GetById(import.Id, default);
        var importResponse = Assert.IsType<OkObjectResult>(importResult.Result);
        var details = Assert.IsType<LadybugImportDetailsDto>(importResponse.Value);
        Assert.Equal(csv, details.SourceCsv);
        Assert.Equal(8, details.FeatureCount);

        var pages = new List<LadybugMeasurementPage>();
        for (var pageNumber = 1; pageNumber <= 4; pageNumber++)
        {
            var response = await controller.GetMeasurements(import.Id, pageNumber, 100, default);
            var page = Assert.IsType<OkObjectResult>(response.Result).Value as LadybugMeasurementPage;
            Assert.NotNull(page);
            Assert.Equal(347, page.TotalCount);
            pages.Add(page);
        }
        Assert.Equal(new[] { 100, 100, 100, 47 }, pages.Select(x => x.Items.Count));
        Assert.Equal(347, pages.SelectMany(x => x.Items).Select(x => x.SensorId)
            .Distinct(StringComparer.Ordinal).Count());
    }
}
