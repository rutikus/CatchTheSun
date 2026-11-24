using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.CalculationEngine.Models;

public sealed record CalculationHistoryItem(
    Guid Id,
    Guid SiteId,
    CalculationMethod Method,
    DateTime CreatedAt,
    string? AlgorithmVersion,
    int ResultCount);
