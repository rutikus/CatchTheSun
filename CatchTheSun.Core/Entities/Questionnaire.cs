namespace CatchTheSun.Core.Entities;

public sealed class Questionnaire
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public QuestionnaireStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public ICollection<QuestionnaireCriterion> Criteria { get; set; } = new List<QuestionnaireCriterion>();
    public ICollection<ExpertResponse> Responses { get; set; } = new List<ExpertResponse>();
}
