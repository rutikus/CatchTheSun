using System.Text;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Sun;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api/variants/{variantId:guid}/urban-sketch-imports")]
public sealed class UrbanSketchImportsController(ApplicationDbContext db, DirectSunCsvParser parser) : ControllerBase
{
    [HttpPost]
    [Consumes("text/csv", "application/octet-stream")]
    public async Task<ActionResult<UrbanSketchImportResponse>> Import(Guid variantId, CancellationToken ct)
    {
        if (!await db.Variants.AnyAsync(x => x.Id == variantId, ct)) return NotFound();
        using var reader = new StreamReader(Request.Body, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
        var csv = await reader.ReadToEndAsync(ct);
        CatchTheSun.Core.Sun.Models.DirectSunImportResult parsed;
        try { parsed = parser.Parse(csv); }
        catch (FormatException ex) { return BadRequest(new { error = ex.Message }); }

        var import = new UrbanSketchImport
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            SourceCsv = csv,
            ImportedAt = DateTime.UtcNow
        };
        foreach (var item in parsed.Measurements)
        {
            import.Measurements.Add(new UrbanSketchMeasurementRecord
            {
                Id = Guid.NewGuid(),
                SensorId = item.SensorId,
                FeatureId = item.FeatureId,
                RecreationalType = item.RecreationalType,
                XMetres = item.XMetres,
                YMetres = item.YMetres,
                ZMetres = item.ZMetres,
                HeightAboveZoneMetres = item.HeightAboveZoneMetres,
                GridSpacingMetres = item.GridSpacingMetres,
                Longitude = item.Longitude,
                Latitude = item.Latitude,
                DirectSunHoursTestDay = item.DirectSunHoursTestDay,
                SunlitSamples = item.SunlitSamples,
                SampleCountDaylight = item.SampleCountDaylight,
                RawRow = item.RawRow
            });
        }

        db.UrbanSketchImports.Add(import);
        await db.SaveChangesAsync(ct);
        return Ok(new UrbanSketchImportResponse(import.Id, parsed.ProcessedRecordCount, parsed.FeatureCount,
            parsed.MinimumDirectSunHours, parsed.MaximumDirectSunHours));
    }
}

public sealed record UrbanSketchImportResponse(Guid ImportId, int ProcessedRecordCount, int FeatureCount,
    double MinimumDirectSunHours, double MaximumDirectSunHours);
