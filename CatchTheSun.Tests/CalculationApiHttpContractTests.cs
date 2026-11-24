using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.Json;
using CatchTheSun.API.Controllers;
using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CatchTheSun.Tests;

public sealed class CalculationApiHttpContractTests
{
    [Fact]
    public async Task HttpRoutesCreateAndReadEveryCalculationMethod()
    {
        var service = new InMemoryCalculationService();
        await using var app = await StartApiAsync(service);
        using var client = CreateClient(app);
        var siteId = Guid.NewGuid();
        var variantIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var criterionIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        foreach (var method in new[] { "AHP", "TOPSIS", "Pareto" })
        {
            var body = JsonSerializer.Serialize(new
            {
                siteId,
                variantIds,
                method,
                criteria = criterionIds.Select((id, index) => new
                {
                    criterionId = id,
                    weight = method == "TOPSIS" ? 0.5m : (decimal?)null
                }),
                comparisons = method == "AHP"
                    ? new object[] { new { criterionAId = criterionIds[0], criterionBId = criterionIds[1], value = 3m } }
                    : Array.Empty<object>()
            });
            using var response = await client.PostAsync("/api/calculations", Json(body));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(method, created.RootElement.GetProperty("method").GetString());
            Assert.Equal(siteId, created.RootElement.GetProperty("siteId").GetGuid());
            Assert.Equal(2, created.RootElement.GetProperty("results").GetArrayLength());
            Assert.True(DateTimeOffset.TryParse(created.RootElement.GetProperty("createdAt").GetString(), out _));

            var id = created.RootElement.GetProperty("id").GetGuid();
            using var detailResponse = await client.GetAsync($"/api/calculations/{id}");
            Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
            using var details = JsonDocument.Parse(await detailResponse.Content.ReadAsStringAsync());
            Assert.Equal(id, details.RootElement.GetProperty("id").GetGuid());
            Assert.Equal("Options", details.RootElement.GetProperty("options").GetProperty("source").GetString());
            Assert.Equal(2, details.RootElement.GetProperty("criteria").GetArrayLength());

            using var resultsResponse = await client.GetAsync($"/api/calculations/{id}/results");
            Assert.Equal(HttpStatusCode.OK, resultsResponse.StatusCode);
            using var results = JsonDocument.Parse(await resultsResponse.Content.ReadAsStringAsync());
            Assert.Equal(2, results.RootElement.GetArrayLength());
            Assert.Equal(2, results.RootElement[0].GetProperty("inputs").GetArrayLength());
            Assert.Equal(1m, results.RootElement[0].GetProperty("inputs")[0].GetProperty("value").GetDecimal());

            var first = details.RootElement.GetProperty("results")[0];
            Assert.Equal(JsonValueKind.Number, first.GetProperty("rank").ValueKind);
            if (method == "Pareto")
            {
                Assert.Equal(JsonValueKind.Null, first.GetProperty("score").ValueKind);
                Assert.Equal(JsonValueKind.Null, first.GetProperty("paretoSecondaryScore").ValueKind);
            }
            else
            {
                Assert.Equal(JsonValueKind.Number, first.GetProperty("score").ValueKind);
            }
        }

        using var historyResponse = await client.GetAsync($"/api/sites/{siteId}/calculations");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync());
        Assert.Equal(3, history.RootElement.GetArrayLength());
        Assert.All(history.RootElement.EnumerateArray(), item => Assert.Equal(2, item.GetProperty("resultCount").GetInt32()));

