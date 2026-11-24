using CatchTheSun.API.DTOs.Calculations;
using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api")]
public sealed class CalculationsController(ICalculationService calculationService) : ControllerBase
{
    [HttpPost("calculations")]
    [ProducesResponseType<CalculationResponseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CalculationResponseDto>> Create(
        CreateCalculationRequest request,
        CancellationToken cancellationToken)
    {
        CreateCalculationCommand command;
        try
        {
            command = request.ToCommand();
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "calculation.request_invalid",
                Detail = exception.Message
            });
        }

        try
        {
            var details = await calculationService.CreateAsync(command, cancellationToken);
            var response = CalculationResponseDto.From(details);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (CalculationServiceException exception)
        {
            return ToProblem(exception);
        }
    }

    [HttpGet("calculations/{id:guid}")]
    [ProducesResponseType<CalculationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalculationResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var details = await calculationService.GetByIdAsync(id, cancellationToken);
            return details is null ? NotFound() : Ok(CalculationResponseDto.From(details));
        }
        catch (CalculationServiceException exception)
        {
            return ToProblem(exception);
        }
    }

    [HttpGet("sites/{siteId:guid}/calculations")]
    [ProducesResponseType<IReadOnlyList<CalculationHistoryResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CalculationHistoryResponseDto>>> GetBySiteId(
        Guid siteId,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await calculationService.GetBySiteIdAsync(siteId, cancellationToken);
            return history is null
                ? NotFound()
                : Ok(history.Select(CalculationHistoryResponseDto.From).ToArray());
        }
        catch (CalculationServiceException exception)
        {
            return ToProblem(exception);
        }
    }

    [HttpGet("calculations/{id:guid}/results")]
    [ProducesResponseType<IReadOnlyList<CalculationResultResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CalculationResultResponseDto>>> GetResults(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var results = await calculationService.GetResultsAsync(id, cancellationToken);
            return results is null
                ? NotFound()
                : Ok(results.Select(CalculationResultResponseDto.From).ToArray());
        }
        catch (CalculationServiceException exception)
        {
            return ToProblem(exception);
        }
    }

    private ObjectResult ToProblem(CalculationServiceException exception)
    {
        var status = exception.Kind switch
        {
            CalculationServiceErrorKind.InvalidRequest => StatusCodes.Status400BadRequest,
            CalculationServiceErrorKind.NotFound => StatusCodes.Status404NotFound,
            CalculationServiceErrorKind.Conflict => StatusCodes.Status409Conflict,
            CalculationServiceErrorKind.Validation => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(status, new ProblemDetails
        {
            Status = status,
            Title = exception.Code,
            Detail = exception.Message
        });
    }
}
