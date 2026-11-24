using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Validation;

namespace CatchTheSun.Core.DTOs;

public sealed class UpdateCriterionDto
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }
    [StringLength(2000)]
    public string? Description { get; set; }
    [StringLength(50)]
    public string? Unit { get; set; }
    public CriterionDirection Direction { get; set; }
    [CriterionTargetValue]
    public decimal? TargetValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
}
