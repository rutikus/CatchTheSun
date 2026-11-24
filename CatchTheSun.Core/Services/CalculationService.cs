using System.Text.Json;
using System.Text.Json.Serialization;
using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Algorithms;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;

namespace CatchTheSun.Core.Services;

public sealed class CalculationService(
    ICalculationRepository repository,
    ICalculationAlgorithmFactory algorithmFactory) : ICalculationService
{
    private static readonly JsonSerializerOptions OptionsSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<CalculationDetails> CreateAsync(
        CreateCalculationCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateCommand(command);
        var calculationId = Guid.NewGuid();

        await using var session = await repository.BeginCreationAsync(cancellationToken);
        var source = await session.LoadSourceAsync(
            command.SiteId,
            command.VariantIds,
            command.Criteria.Select(x => x.CriterionId).ToArray(),
            cancellationToken);

        var site = source.Site ?? throw NotFound("calculation.site_not_found", "The requested site does not exist.");
        var variantsById = ValidateAndIndexVariants(command, source.Variants);
        var criteriaById = ValidateAndIndexCriteria(command, source.Criteria);
        ValidateRequiredCriteria(command, source.Criteria);
        ValidateCompleteMatrix(command, source.CriterionValues);

        var options = new CalculationAlgorithmOptions();
        IReadOnlyList<CriterionComparison> storedComparisons;
        IReadOnlyList<CalculationCriterionComparisonInput> algorithmComparisons;
        try
        {
            storedComparisons = command.Method == CalculationMethod.AHP
                ? command.Comparisons.Select(comparison => CriterionComparison.Create(
                    calculationId,
                    comparison.CriterionAId,
                    comparison.CriterionBId,
                    comparison.Value)).ToArray()
                : [];

            algorithmComparisons = command.Method == CalculationMethod.AHP
                ? storedComparisons.Select(x => new CalculationCriterionComparisonInput(
                    x.CriterionAId,
                    x.CriterionBId,
                    x.Value)).ToArray()
                : command.Comparisons;
        }
        catch (ArgumentException exception)
        {
            throw Validation("calculation.comparison_invalid", exception.Message, exception);
        }

        var criterionValues = source.CriterionValues.ToDictionary(
            x => (x.VariantId, x.CriterionId),
            x => x.Value);
        var criteriaInRequestOrder = command.Criteria
            .Select(selection =>
            {
                var criterion = criteriaById[selection.CriterionId];
                return new CalculationCriterionInput(
                    criterion.Id,
                    criterion.Direction,
                    criterion.TargetValue,
                    selection.Weight);
            })
            .ToArray();
        var alternatives = command.VariantIds.Select(variantId => new CalculationAlternativeInput(
            variantId,
            command.Criteria.Select(selection => criterionValues[(variantId, selection.CriterionId)])));

        CalculationInput input;
        try
        {
            input = new CalculationInput(
                command.Method,
                criteriaInRequestOrder,
                alternatives,
                algorithmComparisons,
                options);
        }
        catch (ArgumentException exception)
        {
            throw Validation("calculation.input_invalid", exception.Message, exception);
        }

        CalculationAlgorithmOutput algorithmOutput;
        try
        {
            algorithmOutput = algorithmFactory.Get(command.Method).Calculate(input, cancellationToken);
        }
        catch (CalculationAlgorithmException exception) when (exception.Code != "calculation.algorithm_not_registered")
        {
            throw Validation(exception.Code, exception.Message, exception);
        }

        var rankedOutput = CalculationResultRanker.Rank(algorithmOutput);
        var weightsByCriterion = ValidateAndIndexWeights(input, rankedOutput);
        var calculation = CreateCalculationEntity(
            calculationId,
            site,
            command,
            input,
            rankedOutput,
            variantsById,
            criteriaById,
            criterionValues,
            storedComparisons,
            weightsByCriterion,
            options);

        await session.SaveAsync(calculation, cancellationToken);
        await session.CommitAsync(cancellationToken);
        return ToDetails(calculation);
    }

    public async Task<CalculationDetails?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw InvalidRequest("calculation.id_invalid", "Calculation id must not be empty.");

        var calculation = await repository.GetByIdAsync(id, cancellationToken);
        return calculation is null ? null : ToDetails(calculation);
    }

    public async Task<IReadOnlyList<CalculationHistoryItem>?> GetBySiteIdAsync(
        Guid siteId,
        CancellationToken cancellationToken = default)
    {
        if (siteId == Guid.Empty)
            throw InvalidRequest("calculation.site_id_invalid", "SiteId must not be empty.");
        if (!await repository.SiteExistsAsync(siteId, cancellationToken))
            return null;

        return await repository.GetBySiteIdAsync(siteId, cancellationToken);
    }

    public async Task<IReadOnlyList<CalculationResultDetails>?> GetResultsAsync(
        Guid calculationId,
        CancellationToken cancellationToken = default)
    {
        var calculation = await GetByIdAsync(calculationId, cancellationToken);
        return calculation?.Results;
    }

    private static void ValidateCommand(CreateCalculationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.SiteId == Guid.Empty)
            throw InvalidRequest("calculation.site_id_invalid", "SiteId must not be empty.");
        if (!Enum.IsDefined(command.Method))
            throw InvalidRequest("calculation.method_invalid", "Calculation method is not supported.");
        if (command.VariantIds is null || command.Criteria is null || command.Comparisons is null)
            throw InvalidRequest("calculation.request_invalid", "VariantIds, Criteria, and Comparisons are required collections.");
        if (command.VariantIds.Count < 2)
            throw Validation("calculation.variants_too_few", "At least two variants are required for every calculation method.");
        if (command.VariantIds.Any(x => x == Guid.Empty) || command.VariantIds.Distinct().Count() != command.VariantIds.Count)
            throw InvalidRequest("calculation.variant_ids_invalid", "VariantId values must be non-empty and unique.");
        if (command.Criteria.Count == 0)
            throw Validation("calculation.criteria_required", "At least one criterion must be selected.");
        if (command.Criteria.Any(x => x.CriterionId == Guid.Empty) ||
            command.Criteria.Select(x => x.CriterionId).Distinct().Count() != command.Criteria.Count)
        {
            throw InvalidRequest("calculation.criterion_ids_invalid", "CriterionId values must be non-empty and unique.");
        }
    }

    private static IReadOnlyDictionary<Guid, Variant> ValidateAndIndexVariants(
        CreateCalculationCommand command,
        IReadOnlyList<Variant> variants)
    {
        var byId = variants.ToDictionary(x => x.Id);
        var missing = command.VariantIds.Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
            throw NotFound("calculation.variant_not_found", $"Variant(s) not found: {string.Join(", ", missing)}.");

        var fromOtherSite = command.VariantIds.Where(id => byId[id].SiteId != command.SiteId).ToArray();
        if (fromOtherSite.Length > 0)
            throw Validation("calculation.variant_site_mismatch", "Every selected variant must belong to the requested site.");

        return byId;
    }

    private static IReadOnlyDictionary<Guid, Criterion> ValidateAndIndexCriteria(
        CreateCalculationCommand command,
        IReadOnlyList<Criterion> criteria)
    {
        var byId = criteria.ToDictionary(x => x.Id);
        var missing = command.Criteria.Select(x => x.CriterionId).Where(id => !byId.ContainsKey(id)).ToArray();
        if (missing.Length > 0)
            throw NotFound("calculation.criterion_not_found", $"Criterion(s) not found: {string.Join(", ", missing)}.");

        foreach (var selection in command.Criteria)
        {
            var criterion = byId[selection.CriterionId];
            if (!criterion.IsActive)
                throw Validation("calculation.criterion_inactive", $"Criterion '{criterion.Name}' is inactive.");
            if (!Enum.IsDefined(criterion.Direction))
                throw Validation("calculation.criterion_direction_invalid", $"Criterion '{criterion.Name}' has an unsupported direction.");
            if (criterion.Direction == CriterionDirection.Target && criterion.TargetValue is null)
                throw Validation("calculation.target_value_required", $"TargetValue is required for criterion '{criterion.Name}'.");
        }

        return byId;
    }

    private static void ValidateRequiredCriteria(
        CreateCalculationCommand command,
        IReadOnlyList<Criterion> criteria)
    {
        var selected = command.Criteria.Select(x => x.CriterionId).ToHashSet();
        var missingRequired = criteria
            .Where(x => x.IsActive && x.IsRequired && !selected.Contains(x.Id))
            .Select(x => x.Name)
            .ToArray();
        if (missingRequired.Length > 0)
        {
            throw Validation(
                "calculation.required_criteria_missing",
                $"All active required criteria must be included. Missing: {string.Join(", ", missingRequired)}.");
        }
    }

    private static void ValidateCompleteMatrix(
        CreateCalculationCommand command,
        IReadOnlyList<CriterionValue> criterionValues)
    {
        var valuePairs = criterionValues.Select(x => (x.VariantId, x.CriterionId)).ToHashSet();
        foreach (var variantId in command.VariantIds)
        foreach (var criterion in command.Criteria)
        {
            if (!valuePairs.Contains((variantId, criterion.CriterionId)))
            {
                throw Validation(
                    "calculation.matrix_incomplete",
                    $"Variant '{variantId}' has no value for criterion '{criterion.CriterionId}'.");
            }
        }
    }

    private static IReadOnlyDictionary<Guid, decimal?> ValidateAndIndexWeights(
        CalculationInput input,
        CalculationAlgorithmOutput output)
    {
        if (output.Method != input.Method)
            throw new InvalidOperationException("Calculation algorithm returned an output for a different method.");

        var byId = output.CriterionWeights.ToDictionary(x => x.CriterionId, x => x.Weight);
        if (byId.Count != input.Criteria.Count || input.Criteria.Any(x => !byId.ContainsKey(x.CriterionId)))
            throw new InvalidOperationException("Calculation algorithm did not return one weight entry for every selected criterion.");

        foreach (var criterion in input.Criteria)
        {
            if (input.Method is CalculationMethod.AHP or CalculationMethod.TOPSIS && byId[criterion.CriterionId] is null)
                throw new InvalidOperationException("AHP and TOPSIS algorithms must return a weight for every criterion.");
        }

        if (input.Method == CalculationMethod.Pareto &&
            input.Criteria.Any(x => x.Weight is not null) && input.Criteria.Any(x => x.Weight is null))
            throw new InvalidOperationException("Pareto input contains a partial weight set.");

        return byId;
    }

    private static Calculation CreateCalculationEntity(
        Guid calculationId,
        Site site,
        CreateCalculationCommand command,
        CalculationInput input,
        CalculationAlgorithmOutput output,
        IReadOnlyDictionary<Guid, Variant> variantsById,
        IReadOnlyDictionary<Guid, Criterion> criteriaById,
        IReadOnlyDictionary<(Guid VariantId, Guid CriterionId), decimal> criterionValues,
        IReadOnlyList<CriterionComparison> storedComparisons,
        IReadOnlyDictionary<Guid, decimal?> weightsByCriterion,
        CalculationAlgorithmOptions options)
    {
        var calculation = new Calculation
        {
            Id = calculationId,
            SiteId = site.Id,
            Site = site,
            Method = command.Method,
            CreatedAt = DateTime.UtcNow,
            AlgorithmVersion = options.AlgorithmVersion,
            OptionsJson = JsonSerializer.Serialize(options, OptionsSerializerOptions),
            ConsistencyRatio = output.Diagnostics.ConsistencyRatio,
            LambdaMax = output.Diagnostics.LambdaMax,
            IterationCount = output.Diagnostics.IterationCount
        };

        var settingsByCriterion = new Dictionary<Guid, CalculationCriterion>();
        foreach (var criterionInput in input.Criteria)
        {
            var criterion = criteriaById[criterionInput.CriterionId];
            var setting = new CalculationCriterion
            {
                Id = Guid.NewGuid(),
                CalculationId = calculationId,
                CriterionId = criterion.Id,
                Criterion = criterion,
                Calculation = calculation,
                NameSnapshot = criterion.Name,
                UnitSnapshot = criterion.Unit,
                DirectionSnapshot = criterion.Direction,
                TargetValueSnapshot = criterion.TargetValue,
                IsRequiredSnapshot = criterion.IsRequired,
                Weight = weightsByCriterion[criterion.Id]
            };
            settingsByCriterion.Add(criterion.Id, setting);
            calculation.Criteria.Add(setting);
        }

        foreach (var comparison in storedComparisons)
        {
            comparison.CalculationId = calculationId;
            comparison.Calculation = calculation;
            comparison.CalculationCriterionA = settingsByCriterion[comparison.CriterionAId];
            comparison.CalculationCriterionB = settingsByCriterion[comparison.CriterionBId];
            calculation.CriterionComparisons.Add(comparison);
        }

        foreach (var result in output.Results)
        {
            var variant = variantsById[result.VariantId];
            var resultEntity = new CalculationResult
            {
                Id = Guid.NewGuid(),
                CalculationId = calculationId,
                Calculation = calculation,
                VariantId = variant.Id,
                Variant = variant,
                SiteId = site.Id,
                VariantNameSnapshot = variant.Name,
                ExternalIdSnapshot = variant.ExternalId,
                Score = RoundForStorage(result.Score),
                Rank = result.Rank,
                ParetoFront = result.ParetoFront,
                IsParetoOptimal = result.IsParetoOptimal,
                ParetoSecondaryScore = RoundForStorage(result.ParetoSecondaryScore)
            };

            foreach (var setting in calculation.Criteria)
            {
                resultEntity.InputSnapshots.Add(new CalculationResultCriterionSnapshot
                {
                    Id = Guid.NewGuid(),
                    CalculationResultId = resultEntity.Id,
                    CalculationId = calculationId,
                    CriterionId = setting.CriterionId,
                    ValueSnapshot = criterionValues[(variant.Id, setting.CriterionId)],
                    DirectionSnapshot = setting.DirectionSnapshot,
                    TargetValueSnapshot = setting.TargetValueSnapshot,
                    WeightSnapshot = setting.Weight,
                    CalculationResult = resultEntity
                });
            }

            calculation.Results.Add(resultEntity);
        }

        return calculation;
    }

    private static decimal? RoundForStorage(decimal? value) =>
        value is null ? null : Math.Round(value.Value, 8, MidpointRounding.ToEven);

    private static CalculationDetails ToDetails(Calculation calculation)
    {
        var criteria = calculation.Criteria
            .OrderBy(x => x.CriterionId.ToString("N"), StringComparer.Ordinal)
            .Select(x => new CalculationCriterionDetails(
                x.CriterionId,
                x.NameSnapshot,
                x.UnitSnapshot,
                x.DirectionSnapshot,
                x.TargetValueSnapshot,
                x.IsRequiredSnapshot,
                x.Weight))
            .ToArray();
        var comparisons = calculation.CriterionComparisons
            .OrderBy(x => x.CriterionAId.ToString("N"), StringComparer.Ordinal)
            .ThenBy(x => x.CriterionBId.ToString("N"), StringComparer.Ordinal)
            .Select(x => new CalculationComparisonDetails(x.CriterionAId, x.CriterionBId, x.Value))
            .ToArray();
        var results = calculation.Results
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.VariantId.ToString("N"), StringComparer.Ordinal)
            .Select(x => new CalculationResultDetails(
                x.VariantId,
                x.VariantNameSnapshot,
                x.ExternalIdSnapshot,
                x.Score,
                x.Rank,
                x.ParetoFront,
                x.IsParetoOptimal,
                x.ParetoSecondaryScore,
                x.InputSnapshots
                    .OrderBy(snapshot => snapshot.CriterionId.ToString("N"), StringComparer.Ordinal)
                    .Select(snapshot => new CalculationValueSnapshotDetails(snapshot.CriterionId, snapshot.ValueSnapshot))
                    .ToArray()))
            .ToArray();

        return new CalculationDetails(
            calculation.Id,
            calculation.SiteId,
            calculation.Method,
            calculation.CreatedAt,
            calculation.AlgorithmVersion,
            calculation.OptionsJson,
            calculation.ConsistencyRatio,
            calculation.LambdaMax,
            calculation.IterationCount,
            criteria,
            comparisons,
            results);
    }

    private static CalculationServiceException NotFound(string code, string message) =>
        new(code, CalculationServiceErrorKind.NotFound, message);

    private static CalculationServiceException Validation(string code, string message, Exception? inner = null) =>
        new(code, CalculationServiceErrorKind.Validation, message, inner);

    private static CalculationServiceException InvalidRequest(string code, string message) =>
        new(code, CalculationServiceErrorKind.InvalidRequest, message);
}
