using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.API.DTOs.Questionnaires;

public sealed class CreateExpertRequest
{
    [StringLength(100)] public string? ExperienceCategory { get; init; }
}

public sealed class CreateQuestionnaireRequest
{
    [Required, StringLength(200, MinimumLength = 1)] public string Name { get; init; } = string.Empty;
    [StringLength(4000)] public string? Description { get; init; }
    public List<Guid> CriterionIds { get; init; } = [];
}

public sealed class UpdateQuestionnaireRequest
{
    [Required, StringLength(200, MinimumLength = 1)] public string Name { get; init; } = string.Empty;
    [StringLength(4000)] public string? Description { get; init; }
}

public sealed class SetQuestionnaireCriteriaRequest
{
    [Required] public List<Guid> CriterionIds { get; init; } = [];
}

public sealed class SubmitExpertResponseRequest
{
    [Required] public Guid ExpertId { get; init; }
    [Required] public List<SubmitPairwiseComparisonRequest> Comparisons { get; init; } = [];
    [Required] public List<SubmitCriterionRankingRequest> Rankings { get; init; } = [];
}

public sealed class SubmitPairwiseComparisonRequest
{
    [Required] public Guid FirstCriterionId { get; init; }
    [Required] public Guid SecondCriterionId { get; init; }
    [Required] public decimal? Value { get; init; }
}

public sealed class SubmitCriterionRankingRequest
{
    [Required] public Guid CriterionId { get; init; }
    [Range(1, 20)] public int Rank { get; init; }
}

public sealed class QuestionnaireCalculationRequest
{
    [Required] public Guid SiteId { get; init; }
    [Required, MinLength(2)] public List<Guid> VariantIds { get; init; } = [];
    [Required, JsonConverter(typeof(JsonStringEnumConverter))] public CalculationMethod? Method { get; init; }
}

public sealed record ExpertDto(Guid Id, string AnonymousCode, string? ExperienceCategory, DateTime CreatedAt);
public sealed record QuestionnaireCriterionDto(Guid CriterionId, string Name, string? Description, string? Unit,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] CriterionDirection Direction, decimal? TargetValue, int DisplayOrder);
public sealed record QuestionnaireDto(Guid Id, string Name, string? Description,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] QuestionnaireStatus Status,
    DateTime CreatedAt, DateTime? PublishedAt, DateTime? ClosedAt, IReadOnlyList<QuestionnaireCriterionDto> Criteria,
    int ResponseCount);
public sealed record SubmittedComparisonDto(Guid FirstCriterionId, Guid SecondCriterionId, decimal Value);
public sealed record SubmittedRankingDto(Guid CriterionId, int Rank);
public sealed record ExpertResponseDto(Guid Id, Guid QuestionnaireId, Guid ExpertId, DateTime SubmittedAt,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExpertResponseStatus Status, IReadOnlyList<SubmittedComparisonDto> Comparisons,
    IReadOnlyList<SubmittedRankingDto> Rankings);
public sealed record QuestionnaireAggregateDto(Guid QuestionnaireId, int ExpertCount, double ConsistencyRatio,
    double? KendallW, IReadOnlyList<AggregatedComparisonDto> Comparisons, IReadOnlyList<AggregatedWeightDto> Weights);
public sealed record AggregatedComparisonDto(Guid FirstCriterionId, Guid SecondCriterionId, decimal Value);
public sealed record AggregatedWeightDto(Guid CriterionId, decimal Weight);
