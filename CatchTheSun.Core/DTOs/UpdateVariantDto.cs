using System.ComponentModel.DataAnnotations;

namespace CatchTheSun.Core.DTOs;

public sealed class UpdateVariantDto
{
    [Required]
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(200)]
    public string? ExternalId { get; set; }
}
