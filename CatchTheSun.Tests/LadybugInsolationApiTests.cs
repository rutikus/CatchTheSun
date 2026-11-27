using CatchTheSun.API.Controllers;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Sun;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class LadybugInsolationApiTests
{
    [Fact]
    public async Task GetRoutesReturnImportedRealCsvAndMeasurementsInPages()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "direct_sun_hours_points.csv");
        var csv = await File.ReadAllTextAsync(path);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var site = new Site { Id = Guid.NewGuid(), Name = "Audit", Location = "Test", Area = 10m, CreatedAt = DateTime.UtcNow };
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Ladybug test", CreatedAt = DateTime.UtcNow };
        db.Sites.Add(site);
        db.Variants.Add(variant);
        await db.SaveChangesAsync();

        var importController = new DirectSunImportsController(db, new DirectSunCsvParser());
        var http = new DefaultHttpContext();
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        http.Request.ContentType = "text/csv";
        importController.ControllerContext = new ControllerContext { HttpContext = http };
        var postResult = await importController.Import(variant.Id, default);
        var postResponse = Assert.IsType<OkObjectResult>(postResult.Result);
        var imported = Assert.IsType<DirectSunImportResponse>(postResponse.Value);
        Assert.Equal(347, imported.ProcessedRecordCount);
        Assert.Equal(8, imported.FeatureCount);

        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var repeatedPostResult = await importController.Import(variant.Id, default);
        var repeatedPostResponse = Assert.IsType<OkObjectResult>(repeatedPostResult.Result);
        var repeatedImport = Assert.IsType<DirectSunImportResponse>(repeatedPostResponse.Value);
        Assert.NotEqual(imported.ImportId, repeatedImport.ImportId);
        Assert.Equal(694, await db.DirectSunMeasurements.CountAsync());

        var controller = new LadybugInsolationController(db);
        var variantsResult = await controller.GetForVariant(variant.Id, default);
        var variantsResponse = Assert.IsType<OkObjectResult>(variantsResult.Result);
        var variants = Assert.IsAssignableFrom<IReadOnlyList<LadybugImportDto>>(variantsResponse.Value);
        Assert.Equal(2, variants.Count);
        Assert.All(variants, x => Assert.Equal(347, x.MeasurementCount));

        var importResult = await controller.GetById(imported.ImportId, default);
        var importResponse = Assert.IsType<OkObjectResult>(importResult.Result);
        var details = Assert.IsType<LadybugImportDetailsDto>(importResponse.Value);
        Assert.Equal(csv, details.SourceCsv);
        Assert.Equal(8, details.FeatureCount);

        var pages = new List<LadybugMeasurementPage>();
        for (var pageNumber = 1; pageNumber <= 4; pageNumber++)
        {
            var response = await controller.GetMeasurements(imported.ImportId, pageNumber, 100, default);
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
