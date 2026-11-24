namespace CatchTheSun.Core.DTOs;

public sealed class CriterionValueDto
{
    public Guid Id { get; init; }
    public Guid VariantId { get; init; }
    public Guid CriterionId { get; init; }
    public decimal Value { get; init; }
}
