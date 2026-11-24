using CatchTheSun.Core.Validation;

namespace CatchTheSun.Core.DTOs;

public sealed class UpdateCriterionValueDto
{
    [CriterionNumericValue]
    public decimal Value { get; set; }
}
