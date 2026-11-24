namespace CatchTheSun.Core.DTOs;

public sealed class SiteDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public decimal Area { get; init; }
    public DateTime CreatedAt { get; init; }
}
