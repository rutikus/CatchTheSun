namespace CatchTheSun.Core.Entities;

public sealed class QuestionnaireCriterion
{
    public Guid Id { get; set; }
    public Guid QuestionnaireId { get; set; }
    public Guid CriterionId { get; set; }
    public int DisplayOrder { get; set; }
    public Questionnaire Questionnaire { get; set; } = null!;
    public Criterion Criterion { get; set; } = null!;
}
