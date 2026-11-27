using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Services;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.API.Controllers;

[ApiController]
public sealed class InsolationComparisonsController(ApplicationDbContext db) : ControllerBase
{
    [HttpPost("api/variants/{variantId:guid}/insolation/comparisons")]
    public async Task<ActionResult<ComparisonDto>> Create(Guid variantId, CreateComparisonRequest request, CancellationToken ct)
    {
        if (variantId == Guid.Empty || request.ReferenceImportId == Guid.Empty || request.TestImportId == Guid.Empty ||
            !ValidSource(request.ReferenceSource) || !ValidSource(request.TestSource))
            return BadRequest(new { error = "Укажите допустимые источники (ladybug или urbanSketch) и идентификаторы импортов." });
        if (!await db.Variants.AnyAsync(x => x.Id == variantId, ct)) return NotFound(new { error = "Вариант не найден." });
        var reference = await Load(request.ReferenceSource, request.ReferenceImportId, ct);
        var test = await Load(request.TestSource, request.TestImportId, ct);
        if (reference is null || test is null) return NotFound(new { error = "Один или оба импорта не найдены." });
        if (reference.Value.VariantId != variantId || test.Value.VariantId != variantId)
            return BadRequest(new { error = "Оба импорта должны принадлежать указанному варианту." });

        var referenceBySensor = reference.Value.Points.ToDictionary(x => x.SensorId, StringComparer.Ordinal);
        var testBySensor = test.Value.Points.ToDictionary(x => x.SensorId, StringComparer.Ordinal);
        var matched = referenceBySensor.Keys.Intersect(testBySensor.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var points = matched.Select(sensor =>
        {
            var x = referenceBySensor[sensor]; var y = testBySensor[sensor];
            var errors = InsolationComparisonMath.Errors(x.Value, y.Value);
            return new InsolationComparisonPoint { Id = Guid.NewGuid(), SensorId = sensor, ReferenceValue = x.Value,
                TestValue = y.Value, AbsoluteError = errors.Absolute, RelativeError = errors.Relative };
        }).ToList();
        var aggregate = InsolationComparisonMath.Aggregate(points.Select(x => (x.AbsoluteError, x.RelativeError)).ToArray());
        var entity = new InsolationComparison
        {
            Id = Guid.NewGuid(), VariantId = variantId, CreatedAt = DateTime.UtcNow,
            ReferenceDirectSunImportId = request.ReferenceSource == "ladybug" ? request.ReferenceImportId : null,
            ReferenceUrbanSketchImportId = request.ReferenceSource == "urbanSketch" ? request.ReferenceImportId : null,
            TestDirectSunImportId = request.TestSource == "ladybug" ? request.TestImportId : null,
            TestUrbanSketchImportId = request.TestSource == "urbanSketch" ? request.TestImportId : null,
            Mae = aggregate.Mae, Rmse = aggregate.Rmse, Mape = aggregate.Mape,
            ComparedPoints = points.Count, ReferencePoints = reference.Value.Points.Count,
            TestPoints = test.Value.Points.Count, UnmatchedReferencePoints = reference.Value.Points.Count - points.Count,
            UnmatchedTestPoints = test.Value.Points.Count - points.Count, Points = points
        };
        db.InsolationComparisons.Add(entity);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ToDto(entity));
    }

    [HttpGet("api/variants/{variantId:guid}/insolation/comparisons")]
    public async Task<ActionResult<IReadOnlyList<ComparisonDto>>> GetForVariant(Guid variantId, CancellationToken ct)
    {
        if (!await db.Variants.AnyAsync(x => x.Id == variantId, ct)) return NotFound(new { error = "Вариант не найден." });
        var items = await db.InsolationComparisons.AsNoTracking().Where(x => x.VariantId == variantId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Ok(items.Select(ToDto).ToArray());
    }

    [HttpGet("api/insolation/comparisons/{id:guid}")]
    public async Task<ActionResult<ComparisonDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await db.InsolationComparisons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound(new { error = "Сравнение не найдено." }) : Ok(ToDto(item));
    }

    [HttpGet("api/insolation/comparisons/{id:guid}/points")]
    public async Task<ActionResult<ComparisonPointPage>> GetPoints(Guid id, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 500) return BadRequest(new { error = "page должен быть положительным, pageSize — от 1 до 500." });
        var query = db.InsolationComparisonPoints.AsNoTracking().Where(x => x.InsolationComparisonId == id).OrderBy(x => x.SensorId);
        if (!await db.InsolationComparisons.AnyAsync(x => x.Id == id, ct)) return NotFound(new { error = "Сравнение не найдено." });
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ComparisonPointDto(x.SensorId, x.ReferenceValue, x.TestValue, x.AbsoluteError, x.RelativeError)).ToListAsync(ct);
        return Ok(new ComparisonPointPage(page, pageSize, total, rows));
    }

    private async Task<(Guid VariantId, List<(string SensorId, double Value)> Points)?> Load(string source, Guid id, CancellationToken ct)
    {
        if (source == "ladybug")
        {
            var item = await db.DirectSunImports.AsNoTracking().Where(x => x.Id == id)
                .Select(x => new { x.VariantId, Points = x.Measurements.Select(m => new { m.SensorId, m.DirectSunHoursTestDay }).ToList() }).SingleOrDefaultAsync(ct);
            return item is null ? null : (item.VariantId, item.Points.Select(x => (x.SensorId, x.DirectSunHoursTestDay)).ToList());
        }
        var urban = await db.UrbanSketchImports.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.VariantId, Points = x.Measurements.Select(m => new { m.SensorId, m.DirectSunHoursTestDay }).ToList() }).SingleOrDefaultAsync(ct);
        return urban is null ? null : (urban.VariantId, urban.Points.Select(x => (x.SensorId, x.DirectSunHoursTestDay)).ToList());
    }

    private static bool ValidSource(string source) => source is "ladybug" or "urbanSketch";
    private static ComparisonDto ToDto(InsolationComparison x) => new(x.Id, x.VariantId,
        x.ReferenceDirectSunImportId.HasValue ? "ladybug" : "urbanSketch", x.ReferenceDirectSunImportId ?? x.ReferenceUrbanSketchImportId!.Value,
        x.TestDirectSunImportId.HasValue ? "ladybug" : "urbanSketch", x.TestDirectSunImportId ?? x.TestUrbanSketchImportId!.Value,
        x.Mae, x.Rmse, x.Mape, x.ComparedPoints, x.ReferencePoints, x.TestPoints,
        x.UnmatchedReferencePoints, x.UnmatchedTestPoints, x.CreatedAt);
}

public sealed record CreateComparisonRequest(string ReferenceSource, Guid ReferenceImportId, string TestSource, Guid TestImportId);
public sealed record ComparisonDto(Guid Id, Guid VariantId, string ReferenceSource, Guid ReferenceImportId,
    string TestSource, Guid TestImportId, double? Mae, double? Rmse, double? Mape, int ComparedPoints,
    int ReferencePoints, int TestPoints, int UnmatchedReferencePoints, int UnmatchedTestPoints, DateTime CreatedAt);
public sealed record ComparisonPointDto(string SensorId, double ReferenceValue, double TestValue, double AbsoluteError, double? RelativeError);
public sealed record ComparisonPointPage(int Page, int PageSize, int TotalCount, IReadOnlyList<ComparisonPointDto> Items);
