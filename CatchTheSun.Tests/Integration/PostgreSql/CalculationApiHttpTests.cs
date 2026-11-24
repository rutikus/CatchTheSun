using System.Net;
using System.Text;
using System.Text.Json;
using CatchTheSun.API.Controllers;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.Interfaces;
using CatchTheSun.Core.Services;
using CatchTheSun.Data;
using CatchTheSun.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CatchTheSun.Tests.Integration.PostgreSql;

[Collection(PostgreSqlCollection.Name)]
public sealed class CalculationApiHttpTests(PostgreSqlFixture fixture)
{
    [PostgreSqlFact]
    public async Task HttpCalculationMethodsPersistAndReturnDetailsResultsAndHistory()
    {
        var seed = await SeedAsync();
        try
        {
            await using var app = await StartApiAsync();
            using var client = CreateClient(app);

            var swagger = await client.GetAsync("/swagger/v1/swagger.json");
            Assert.Equal(HttpStatusCode.OK, swagger.StatusCode);
            using (var swaggerJson = JsonDocument.Parse(await swagger.Content.ReadAsStringAsync()))
            {
                var paths = swaggerJson.RootElement.GetProperty("paths");
                Assert.True(paths.TryGetProperty("/api/calculations", out _));
                Assert.True(paths.TryGetProperty("/api/calculations/{id}", out _));
                Assert.True(paths.TryGetProperty("/api/calculations/{id}/results", out _));
                Assert.True(paths.TryGetProperty("/api/sites/{siteId}/calculations", out _));
            }

            var createdByMethod = new Dictionary<string, JsonElement>();
            foreach (var method in new[] { "AHP", "TOPSIS", "Pareto" })
            {
                var post = await PostCalculationAsync(client, seed, method);
                Assert.Equal(HttpStatusCode.Created, post.StatusCode);
                Assert.NotNull(post.Headers.Location);

                using var createdJson = JsonDocument.Parse(await post.Content.ReadAsStringAsync());
                var created = createdJson.RootElement.Clone();
                createdByMethod.Add(method, created);

                var id = created.GetProperty("id").GetGuid();
                Assert.Equal(seed.SiteId, created.GetProperty("siteId").GetGuid());
                Assert.Equal(method, created.GetProperty("method").GetString());
                Assert.True(DateTimeOffset.TryParse(created.GetProperty("createdAt").GetString(), out _));
                Assert.Equal("1.0.0", created.GetProperty("algorithmVersion").GetString());
                Assert.Equal(JsonValueKind.Object, created.GetProperty("options").ValueKind);
                Assert.Equal(2, created.GetProperty("criteria").GetArrayLength());
                Assert.Equal(2, created.GetProperty("results").GetArrayLength());

                var firstResult = created.GetProperty("results")[0];
                Assert.True(firstResult.GetProperty("variantId").GetGuid() != Guid.Empty);
                Assert.Equal(JsonValueKind.Array, firstResult.GetProperty("inputs").ValueKind);
                Assert.Equal(2, firstResult.GetProperty("inputs").GetArrayLength());
                if (method is "AHP" or "TOPSIS")
                {
                    Assert.Equal(JsonValueKind.Number, firstResult.GetProperty("score").ValueKind);
                    Assert.Equal(JsonValueKind.Number, firstResult.GetProperty("rank").ValueKind);
                }
                else
                {
                    Assert.Equal(JsonValueKind.Null, firstResult.GetProperty("score").ValueKind);
                    Assert.Equal(JsonValueKind.Null, firstResult.GetProperty("paretoSecondaryScore").ValueKind);
                    Assert.Equal(JsonValueKind.Number, firstResult.GetProperty("paretoFront").ValueKind);
                    Assert.Equal(JsonValueKind.Number, firstResult.GetProperty("rank").ValueKind);
                }

                if (method == "AHP")
                {
                    Assert.Single(created.GetProperty("comparisons").EnumerateArray());
                    Assert.Equal(0d, created.GetProperty("consistencyRatio").GetDouble());
                }
                else
                {
                    Assert.Equal(0, created.GetProperty("comparisons").GetArrayLength());
                }

                var detailsResponse = await client.GetAsync(post.Headers.Location);
                Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
                using var detailsJson = JsonDocument.Parse(await detailsResponse.Content.ReadAsStringAsync());
                Assert.Equal(id, detailsJson.RootElement.GetProperty("id").GetGuid());
                Assert.Equal(method, detailsJson.RootElement.GetProperty("method").GetString());
                Assert.Equal(created.GetProperty("results").GetArrayLength(), detailsJson.RootElement.GetProperty("results").GetArrayLength());
                Assert.Contains(
                    detailsJson.RootElement.GetProperty("criteria").EnumerateArray(),
                    item => item.GetProperty("name").GetString() == "Solar exposure");

                var resultsResponse = await client.GetAsync($"/api/calculations/{id}/results");
                Assert.Equal(HttpStatusCode.OK, resultsResponse.StatusCode);
                using var resultsJson = JsonDocument.Parse(await resultsResponse.Content.ReadAsStringAsync());
                Assert.Equal(2, resultsJson.RootElement.GetArrayLength());
                Assert.Equal(2, resultsJson.RootElement[0].GetProperty("inputs").GetArrayLength());
            }

            var historyResponse = await client.GetAsync($"/api/sites/{seed.SiteId}/calculations");
            Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
            using var historyJson = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
            Assert.Equal(3, historyJson.RootElement.GetArrayLength());
            Assert.All(historyJson.RootElement.EnumerateArray(), entry =>
            {
                Assert.Equal(2, entry.GetProperty("resultCount").GetInt32());
                Assert.Contains(entry.GetProperty("method").GetString(), new[] { "AHP", "TOPSIS", "Pareto" });
            });

            await using var persistedDb = fixture.CreateDbContext();
            Assert.Equal(3, await persistedDb.Calculations.CountAsync(x => x.SiteId == seed.SiteId));
            var persistedAhp = await persistedDb.Calculations
                .Include(x => x.CriterionComparisons)
                .SingleAsync(x => x.Id == createdByMethod["AHP"].GetProperty("id").GetGuid());
            Assert.Single(persistedAhp.CriterionComparisons);
            Assert.Equal(3m, persistedAhp.CriterionComparisons.Single().Value);
        }
        finally
        {
            await CleanupAsync(seed);
        }
    }

