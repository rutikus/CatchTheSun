using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Exceptions;
using CatchTheSun.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api")]
public sealed class VariantsController(VariantService variantService) : ControllerBase
{
    [HttpGet("sites/{siteId:guid}/variants")]
    [ProducesResponseType<IReadOnlyList<VariantDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<VariantDto>>> GetBySiteId(Guid siteId, CancellationToken cancellationToken)
    {
        var variants = await variantService.GetBySiteIdAsync(siteId, cancellationToken);
        return variants is null ? NotFound() : Ok(variants);
    }

    [HttpGet("variants/{id:guid}")]
    [ProducesResponseType<VariantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var variant = await variantService.GetByIdAsync(id, cancellationToken);
        return variant is null ? NotFound() : Ok(variant);
    }

    [HttpPost("sites/{siteId:guid}/variants")]
    [ProducesResponseType<VariantDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDto>> Create(Guid siteId, CreateVariantDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var variant = await variantService.CreateAsync(siteId, dto, cancellationToken);
            return variant is null
                ? NotFound()
                : CreatedAtAction(nameof(GetById), new { id = variant.Id }, variant);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = exception.Message });
        }
    }

    [HttpPut("variants/{id:guid}")]
    [ProducesResponseType<VariantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantDto>> Update(Guid id, UpdateVariantDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var variant = await variantService.UpdateAsync(id, dto, cancellationToken);
            return variant is null ? NotFound() : Ok(variant);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = exception.Message });
        }
    }

    [HttpDelete("variants/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await variantService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (VariantHasDependenciesException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Detail = "The variant cannot be deleted because it is referenced by existing calculation results."
            });
        }
    }
}
