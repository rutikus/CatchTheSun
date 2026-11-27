using System.Text;
using CatchTheSun.API.Controllers;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Sun;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class UrbanSketchInsolationApiTests
{
    private static readonly string CsvPath = Path.Combine(AppContext.BaseDirectory, "TestData", "direct_sun_hours_points.csv");

    [Fact]
    public async Task ImportsAndReadsSameSchemaCsvSeparatelyFromLadybugIncludingRepeatAndPagination()
    {
        var csvBytes = await File.ReadAllBytesAsync(CsvPath);
        var csv = Encoding.UTF8.GetString(csvBytes);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var site = new Site { Id = Guid.NewGuid(), Name = "CSV schema test", Location = "Test fixture only", Area = 1m, CreatedAt = DateTime.UtcNow };
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Two source import test", CreatedAt = DateTime.UtcNow };
        db.Sites.Add(site);
        db.Variants.Add(variant);
        await db.SaveChangesAsync();

        var ladybug = new DirectSunImportsController(db, new DirectSunCsvParser());
        SetBody(ladybug, csvBytes);
        var ladybugResult = await ladybug.Import(variant.Id, default);
        Assert.IsType<OkObjectResult>(ladybugResult.Result);

        var urbanSketch = new UrbanSketchImportsController(db, new DirectSunCsvParser());
        SetBody(urbanSketch, csvBytes);
        var firstPost = await urbanSketch.Import(variant.Id, default);
        var firstResponse = Assert.IsType<OkObjectResult>(firstPost.Result);
        var firstImport = Assert.IsType<UrbanSketchImportResponse>(firstResponse.Value);
        Assert.Equal(347, firstImport.ProcessedRecordCount);
        Assert.Equal(8, firstImport.FeatureCount);

        SetBody(urbanSketch, csvBytes);
        var repeatedPost = await urbanSketch.Import(variant.Id, default);
        var repeatedResponse = Assert.IsType<OkObjectResult>(repeatedPost.Result);
        var repeatedImport = Assert.IsType<UrbanSketchImportResponse>(repeatedResponse.Value);
        Assert.NotEqual(firstImport.ImportId, repeatedImport.ImportId);

        Assert.Equal(347, await db.DirectSunMeasurements.CountAsync());
        Assert.Equal(694, await db.UrbanSketchMeasurements.CountAsync());
        Assert.Equal(1, await db.DirectSunImports.CountAsync());
        Assert.Equal(2, await db.UrbanSketchImports.CountAsync());

        var getController = new UrbanSketchInsolationController(db);
        var variantGet = await getController.GetForVariant(variant.Id, default);
        var imports = Assert.IsAssignableFrom<IReadOnlyList<UrbanSketchImportDto>>(
            Assert.IsType<OkObjectResult>(variantGet.Result).Value);
        Assert.Equal(2, imports.Count);
        Assert.All(imports, x => Assert.Equal(347, x.MeasurementCount));

        var detailsGet = await getController.GetById(firstImport.ImportId, default);
        var details = Assert.IsType<UrbanSketchImportDetailsDto>(Assert.IsType<OkObjectResult>(detailsGet.Result).Value);
        Assert.Equal(csv, details.SourceCsv);

        var pages = new List<UrbanSketchMeasurementPage>();
        for (var pageNumber = 1; pageNumber <= 4; pageNumber++)
        {
            var result = await getController.GetMeasurements(firstImport.ImportId, pageNumber, 100, default);
            var page = Assert.IsType<UrbanSketchMeasurementPage>(Assert.IsType<OkObjectResult>(result.Result).Value);
            Assert.Equal(347, page.TotalCount);
            pages.Add(page);
        }
        Assert.Equal(new[] { 100, 100, 100, 47 }, pages.Select(x => x.Items.Count));
        Assert.Equal(347, pages.SelectMany(x => x.Items).Select(x => x.SensorId).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task InvalidFeatureGuidDoesNotCreateUrbanSketchImport()
    {
        var csv = await File.ReadAllTextAsync(CsvPath);
        csv = csv.Replace("092a4b73-9ced-4856-89fe-740d0bd81ca4", "not-a-guid", StringComparison.Ordinal);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var site = new Site { Id = Guid.NewGuid(), Name = "Validation test", Location = "Test fixture only", Area = 1m, CreatedAt = DateTime.UtcNow };
        var variant = new Variant { Id = Guid.NewGuid(), SiteId = site.Id, Name = "Validation test", CreatedAt = DateTime.UtcNow };
        db.Sites.Add(site);
        db.Variants.Add(variant);
        await db.SaveChangesAsync();

        var controller = new UrbanSketchImportsController(db, new DirectSunCsvParser());
        SetBody(controller, Encoding.UTF8.GetBytes(csv));
        var result = await controller.Import(variant.Id, default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(await db.UrbanSketchImports.ToListAsync());
        Assert.Empty(await db.UrbanSketchMeasurements.ToListAsync());
    }

    private static void SetBody(ControllerBase controller, byte[] content)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(content);
        context.Request.ContentType = "text/csv";
        controller.ControllerContext = new ControllerContext { HttpContext = context };
    }
}
