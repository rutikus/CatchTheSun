using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.API.DTOs.Calculations;

public sealed class CreateCalculationRequest
{
    [Required]
    public Guid SiteId { get; init; }

    [Required, MinLength(2)]
    public List<Guid> VariantIds { get; init; } = [];

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CalculationMethod? Method { get; init; }

    [Required, MinLength(1)]
    public List<CreateCalculationCriterionRequest> Criteria { get; init; } = [];

    public List<CreateCalculationComparisonRequest>? Comparisons { get; init; } = [];

    public CreateCalculationCommand ToCommand()
    {
        if (Method is null)
            throw new InvalidOperationException("Calculation method is required.");
        if (VariantIds is null || Criteria is null)
            throw new InvalidOperationException("VariantIds and Criteria are required.");
        if (Criteria.Any(x => x is null))
            throw new InvalidOperationException("Criteria must not contain null items.");
        if (Comparisons?.Any(x => x is null) is true)
            throw new InvalidOperationException("Comparisons must not contain null items.");

        return new CreateCalculationCommand(
            SiteId,
            VariantIds,
            Method.Value,
            Criteria.Select(x => new CalculationCriterionSelection(x.CriterionId, x.Weight)).ToArray(),
            (Comparisons ?? []).Select(x => new CalculationCriterionComparisonInput(
                x.CriterionAId,
                x.CriterionBId,
                x.Value ?? throw new InvalidOperationException("Comparison value is required."))).ToArray());
    }
}

public sealed class CreateCalculationCriterionRequest
{
    [Required]
    public Guid CriterionId { get; init; }

    public decimal? Weight { get; init; }
}

public sealed class CreateCalculationComparisonRequest
{
    [Required]
    public Guid CriterionAId { get; init; }

    [Required]
    public Guid CriterionBId { get; init; }

    [Required]
    public decimal? Value { get; init; }
}
