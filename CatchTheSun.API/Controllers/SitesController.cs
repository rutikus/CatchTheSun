using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.DTOs;
using CatchTheSun.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api/sites")]
public sealed class SitesController(SiteService siteService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SiteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> GetAll(CancellationToken cancellationToken)
    {
        var sites = await siteService.GetAllAsync(cancellationToken);
        return Ok(sites);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<SiteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var site = await siteService.GetByIdAsync(id, cancellationToken);
        return site is null ? NotFound() : Ok(site);
    }

    [HttpPost]
    [ProducesResponseType<SiteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SiteDto>> Create(CreateSiteDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var site = await siteService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = site.Id }, site);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<SiteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDto>> Update(Guid id, UpdateSiteDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var site = await siteService.UpdateAsync(id, dto, cancellationToken);
            return site is null ? NotFound() : Ok(site);
        }
        catch (ValidationException exception)
        {
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await siteService.DeleteAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ForeignKeyViolation
        })
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "The site cannot be deleted while it has calculation history or other dependent records."
            });
        }
    }
}
