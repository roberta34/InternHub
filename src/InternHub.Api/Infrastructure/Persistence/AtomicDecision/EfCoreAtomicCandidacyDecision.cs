using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence.AtomicDecision;

public sealed class EfCoreAtomicCandidacyDecision(
    InternHubDbContext db) : IAtomicCandidacyDecision
{
    private const int MaximumAttempts = 3;
    private const int RetryDelayMilliseconds = 25;
    private const int SqliteBusyErrorCode = 5;
    private const int SqliteLockedErrorCode = 6;

    public async Task<AtomicCandidacyDecisionResult> DecideAsync(
        Guid candidacyId,
        CandidacyDecision decision,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            db.ChangeTracker.Clear();

            try
            {
                return await DecideAttemptAsync(
                    candidacyId,
                    decision,
                    cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (attempt == MaximumAttempts)
                {
                    break;
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsSqliteContention(exception))
            {
                if (attempt == MaximumAttempts)
                {
                    break;
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (SqliteException exception)
                when (IsSqliteContention(exception))
            {
                if (attempt == MaximumAttempts)
                {
                    break;
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
        }

        db.ChangeTracker.Clear();

        return AtomicCandidacyDecisionResult.Failure(
            AtomicCandidacyDecisionStatus.ConcurrencyConflict);
    }

    private async Task<AtomicCandidacyDecisionResult> DecideAttemptAsync(
        Guid candidacyId,
        CandidacyDecision decision,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var candidacy = await db.InternshipCandidacies
            .SingleOrDefaultAsync(item => item.Id == candidacyId, cancellationToken);

        if (candidacy is null)
        {
            return AtomicCandidacyDecisionResult.Failure(
                AtomicCandidacyDecisionStatus.CandidacyNotFound);
        }

        bool changed;

        switch (decision)
        {
            case CandidacyDecision.Accept:
            {
                var offer = await db.Offers
                    .SingleOrDefaultAsync(item => item.Id == candidacy.OfferId, cancellationToken);

                if (offer is null)
                {
                    return AtomicCandidacyDecisionResult.Failure(
                        AtomicCandidacyDecisionStatus.OfferNotFound);
                }

                var acceptedCount = await db.InternshipCandidacies.CountAsync(
                    item => item.OfferId == offer.Id &&
                            item.Status == InternshipCandidacyStatus.Accepted,
                    cancellationToken);

                changed = candidacy.Accept(acceptedCount < offer.MaximumAvailableSlots);

                if (changed && candidacy.Status == InternshipCandidacyStatus.Accepted)
                {
                    // Same SaveChanges: the Offers row is guarded by Version.
                    offer.RegisterAcceptedSlot();
                }

                break;
            }
            case CandidacyDecision.Shortlist:
                changed = candidacy.Shortlist();
                break;
            case CandidacyDecision.Reject:
                changed = candidacy.Reject();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(decision));
        }

        if (!changed)
        {
            return AtomicCandidacyDecisionResult.Failure(
                AtomicCandidacyDecisionStatus.InvalidTransition);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AtomicCandidacyDecisionResult.Success(candidacy);
    }

    private static bool IsSqliteContention(Exception exception)
    {
        var sqliteException = exception switch
        {
            SqliteException direct => direct,
            DbUpdateException { InnerException: SqliteException inner } => inner,
            _ => null
        };

        return sqliteException is
            {
                SqliteErrorCode: SqliteBusyErrorCode or SqliteLockedErrorCode
            };
    }

    private static Task DelayBeforeRetryAsync(
        CancellationToken cancellationToken) =>
        Task.Delay(RetryDelayMilliseconds, cancellationToken);
}
