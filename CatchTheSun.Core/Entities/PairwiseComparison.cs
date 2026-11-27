namespace CatchTheSun.Core.Entities;

public sealed class PairwiseComparison
{
    public Guid Id { get; set; }
    public Guid ExpertResponseId { get; set; }
    public Guid QuestionnaireId { get; set; }
    public Guid FirstCriterionId { get; set; }
    public Guid SecondCriterionId { get; set; }
    public decimal Value { get; set; }
    public ExpertResponse ExpertResponse { get; set; } = null!;
}
