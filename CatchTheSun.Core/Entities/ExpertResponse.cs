namespace CatchTheSun.Core.Entities;

public sealed class ExpertResponse
{
    public Guid Id { get; set; }
    public Guid QuestionnaireId { get; set; }
    public Guid ExpertId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ExpertResponseStatus Status { get; set; }
    public Questionnaire Questionnaire { get; set; } = null!;
    public Expert Expert { get; set; } = null!;
    public ICollection<PairwiseComparison> PairwiseComparisons { get; set; } = new List<PairwiseComparison>();
    public ICollection<CriterionRanking> Rankings { get; set; } = new List<CriterionRanking>();
}
