namespace CatchTheSun.Core.DTOs;

public sealed class VariantDto
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ExternalId { get; init; }
    public DateTime CreatedAt { get; init; }
}
