using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.Validation;

namespace CatchTheSun.Core.DTOs;

public sealed class CreateCriterionValueDto
{
    [Required]
    public Guid CriterionId { get; set; }
    [CriterionNumericValue]
    public decimal Value { get; set; }
}
