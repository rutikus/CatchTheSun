namespace CatchTheSun.Core.Entities;

/// <summary>An anonymous participant profile. No direct personal identifiers are stored.</summary>
public sealed class Expert
{
    public Guid Id { get; set; }
    public string AnonymousCode { get; set; } = string.Empty;
    public string? ExperienceCategory { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ExpertResponse> Responses { get; set; } = new List<ExpertResponse>();
}
