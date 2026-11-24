using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.DTOs;

public sealed class CriterionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Unit { get; init; }
    public CriterionDirection Direction { get; init; }
    public decimal? TargetValue { get; init; }
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
