using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api/criteria")]
public sealed class CriteriaController(CriterionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CriterionDto>>> GetAll(CancellationToken ct) => Ok(await service.GetAllAsync(ct));
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CriterionDto>> GetById(Guid id, CancellationToken ct) => (await service.GetByIdAsync(id, ct)) is { } x ? Ok(x) : NotFound();
    [HttpPost]
    public async Task<ActionResult<CriterionDto>> Create(CreateCriterionDto dto, CancellationToken ct)
    {
        try { var result = await service.CreateAsync(dto, ct); return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); }
        catch (ValidationException e) { return BadRequest(new ProblemDetails { Status = 400, Detail = e.Message }); }
        catch (DuplicateCriterionNameException e) { return Conflict(new ProblemDetails { Status = 409, Detail = e.Message }); }
    }
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CriterionDto>> Update(Guid id, UpdateCriterionDto dto, CancellationToken ct)
    {
        try { var result = await service.UpdateAsync(id, dto, ct); return result is null ? NotFound() : Ok(result); }
        catch (ValidationException e) { return BadRequest(new ProblemDetails { Status = 400, Detail = e.Message }); }
        catch (DuplicateCriterionNameException e) { return Conflict(new ProblemDetails { Status = 409, Detail = e.Message }); }
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try { return await service.DeleteAsync(id, ct) ? NoContent() : NotFound(); }
        catch (CriterionHasDependenciesException e) { return Conflict(new ProblemDetails { Status = 409, Detail = e.Message }); }
    }
}
