using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api")]
public sealed class CriterionValuesController(CriterionValueService service) : ControllerBase
{
    [HttpGet("variants/{variantId:guid}/criteria-values")]
    public async Task<ActionResult<IReadOnlyList<CriterionValueDto>>> GetByVariant(Guid variantId, CancellationToken ct) => (await service.GetByVariantIdAsync(variantId, ct)) is { } x ? Ok(x) : NotFound();
    [HttpGet("criteria-values/{id:guid}")]
    public async Task<ActionResult<CriterionValueDto>> GetById(Guid id, CancellationToken ct) => (await service.GetByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();
    [HttpPost("variants/{variantId:guid}/criteria-values")]
    public async Task<ActionResult<CriterionValueDto>> Create(Guid variantId, CreateCriterionValueDto dto, CancellationToken ct)
    {
        try { var x = await service.CreateAsync(variantId, dto, ct); return x is null ? NotFound() : CreatedAtAction(nameof(GetById), new { id = x.Id }, x); }
        catch (ValidationException e) { return BadRequest(new ProblemDetails { Status = 400, Detail = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new ProblemDetails { Status = 409, Detail = e.Message }); }
    }
    [HttpPut("criteria-values/{id:guid}")]
    public async Task<ActionResult<CriterionValueDto>> Update(Guid id, UpdateCriterionValueDto dto, CancellationToken ct)
    {
        try { var x = await service.UpdateAsync(id, dto, ct); return x is null ? NotFound() : Ok(x); }
        catch (ValidationException e) { return BadRequest(new ProblemDetails { Status = 400, Detail = e.Message }); }
    }
    [HttpDelete("criteria-values/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
