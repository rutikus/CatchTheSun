using CatchTheSun.API.Controllers;
using CatchTheSun.API.DTOs.Questionnaires;
using CatchTheSun.Core.Entities;
using CatchTheSun.Data;
using CatchTheSun.Tests.Integration.PostgreSql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CatchTheSun.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class QuestionnairesApiTests(PostgreSqlFixture fixture)
{
    [PostgreSqlFact]
    public async Task PublishedQuestionnaireAcceptsFiveCompleteAnonymousResponsesAndAggregatesTiedRanks()
    {
        await using var db = fixture.CreateDbContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ids = new[]
        {
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Guid.Parse("00000000-0000-0000-0000-000000000003")
        };
        db.Criteria.AddRange(ids.Select((id, i) => new Criterion
        {
            Id = id, Name = $"Criterion {i + 1}", Unit = "points", Direction = CriterionDirection.Maximize,
            IsActive = true, IsRequired = false, CreatedAt = DateTime.UtcNow
        }));
        await db.SaveChangesAsync();

        var controller = new QuestionnairesController(db, null!);
        var createResult = await controller.CreateQuestionnaire(new CreateQuestionnaireRequest
        { Name = "Expert survey", Description = "Anonymous", CriterionIds = ids.ToList() }, default);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var questionnaire = Assert.IsType<QuestionnaireDto>(created.Value);
        Assert.Equal(QuestionnaireStatus.Draft, questionnaire.Status);

        var publishResult = await controller.Publish(questionnaire.Id, default);
        Assert.IsType<OkObjectResult>(publishResult.Result);
        var experts = new List<ExpertDto>();
        for (var i = 0; i < 5; i++)
        {
            var expertResult = await controller.CreateExpert(new CreateExpertRequest { ExperienceCategory = "experienced" }, default);
            experts.Add(Assert.IsType<ExpertDto>(Assert.IsType<CreatedAtActionResult>(expertResult.Result).Value));
        }
        Assert.All(experts, e => Assert.StartsWith("EXP-", e.AnonymousCode));

        var responseRequest = new SubmitExpertResponseRequest
        {
            ExpertId = experts[0].Id,
            Comparisons =
            [
                new() { FirstCriterionId = ids[0], SecondCriterionId = ids[1], Value = 2m },
                new() { FirstCriterionId = ids[0], SecondCriterionId = ids[2], Value = 4m },
                new() { FirstCriterionId = ids[1], SecondCriterionId = ids[2], Value = 2m }
            ],
            Rankings = [new() { CriterionId = ids[0], Rank = 1 }, new() { CriterionId = ids[1], Rank = 1 }, new() { CriterionId = ids[2], Rank = 3 }]
        };
        foreach (var expert in experts)
        {
            var result = await controller.Submit(questionnaire.Id, new SubmitExpertResponseRequest
            {
                ExpertId = expert.Id, Comparisons = responseRequest.Comparisons, Rankings = responseRequest.Rankings
            }, default);
            Assert.True(result.Result is CreatedAtActionResult,
                System.Text.Json.JsonSerializer.Serialize((result.Result as ConflictObjectResult)?.Value));
        }

        var duplicate = await controller.Submit(questionnaire.Id, new SubmitExpertResponseRequest
        {
            ExpertId = experts[0].Id, Comparisons = responseRequest.Comparisons, Rankings = responseRequest.Rankings
        }, default);
        Assert.IsType<ConflictObjectResult>(duplicate.Result);
        var aggregateAction = await controller.Aggregate(questionnaire.Id, default);
        var aggregate = Assert.IsType<QuestionnaireAggregateDto>(Assert.IsType<OkObjectResult>(aggregateAction.Result).Value);
        Assert.Equal(5, aggregate.ExpertCount);
        Assert.Equal(1d, aggregate.KendallW!.Value, 12);
        Assert.Equal(1m, aggregate.Weights.Sum(x => x.Weight));
        Assert.Equal(3, aggregate.Comparisons.Count);

        var setCriteria = await controller.SetCriteria(questionnaire.Id,
            new SetQuestionnaireCriteriaRequest { CriterionIds = ids.Reverse().ToList() }, default);
        Assert.IsType<ConflictObjectResult>(setCriteria.Result);
        await transaction.RollbackAsync();
    }
}
