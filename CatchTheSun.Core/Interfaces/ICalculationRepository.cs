using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;

namespace CatchTheSun.Core.Interfaces;

public interface ICalculationRepository
{
    Task<Calculation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CalculationHistoryItem>> GetBySiteIdAsync(
        Guid siteId,
        CancellationToken cancellationToken = default);

    Task<ICalculationCreationSession> BeginCreationAsync(CancellationToken cancellationToken = default);
}

public interface ICalculationCreationSession : IAsyncDisposable
{
    Task<CalculationSourceData> LoadSourceAsync(
        Guid siteId,
        IReadOnlyCollection<Guid> variantIds,
        IReadOnlyCollection<Guid> criterionIds,
        CancellationToken cancellationToken = default);

    Task SaveAsync(Calculation calculation, CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);
}