    [PostgreSqlFact]
    public async Task HttpErrorsReturnExpectedStatusCodesAndProblemDetails()
    {
        var seed = await SeedAsync();
        try
        {
            await using var app = await StartApiAsync();
            using var client = CreateClient(app);

            var missingId = Guid.NewGuid();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/calculations/{missingId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/calculations/{missingId}/results")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/sites/{Guid.NewGuid()}/calculations")).StatusCode);

            var missingSiteBody = RequestBody(seed, "TOPSIS", [1m, 0m], siteId: Guid.NewGuid());
            var missingSite = await client.PostAsync("/api/calculations", JsonContent(missingSiteBody));
            Assert.Equal(HttpStatusCode.NotFound, missingSite.StatusCode);
            await AssertProblemDetailsAsync(missingSite, HttpStatusCode.NotFound, "calculation.site_not_found");

            var malformed = await client.PostAsync("/api/calculations", JsonContent($"{{\"siteId\":\"{seed.SiteId}\",\"variantIds\":[],\"method\":\"TOPSIS\",\"criteria\":[]}}"));
            Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
            await AssertProblemDetailsAsync(malformed, HttpStatusCode.BadRequest, null);

            var invalidWeights = await client.PostAsync("/api/calculations", JsonContent(RequestBody(seed, "TOPSIS", [0.2m, 0.2m])));
            Assert.Equal((HttpStatusCode)422, invalidWeights.StatusCode);
            await AssertProblemDetailsAsync(invalidWeights, (HttpStatusCode)422, "calculation.topsis.invalid_input");
        }
        finally
        {
            await CleanupAsync(seed);
        }
    }

    private async Task<WebApplication> StartApiAsync()
    {
        await using var connectionContext = fixture.CreateDbContext();
        var connectionString = connectionContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The PostgreSQL fixture has no connection string.");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddScoped<ICalculationRepository, CalculationRepository>();
        builder.Services.AddScoped<ICalculationService, CalculationService>();
        builder.Services.AddScoped<ICalculationAlgorithm, AhpWeightedSumAlgorithm>();
        builder.Services.AddScoped<ICalculationAlgorithm, TopsisAlgorithm>();
        builder.Services.AddScoped<ICalculationAlgorithm, ParetoAlgorithm>();
        builder.Services.AddScoped<ICalculationAlgorithmFactory, CalculationAlgorithmFactory>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CalculationsController).Assembly);
        builder.Services.AddSwaggerGen();

        var app = builder.Build();
        app.UseSwagger();
        app.MapControllers();
        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var server = app.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("Kestrel did not publish its bound address.");
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private async Task<SeedData> SeedAsync()
    {
        var siteId = Guid.NewGuid();
        var variantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var criterionIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            .OrderBy(id => id.ToString("N"), StringComparer.Ordinal)
            .ToArray();
        await using var db = fixture.CreateDbContext();
        var site = new Core.Entities.Site
        {
            Id = siteId, Name = "Calculation API test site", Location = "Integration test", Area = 10m,
            CreatedAt = DateTime.UtcNow
        };
        var variants = variantIds.Select((id, index) => new Core.Entities.Variant
        {
            Id = id, SiteId = siteId, Name = $"API variant {index + 1}", ExternalId = $"api-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        }).ToArray();
        var criteria = new[]
        {
            NewCriterion(criterionIds[0], "Solar exposure"),
            NewCriterion(criterionIds[1], "Building density")
        };
        db.AddRange(site);
        db.AddRange(variants);
        db.AddRange(criteria);
        db.CriterionValues.AddRange(
            new Core.Entities.CriterionValue { Id = Guid.NewGuid(), VariantId = variantIds[0], CriterionId = criterionIds[0], Value = 10m },
            new Core.Entities.CriterionValue { Id = Guid.NewGuid(), VariantId = variantIds[0], CriterionId = criterionIds[1], Value = 20m },
            new Core.Entities.CriterionValue { Id = Guid.NewGuid(), VariantId = variantIds[1], CriterionId = criterionIds[0], Value = 20m },
            new Core.Entities.CriterionValue { Id = Guid.NewGuid(), VariantId = variantIds[1], CriterionId = criterionIds[1], Value = 10m });
        await db.SaveChangesAsync();
        return new SeedData(siteId, variantIds, criterionIds);
    }

    private async Task CleanupAsync(SeedData seed)
    {
        await using var db = fixture.CreateDbContext();
        var calculationIds = await db.Calculations.Where(x => x.SiteId == seed.SiteId).Select(x => x.Id).ToArrayAsync();
        if (calculationIds.Length > 0)
        {
            await db.CalculationResults.Where(x => calculationIds.Contains(x.CalculationId)).ExecuteDeleteAsync();
            await db.Calculations.Where(x => calculationIds.Contains(x.Id)).ExecuteDeleteAsync();
        }

        await db.Variants.Where(x => seed.VariantIds.Contains(x.Id)).ExecuteDeleteAsync();
        await db.Criteria.Where(x => seed.CriterionIds.Contains(x.Id)).ExecuteDeleteAsync();
        await db.Sites.Where(x => x.Id == seed.SiteId).ExecuteDeleteAsync();
    }

    private static Core.Entities.Criterion NewCriterion(Guid id, string name) => new()
    {
        Id = id, Name = name, Unit = "unit", Direction = Core.Entities.CriterionDirection.Maximize,
        IsRequired = true, IsActive = true, CreatedAt = DateTime.UtcNow
    };

    private static string RequestBody(SeedData seed, string method, decimal?[] weights, Guid? siteId = null) =>
        JsonSerializer.Serialize(new
        {
            siteId = siteId ?? seed.SiteId,
            variantIds = seed.VariantIds,
            method,
            criteria = seed.CriterionIds.Select((id, index) => new { criterionId = id, weight = weights[index] }),
            comparisons = method == "AHP"
                ? new object[] { new { criterionAId = seed.CriterionIds[0], criterionBId = seed.CriterionIds[1], value = 3m } }
                : Array.Empty<object>()
        });

    private static StringContent JsonContent(string body) => new(body, Encoding.UTF8, "application/json");

    private static async Task<HttpResponseMessage> PostCalculationAsync(HttpClient client, SeedData seed, string method)
    {
        var weights = method switch
        {
            "TOPSIS" => new decimal?[] { 0.5m, 0.5m },
            _ => new decimal?[] { null, null }
        };
        return await client.PostAsync("/api/calculations", JsonContent(RequestBody(seed, method, weights)));
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode status, string? title)
    {
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
        if (title is not null)
            Assert.Equal(title, json.RootElement.GetProperty("title").GetString());
    }

    private sealed record SeedData(Guid SiteId, Guid[] VariantIds, Guid[] CriterionIds);
}