        using var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, swaggerResponse.StatusCode);
        using var swagger = JsonDocument.Parse(await swaggerResponse.Content.ReadAsStringAsync());
        Assert.True(swagger.RootElement.GetProperty("paths").TryGetProperty("/api/calculations/{id}/results", out _));
    }

    [Fact]
    public async Task HttpUnknownIdsAndInvalidRequestsReturnProblemDetails()
    {
        await using var app = await StartApiAsync(new InMemoryCalculationService());
        using var client = CreateClient(app);
        var missingId = Guid.NewGuid();
        using var missing = await client.GetAsync($"/api/calculations/{missingId}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await AssertProblemAsync(missing, HttpStatusCode.NotFound);
        using var missingResults = await client.GetAsync($"/api/calculations/{missingId}/results");
        Assert.Equal(HttpStatusCode.NotFound, missingResults.StatusCode);
        await AssertProblemAsync(missingResults, HttpStatusCode.NotFound);
        using var missingSiteHistory = await client.GetAsync($"/api/sites/{Guid.NewGuid()}/calculations");
        Assert.Equal(HttpStatusCode.NotFound, missingSiteHistory.StatusCode);
        await AssertProblemAsync(missingSiteHistory, HttpStatusCode.NotFound);

        var badRequest = await client.PostAsync("/api/calculations", Json("{\"variantIds\":[],\"criteria\":[]}"));
        using (badRequest)
        {
            Assert.Equal(HttpStatusCode.BadRequest, badRequest.StatusCode);
            await AssertProblemAsync(badRequest, HttpStatusCode.BadRequest);
        }

        var siteId = Guid.NewGuid();
        var invalidBusinessRequest = JsonSerializer.Serialize(new
        {
            siteId,
            variantIds = new[] { Guid.NewGuid(), Guid.NewGuid() },
            method = "TOPSIS",
            criteria = new[] { new { criterionId = Guid.NewGuid(), weight = 0.7m } },
            comparisons = Array.Empty<object>()
        });
        var unprocessable = await client.PostAsync("/api/calculations", Json(invalidBusinessRequest));
        using (unprocessable)
        {
            Assert.Equal((HttpStatusCode)422, unprocessable.StatusCode);
            await AssertProblemAsync(unprocessable, (HttpStatusCode)422);
        }
    }

    private static async Task<WebApplication> StartApiAsync(ICalculationService service)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICalculationService>(service);
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
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.Single()
            ?? throw new InvalidOperationException("Kestrel did not publish its bound address.");
        return new HttpClient { BaseAddress = new Uri(address) };
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Contains("application/problem+json", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class InMemoryCalculationService : ICalculationService
    {
        private readonly List<CalculationDetails> calculations = [];
        private readonly HashSet<Guid> sites = [];

        public Task<CalculationDetails> CreateAsync(CreateCalculationCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command.Method == CalculationMethod.TOPSIS && Math.Abs(command.Criteria.Sum(x => x.Weight ?? 0m) - 1m) > 0.000001m)
                throw new CalculationServiceException("calculation.topsis.invalid_input", CalculationServiceErrorKind.Validation, "Invalid TOPSIS weights.");

            var createdAt = DateTime.UtcNow;
            var details = new CalculationDetails(
                Guid.NewGuid(), command.SiteId, command.Method, createdAt, "1.0.0", "{\"source\":\"Options\"}",
                command.Method == CalculationMethod.AHP ? 0d : null,
                command.Method == CalculationMethod.AHP ? 2d : null,
                command.Method == CalculationMethod.AHP ? 2 : null,
                command.Criteria.Select((criterion, index) => new CalculationCriterionDetails(
                    criterion.CriterionId, $"Criterion {index + 1}", "unit", CriterionDirection.Maximize, null,
                    true, command.Method == CalculationMethod.AHP ? 0.5m : criterion.Weight)).ToArray(),
                command.Comparisons.Select(x => new CalculationComparisonDetails(x.CriterionAId, x.CriterionBId, x.Value)).ToArray(),
                command.VariantIds.Select((variantId, index) => new CalculationResultDetails(
                    variantId, $"Variant {index + 1}", $"external-{index + 1}",
                    command.Method == CalculationMethod.Pareto ? null : 0.75m - index * 0.25m,
                    index + 1,
                    command.Method == CalculationMethod.Pareto ? 1 : null,
                    command.Method == CalculationMethod.Pareto ? true : null,
                    null,
                    command.Criteria.Select((criterion, criterionIndex) => new CalculationValueSnapshotDetails(
                        criterion.CriterionId, index + criterionIndex + 1m)).ToArray())).ToArray());
            calculations.Add(details);
            sites.Add(command.SiteId);
            return Task.FromResult(details);
        }

        public Task<CalculationDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(calculations.SingleOrDefault(x => x.Id == id));
        }

        public Task<IReadOnlyList<CalculationHistoryItem>?> GetBySiteIdAsync(Guid siteId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sites.Contains(siteId))
                return Task.FromResult<IReadOnlyList<CalculationHistoryItem>?>(null);
            IReadOnlyList<CalculationHistoryItem> history = calculations
                .Where(x => x.SiteId == siteId)
                .Select(x => new CalculationHistoryItem(x.Id, x.SiteId, x.Method, x.CreatedAt, x.AlgorithmVersion, x.Results.Count))
                .ToArray();
            return Task.FromResult<IReadOnlyList<CalculationHistoryItem>?>(history);
        }

        public async Task<IReadOnlyList<CalculationResultDetails>?> GetResultsAsync(Guid calculationId, CancellationToken cancellationToken = default) =>
            (await GetByIdAsync(calculationId, cancellationToken))?.Results;
    }
}
