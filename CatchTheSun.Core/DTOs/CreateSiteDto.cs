using System.ComponentModel.DataAnnotations;
using CatchTheSun.Core.Validation;

namespace CatchTheSun.Core.DTOs;

public sealed class CreateSiteDto
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

    [Required]
    [StringLength(500)]
    public string? Location { get; set; }

    [SiteArea]
    public decimal Area { get; set; }
}
