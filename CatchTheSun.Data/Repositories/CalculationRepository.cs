using System.Data;
using CatchTheSun.Core.CalculationEngine;
using CatchTheSun.Core.CalculationEngine.Models;
using CatchTheSun.Core.Entities;
using CatchTheSun.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace CatchTheSun.Data.Repositories;

public sealed class CalculationRepository(ApplicationDbContext db) : ICalculationRepository
{
    public Task<Calculation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Calculations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Criteria)
            .Include(x => x.CriterionComparisons)
            .Include(x => x.Results)
                .ThenInclude(x => x.InputSnapshots)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> SiteExistsAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        db.Sites.AnyAsync(x => x.Id == siteId, cancellationToken);

    public async Task<IReadOnlyList<CalculationHistoryItem>> GetBySiteIdAsync(
        Guid siteId,
        CancellationToken cancellationToken = default) =>
        await db.Calculations
            .AsNoTracking()
            .Where(x => x.SiteId == siteId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new CalculationHistoryItem(
                x.Id,
                x.SiteId,
                x.Method,
                x.CreatedAt,
                x.AlgorithmVersion,
                x.Results.Count))
            .ToListAsync(cancellationToken);

    public async Task<ICalculationCreationSession> BeginCreationAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        return new CalculationCreationSession(db, transaction);
    }

    private sealed class CalculationCreationSession(
        ApplicationDbContext db,
        IDbContextTransaction transaction) : ICalculationCreationSession
    {
        private bool saved;
        private bool committed;
        private bool disposed;

        public async Task<CalculationSourceData> LoadSourceAsync(
            Guid siteId,
            IReadOnlyCollection<Guid> variantIds,
            IReadOnlyCollection<Guid> criterionIds,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            var site = await db.Sites.SingleOrDefaultAsync(x => x.Id == siteId, cancellationToken);
            var variants = await db.Variants
                .Where(x => variantIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            var criteria = await db.Criteria
                .Where(x => criterionIds.Contains(x.Id) || (x.IsActive && x.IsRequired))
                .ToListAsync(cancellationToken);
            var selectedVariantIds = variants.Select(x => x.Id).ToArray();
            var selectedCriterionIds = criterionIds.ToArray();
            var values = await db.CriterionValues
                .Where(x => selectedVariantIds.Contains(x.VariantId) && selectedCriterionIds.Contains(x.CriterionId))
                .ToListAsync(cancellationToken);

            return new CalculationSourceData(site, variants, criteria, values);
        }

        public async Task SaveAsync(Calculation calculation, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(calculation);
            if (saved)
                throw new InvalidOperationException("A calculation creation session can save only one calculation.");

            db.Calculations.Add(calculation);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                saved = true;
            }
            catch (DbUpdateException exception) when (FindPostgresException(exception) is { } postgresException &&
                postgresException.SqlState is PostgresErrorCodes.UniqueViolation or
                    PostgresErrorCodes.ForeignKeyViolation or PostgresErrorCodes.RestrictViolation)
            {
                throw new CalculationServiceException(
                    "calculation.persistence_conflict",
                    CalculationServiceErrorKind.Conflict,
                    "The calculation could not be saved because it conflicts with existing data.",
                    exception);
            }
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!saved)
                throw new InvalidOperationException("A calculation must be saved before its creation session is committed.");

            await transaction.CommitAsync(cancellationToken);
            committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (disposed)
                return;

            try
            {
                if (!committed)
                    await transaction.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                await transaction.DisposeAsync();
                disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(CalculationCreationSession));
        }

        private static PostgresException? FindPostgresException(Exception exception)
        {
            for (Exception? current = exception; current is not null; current = current.InnerException)
            {
                if (current is PostgresException postgresException)
                    return postgresException;
            }

            return null;
        }
    }
}
