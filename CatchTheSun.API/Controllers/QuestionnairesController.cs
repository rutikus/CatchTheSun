using CatchTheSun.API.DTOs.Calculations;
using CatchTheSun.API.DTOs.Questionnaires;
using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using CatchTheSun.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CatchTheSun.API.Controllers;

[ApiController]
[Route("api")]
public sealed class QuestionnairesController(ApplicationDbContext db, ICalculationService calculations) : ControllerBase
{
    private const int MaximumCriteria = 20;
    private const int MinimumExperts = 5;

    [HttpPost("experts")]
    public async Task<ActionResult<ExpertDto>> CreateExpert(CreateExpertRequest request, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var expert = new Expert
        {
            Id = id, AnonymousCode = $"EXP-{Guid.NewGuid():N}"[..16],
            ExperienceCategory = string.IsNullOrWhiteSpace(request.ExperienceCategory) ? null : request.ExperienceCategory.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        db.Experts.Add(expert);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetExpert), new { id }, ToDto(expert));
    }

    [HttpGet("experts/{id:guid}")]
    public async Task<ActionResult<ExpertDto>> GetExpert(Guid id, CancellationToken ct)
    {
        var item = await db.Experts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return item is null ? NotFound() : Ok(ToDto(item));
    }

    [HttpPost("questionnaires")]
    public async Task<ActionResult<QuestionnaireDto>> CreateQuestionnaire(CreateQuestionnaireRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(Error("Название анкеты обязательно."));
        if (request.CriterionIds is null || request.CriterionIds.Any(x => x == Guid.Empty) ||
            request.CriterionIds.Distinct().Count() != request.CriterionIds.Count)
            return BadRequest(Error("Список CriterionId должен содержать непустые уникальные идентификаторы."));
        var criteria = await LoadActiveCriteria(request.CriterionIds, ct);
        if (criteria is null) return BadRequest(Error("Один или несколько критериев не существуют или неактивны."));
        var item = new Questionnaire
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Description = Clean(request.Description),
            Status = QuestionnaireStatus.Draft, CreatedAt = DateTime.UtcNow,
            Criteria = request.CriterionIds.Select((id, index) => new QuestionnaireCriterion
            { Id = Guid.NewGuid(), CriterionId = id, DisplayOrder = index }).ToList()
        };
        db.Questionnaires.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetQuestionnaire), new { id = item.Id }, await ProjectQuestionnaire(item.Id, ct));
    }

    [HttpGet("questionnaires")]
    public async Task<ActionResult<IReadOnlyList<QuestionnaireDto>>> GetQuestionnaires(CancellationToken ct)
    {
        var ids = await db.Questionnaires.AsNoTracking().OrderByDescending(x => x.CreatedAt).Select(x => x.Id).ToListAsync(ct);
        var results = new List<QuestionnaireDto>(ids.Count);
        foreach (var id in ids)
            if (await ProjectQuestionnaire(id, ct) is { } questionnaire) results.Add(questionnaire);
        return Ok(results);
    }

    [HttpGet("published-questionnaires")]
    public async Task<ActionResult<IReadOnlyList<QuestionnaireDto>>> GetPublishedQuestionnaires(CancellationToken ct)
    {
        var ids = await db.Questionnaires.AsNoTracking().Where(x => x.Status == QuestionnaireStatus.Published)
            .OrderByDescending(x => x.PublishedAt).Select(x => x.Id).ToListAsync(ct);
        var results = new List<QuestionnaireDto>(ids.Count);
        foreach (var id in ids)
            if (await ProjectQuestionnaire(id, ct) is { } questionnaire) results.Add(questionnaire);
        return Ok(results);
    }

    [HttpGet("published-questionnaires/{id:guid}")]
    public async Task<ActionResult<QuestionnaireDto>> GetPublishedQuestionnaire(Guid id, CancellationToken ct)
    {
        if (!await db.Questionnaires.AnyAsync(x => x.Id == id && x.Status == QuestionnaireStatus.Published, ct)) return NotFound();
        return Ok(await ProjectQuestionnaire(id, ct));
    }

    [HttpGet("questionnaires/{id:guid}")]
    public async Task<ActionResult<QuestionnaireDto>> GetQuestionnaire(Guid id, CancellationToken ct)
    {
        var result = await ProjectQuestionnaire(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("questionnaires/{id:guid}")]
    public async Task<ActionResult<QuestionnaireDto>> UpdateQuestionnaire(Guid id, UpdateQuestionnaireRequest request, CancellationToken ct)
    {
        var item = await db.Questionnaires.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Status != QuestionnaireStatus.Draft) return Conflict(Error("Изменять можно только черновик."));
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(Error("Название анкеты обязательно."));
        item.Name = request.Name.Trim(); item.Description = Clean(request.Description);
        await db.SaveChangesAsync(ct);
        return Ok(await ProjectQuestionnaire(id, ct));
    }

    [HttpPut("questionnaires/{id:guid}/criteria")]
    public async Task<ActionResult<QuestionnaireDto>> SetCriteria(Guid id, SetQuestionnaireCriteriaRequest request, CancellationToken ct)
    {
        var item = await db.Questionnaires.Include(x => x.Criteria).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Status != QuestionnaireStatus.Draft) return Conflict(Error("Состав критериев опубликованной анкеты заблокирован."));
        if (await db.ExpertResponses.AnyAsync(x => x.QuestionnaireId == id, ct)) return Conflict(Error("Нельзя менять критерии анкеты с ответами."));
        if (request.CriterionIds is null || request.CriterionIds.Any(x => x == Guid.Empty) || request.CriterionIds.Distinct().Count() != request.CriterionIds.Count)
            return BadRequest(Error("Список CriterionId должен содержать уникальные непустые идентификаторы."));
        var criteria = await LoadActiveCriteria(request.CriterionIds, ct);
        if (criteria is null) return BadRequest(Error("Один или несколько критериев не существуют или неактивны."));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        db.QuestionnaireCriteria.RemoveRange(item.Criteria);
        await db.SaveChangesAsync(ct);
        item.Criteria = request.CriterionIds.Select((criterionId, index) => new QuestionnaireCriterion
        { Id = Guid.NewGuid(), QuestionnaireId = id, CriterionId = criterionId, DisplayOrder = index }).ToList();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(await ProjectQuestionnaire(id, ct));
    }

    [HttpPost("questionnaires/{id:guid}/publish")]
    public async Task<ActionResult<QuestionnaireDto>> Publish(Guid id, CancellationToken ct)
    {
        var item = await db.Questionnaires.Include(x => x.Criteria).ThenInclude(x => x.Criterion).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Status != QuestionnaireStatus.Draft) return Conflict(Error("Опубликовать можно только черновик."));
        if (item.Criteria.Count is < 2 or > MaximumCriteria || item.Criteria.Any(x => !x.Criterion.IsActive))
            return BadRequest(Error($"Для публикации нужны от 2 до {MaximumCriteria} активных критериев."));
        item.Status = QuestionnaireStatus.Published; item.PublishedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(await ProjectQuestionnaire(id, ct));
    }

    [HttpPost("questionnaires/{id:guid}/close")]
    public async Task<ActionResult<QuestionnaireDto>> Close(Guid id, CancellationToken ct)
    {
        var item = await db.Questionnaires.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();
        if (item.Status != QuestionnaireStatus.Published) return Conflict(Error("Закрыть можно только опубликованную анкету."));
        item.Status = QuestionnaireStatus.Closed; item.ClosedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(await ProjectQuestionnaire(id, ct));
    }

    [HttpPost("questionnaires/{id:guid}/responses")]
    public async Task<ActionResult<ExpertResponseDto>> Submit(Guid id, SubmitExpertResponseRequest request, CancellationToken ct)
    {
        var questionnaire = await db.Questionnaires.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Status, CriterionIds = x.Criteria.OrderBy(c => c.DisplayOrder).Select(c => c.CriterionId).ToList() })
            .SingleOrDefaultAsync(ct);
        if (questionnaire is null) return NotFound();
        if (questionnaire.Status != QuestionnaireStatus.Published) return Conflict(Error("Ответы принимаются только для опубликованной анкеты."));
        if (!await db.Experts.AnyAsync(x => x.Id == request.ExpertId, ct)) return NotFound(Error("Профиль эксперта не найден."));
        if (await db.ExpertResponses.AnyAsync(x => x.QuestionnaireId == id && x.ExpertId == request.ExpertId, ct))
            return Conflict(Error("Этот эксперт уже отправил ответ на анкету."));

        var criterionIds = questionnaire.CriterionIds;
        if (criterionIds.Count is < 2 or > MaximumCriteria) return Conflict(Error("Анкета не содержит допустимого набора критериев."));
        var expectedPairs = criterionIds.Count * (criterionIds.Count - 1) / 2;
        if (request.Comparisons is null || request.Comparisons.Count != expectedPairs)
            return BadRequest(Error($"Нужно заполнить все {expectedPairs} попарных сравнений."));
        if (request.Comparisons.Any(x => x is null) || request.Rankings is null || request.Rankings.Any(x => x is null) ||
            request.Rankings.Count != criterionIds.Count ||
            request.Rankings.Select(x => x.CriterionId).Distinct().Count() != criterionIds.Count ||
            request.Rankings.Any(x => !criterionIds.Contains(x.CriterionId) || x.Rank < 1 || x.Rank > criterionIds.Count))
            return BadRequest(Error("Нужно указать ранг от 1 до числа критериев для каждого критерия; одинаковые ранги допускаются."));

        var normalizedComparisons = new List<(Guid First, Guid Second, decimal Value)>(expectedPairs);
        var seen = new HashSet<(Guid, Guid)>();
        try
        {
            foreach (var entry in request.Comparisons)
            {
                if (entry.Value is null) return BadRequest(Error("Значение попарного сравнения обязательно."));
                var normalized = ExpertQuestionnaireAggregation.Canonicalize(entry.FirstCriterionId, entry.SecondCriterionId, entry.Value.Value);
                if (!criterionIds.Contains(normalized.First) || !criterionIds.Contains(normalized.Second) || !seen.Add((normalized.First, normalized.Second)))
                    return BadRequest(Error("Каждая пара критериев должна входить в анкету и встречаться ровно один раз."));
                normalizedComparisons.Add(normalized);
            }
        }
        catch (ArgumentException e) { return BadRequest(Error(e.Message)); }
        if (seen.Count != expectedPairs) return BadRequest(Error("Анкета содержит неполную матрицу попарных сравнений."));

        var response = new ExpertResponse
        {
            Id = Guid.NewGuid(), QuestionnaireId = id, ExpertId = request.ExpertId,
            SubmittedAt = DateTime.UtcNow, Status = ExpertResponseStatus.Submitted,
            PairwiseComparisons = normalizedComparisons.Select(x => new PairwiseComparison
            { Id = Guid.NewGuid(), QuestionnaireId = id, FirstCriterionId = x.First, SecondCriterionId = x.Second, Value = x.Value }).ToList(),
            Rankings = request.Rankings.Select(x => new CriterionRanking
            { Id = Guid.NewGuid(), QuestionnaireId = id, CriterionId = x.CriterionId, Rank = x.Rank }).ToList()
        };
        db.ExpertResponses.Add(response);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "questionnaire.response_conflict",
                Detail = "Ответ не сохранён: эксперт уже отправил анкету или данные нарушают ограничения."
            });
        }
        return CreatedAtAction(nameof(GetResponse), new { id = response.Id }, ToDto(response));
    }

    [HttpGet("questionnaire-responses/{id:guid}")]
    public async Task<ActionResult<ExpertResponseDto>> GetResponse(Guid id, CancellationToken ct)
    {
        var response = await db.ExpertResponses.AsNoTracking().Include(x => x.PairwiseComparisons).Include(x => x.Rankings)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        return response is null ? NotFound() : Ok(ToDto(response));
    }

    [HttpGet("questionnaires/{id:guid}/aggregate")]
    public async Task<ActionResult<QuestionnaireAggregateDto>> Aggregate(Guid id, CancellationToken ct)
    {
        var item = await db.Questionnaires.AsNoTracking().Where(x => x.Id == id && x.Status != QuestionnaireStatus.Draft)
            .Select(x => new { x.Id, Criteria = x.Criteria.OrderBy(c => c.DisplayOrder).Select(c => c.CriterionId).ToList() })
            .SingleOrDefaultAsync(ct);
        if (item is null) return NotFound();
        var responses = await db.ExpertResponses.AsNoTracking().Where(x => x.QuestionnaireId == id && x.Status == ExpertResponseStatus.Submitted)
            .Include(x => x.PairwiseComparisons).Include(x => x.Rankings).ToListAsync(ct);
        if (responses.Count < MinimumExperts) return Conflict(Error($"Для агрегации требуется не менее {MinimumExperts} ответов."));
        try
        {
            var aggregate = ExpertQuestionnaireAggregation.Calculate(item.Criteria, responses.Select(x => new ExpertJudgement(
                x.PairwiseComparisons.Select(p => new ExpertPairwiseValue(p.FirstCriterionId, p.SecondCriterionId, p.Value)).ToArray(),
                x.Rankings.Select(r => new ExpertRankValue(r.CriterionId, r.Rank)).ToArray())).ToArray());
            return Ok(new QuestionnaireAggregateDto(id, aggregate.ExpertCount, aggregate.ConsistencyRatio, aggregate.KendallW,
                aggregate.Comparisons.Select(x => new AggregatedComparisonDto(x.FirstCriterionId, x.SecondCriterionId, x.Value)).ToArray(),
                aggregate.Weights.Select(x => new AggregatedWeightDto(x.CriterionId, x.Weight)).ToArray()));
        }
        catch (ArgumentException e) { return Conflict(Error($"Ответы анкеты некорректны для агрегации: {e.Message}")); }
        catch (InvalidOperationException e) { return Conflict(Error(e.Message)); }
    }

    [HttpPost("questionnaires/{id:guid}/calculations")]
    public async Task<ActionResult<CalculationResponseDto>> CreateCalculation(Guid id, QuestionnaireCalculationRequest request, CancellationToken ct)
    {
        if (request.Method is not (CalculationMethod.TOPSIS or CalculationMethod.Pareto))
            return BadRequest(Error("Анкетные AHP-веса можно напрямую применять к методам TOPSIS или Pareto."));
        (List<Guid> CriterionIds, ExpertQuestionnaireAggregate Aggregate)? aggregateResult;
        try { aggregateResult = await AggregateForCalculation(id, ct); }
        catch (InvalidOperationException e) { return Conflict(Error(e.Message)); }
        catch (ArgumentException e) { return Conflict(Error(e.Message)); }
        if (aggregateResult is null) return NotFound();
        var (criteriaIds, aggregate) = aggregateResult.Value;
        if (aggregate.ExpertCount < MinimumExperts) return Conflict(Error($"Для расчёта требуется не менее {MinimumExperts} ответов."));
        var command = new CreateCalculationCommand(request.SiteId, request.VariantIds, request.Method.Value,
            aggregate.Weights.Select(x => new CalculationCriterionSelection(x.CriterionId, x.Weight)).ToArray(), []);
        try
        {
            var result = await calculations.CreateAsync(command, ct);
            return CreatedAtAction("GetById", "Calculations", new { id = result.Id }, CalculationResponseDto.From(result));
        }
        catch (CalculationServiceException e)
        {
            var status = e.Kind switch
            {
                CalculationServiceErrorKind.InvalidRequest => 400,
                CalculationServiceErrorKind.NotFound => 404,
                CalculationServiceErrorKind.Conflict => 409,
                CalculationServiceErrorKind.Validation => 422,
                _ => 500
            };
            return StatusCode(status, new ProblemDetails { Status = status, Title = e.Code, Detail = e.Message });
        }
    }

    private async Task<(List<Guid> CriterionIds, ExpertQuestionnaireAggregate Aggregate)?> AggregateForCalculation(Guid id, CancellationToken ct)
    {
        var q = await db.Questionnaires.AsNoTracking().Where(x => x.Id == id && x.Status != QuestionnaireStatus.Draft)
            .Select(x => new { Criteria = x.Criteria.OrderBy(c => c.DisplayOrder).Select(c => c.CriterionId).ToList() }).SingleOrDefaultAsync(ct);
        if (q is null) return null;
        var responses = await db.ExpertResponses.AsNoTracking().Where(x => x.QuestionnaireId == id)
            .Include(x => x.PairwiseComparisons).Include(x => x.Rankings).ToListAsync(ct);
        if (responses.Count < MinimumExperts) throw new InvalidOperationException($"Для расчёта требуется не менее {MinimumExperts} ответов.");
        var aggregate = ExpertQuestionnaireAggregation.Calculate(q.Criteria, responses.Select(x => new ExpertJudgement(
            x.PairwiseComparisons.Select(p => new ExpertPairwiseValue(p.FirstCriterionId, p.SecondCriterionId, p.Value)).ToArray(),
            x.Rankings.Select(r => new ExpertRankValue(r.CriterionId, r.Rank)).ToArray())).ToArray());
        return (q.Criteria, aggregate);
    }

    private async Task<List<Criterion>?> LoadActiveCriteria(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var criteria = await db.Criteria.Where(x => ids.Contains(x.Id) && x.IsActive).ToListAsync(ct);
        return criteria.Count == ids.Count ? criteria : null;
    }

    private async Task<QuestionnaireDto?> ProjectQuestionnaire(Guid id, CancellationToken ct)
    {
        return await db.Questionnaires.AsNoTracking().Where(x => x.Id == id).Select(x => new QuestionnaireDto(
            x.Id, x.Name, x.Description, x.Status, x.CreatedAt, x.PublishedAt, x.ClosedAt,
            x.Criteria.OrderBy(c => c.DisplayOrder).Select(c => new QuestionnaireCriterionDto(c.CriterionId, c.Criterion.Name,
                c.Criterion.Description, c.Criterion.Unit, c.Criterion.Direction, c.Criterion.TargetValue, c.DisplayOrder)).ToArray(),
            x.Responses.Count)).SingleOrDefaultAsync(ct);
    }

    private static ExpertDto ToDto(Expert e) => new(e.Id, e.AnonymousCode, e.ExperienceCategory, e.CreatedAt);
    private static ExpertResponseDto ToDto(ExpertResponse r) => new(r.Id, r.QuestionnaireId, r.ExpertId, r.SubmittedAt, r.Status,
        r.PairwiseComparisons.OrderBy(x => x.FirstCriterionId).ThenBy(x => x.SecondCriterionId)
            .Select(x => new SubmittedComparisonDto(x.FirstCriterionId, x.SecondCriterionId, x.Value)).ToArray(),
        r.Rankings.Select(x => new SubmittedRankingDto(x.CriterionId, x.Rank)).ToArray());
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ProblemDetails Error(string message) => new() { Status = 400, Title = "questionnaire.request_invalid", Detail = message };
}
