using CatchTheSun.Core.CalculationEngine.Models;

namespace CatchTheSun.Core.Interfaces;

public interface ICalculationService
{
    Task<CalculationDetails> CreateAsync(
        CreateCalculationCommand command,
        CancellationToken cancellationToken = default);

    Task<CalculationDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalculationHistoryItem>?> GetBySiteIdAsync(
        Guid siteId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalculationResultDetails>?> GetResultsAsync(
        Guid calculationId,
        CancellationToken cancellationToken = default);
}
