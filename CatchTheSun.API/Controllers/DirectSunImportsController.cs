using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Sun;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api/variants/{variantId:guid}/direct-sun-imports")]
public sealed class DirectSunImportsController(ApplicationDbContext db, DirectSunCsvParser parser) : ControllerBase
{
    [HttpPost]
    [Consumes("text/csv", "application/octet-stream")]
    public async Task<ActionResult<DirectSunImportResponse>> Import(Guid variantId, CancellationToken ct)
    {
        if (!await db.Variants.AnyAsync(x => x.Id == variantId, ct)) return NotFound();
        using var reader = new StreamReader(Request.Body, new System.Text.UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: false);
        var csv = await reader.ReadToEndAsync(ct);
        CatchTheSun.Core.Sun.Models.DirectSunImportResult parsed;
        try { parsed = parser.Parse(csv); }
        catch (FormatException ex) { return BadRequest(new { error = ex.Message }); }
        var entity = new DirectSunImport { Id = Guid.NewGuid(), VariantId = variantId, SourceCsv = csv, ImportedAt = DateTime.UtcNow };
        foreach (var x in parsed.Measurements)
            entity.Measurements.Add(new DirectSunMeasurementRecord {
                Id = Guid.NewGuid(), SensorId = x.SensorId, FeatureId = x.FeatureId, RecreationalType = x.RecreationalType,
                XMetres = x.XMetres, YMetres = x.YMetres, ZMetres = x.ZMetres, HeightAboveZoneMetres = x.HeightAboveZoneMetres,
                GridSpacingMetres = x.GridSpacingMetres, Longitude = x.Longitude, Latitude = x.Latitude,
                DirectSunHoursTestDay = x.DirectSunHoursTestDay, SunlitSamples = x.SunlitSamples,
                SampleCountDaylight = x.SampleCountDaylight, RawRow = x.RawRow
            });
        db.DirectSunImports.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new DirectSunImportResponse(entity.Id, parsed.ProcessedRecordCount, parsed.FeatureCount,
            parsed.MinimumDirectSunHours, parsed.MaximumDirectSunHours));
    }
}

public sealed record DirectSunImportResponse(Guid ImportId, int ProcessedRecordCount, int FeatureCount,
    double MinimumDirectSunHours, double MaximumDirectSunHours);
