namespace CatchTheSun.Core.Entities;

public sealed class CriterionRanking
{
    public Guid Id { get; set; }
    public Guid ExpertResponseId { get; set; }
    public Guid QuestionnaireId { get; set; }
    public Guid CriterionId { get; set; }
    public int Rank { get; set; }
    public ExpertResponse ExpertResponse { get; set; } = null!;
}
