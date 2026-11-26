using CatchTheSun.Core.Entities;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.API.Controllers;

[ApiController]
public sealed class LadybugInsolationController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("api/variants/{variantId:guid}/insolation/ladybug")]
    public async Task<ActionResult<IReadOnlyList<LadybugImportDto>>> GetForVariant(Guid variantId, CancellationToken ct)
    {
        if (!await db.Variants.AnyAsync(x => x.Id == variantId, ct)) return NotFound();
        var imports = await db.DirectSunImports.AsNoTracking().Where(x => x.VariantId == variantId)
            .OrderByDescending(x => x.ImportedAt).Select(x => new LadybugImportDto(
                x.Id, x.VariantId, x.ImportedAt, x.Measurements.Count,
                x.Measurements.Select(m => m.FeatureId).Distinct().Count(),
                x.Measurements.Min(m => (double?)m.DirectSunHoursTestDay),
                x.Measurements.Max(m => (double?)m.DirectSunHoursTestDay)))
            .ToListAsync(ct);
        return Ok(imports);
    }

    [HttpGet("api/insolation/ladybug/{id:guid}")]
    public async Task<ActionResult<LadybugImportDetailsDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.DirectSunImports.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new LadybugImportDetailsDto(
                x.Id, x.VariantId, x.ImportedAt, x.Measurements.Count,
                x.Measurements.Select(m => m.FeatureId).Distinct().Count(),
                x.Measurements.Select(m => (double?)m.DirectSunHoursTestDay).Min(),
                x.Measurements.Select(m => (double?)m.DirectSunHoursTestDay).Max(), x.SourceCsv))
            .SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        return Ok(item);
    }

    [HttpGet("api/insolation/ladybug/{id:guid}/measurements")]
    public async Task<ActionResult<LadybugMeasurementPage>> GetMeasurements(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 500)
            return BadRequest(new { error = "page must be positive and pageSize must be between 1 and 500." });
        var importExists = await db.DirectSunImports.AnyAsync(x => x.Id == id, ct);
        if (!importExists) return NotFound();
        var query = db.DirectSunMeasurements.AsNoTracking().Where(x => x.ImportId == id).OrderBy(x => x.SensorId);
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new LadybugMeasurementDto(x.Id, x.ImportId, x.SensorId, x.FeatureId,
                x.RecreationalType, x.XMetres, x.YMetres, x.ZMetres, x.HeightAboveZoneMetres,
                x.GridSpacingMetres, x.Longitude, x.Latitude, x.DirectSunHoursTestDay,
                x.SunlitSamples, x.SampleCountDaylight, x.RawRow))
            .ToListAsync(ct);
        return Ok(new LadybugMeasurementPage(page, pageSize, total, rows));
    }
}

public sealed record LadybugImportDto(Guid Id, Guid VariantId, DateTime ImportedAt, int MeasurementCount,
    int FeatureCount, double? MinimumDirectSunHoursTestDay, double? MaximumDirectSunHoursTestDay);

public sealed record LadybugImportDetailsDto(Guid Id, Guid VariantId, DateTime ImportedAt, int MeasurementCount,
    int FeatureCount, double? MinimumDirectSunHoursTestDay, double? MaximumDirectSunHoursTestDay, string SourceCsv);

public sealed record LadybugMeasurementPage(int Page, int PageSize, int TotalCount,
    IReadOnlyList<LadybugMeasurementDto> Items);

public sealed record LadybugMeasurementDto(Guid Id, Guid ImportId, string SensorId, Guid FeatureId,
    string RecreationalType, double XMetres, double YMetres, double ZMetres, double HeightAboveZoneMetres,
    double GridSpacingMetres, double Longitude, double Latitude, double DirectSunHoursTestDay,
    int SunlitSamples, int SampleCountDaylight, string RawRow);
