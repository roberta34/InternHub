using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Notifications;
using InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence.AtomicWithdrawal;

public sealed class EfCoreAtomicCandidacyWithdrawal(
    InternHubDbContext db,
    TimeProvider timeProvider) : IAtomicCandidacyWithdrawal
{
    private const int MaximumAttempts = 3;
    private const int RetryDelayMilliseconds = 25;
    private const int SqliteBusyErrorCode = 5;
    private const int SqliteLockedErrorCode = 6;

    public async Task<AtomicCandidacyWithdrawalResult> WithdrawAsync(
        Guid candidacyId,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            db.ChangeTracker.Clear();

            try
            {
                return await WithdrawAttemptAsync(candidacyId, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
                cancellationToken.ThrowIfCancellationRequested();

                if (attempt == MaximumAttempts)
                {
                    break;
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsSqliteContention(exception))
            {
                db.ChangeTracker.Clear();
                cancellationToken.ThrowIfCancellationRequested();

                if (attempt == MaximumAttempts)
                {
                    break;
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (SqliteException exception)
                when (IsSqliteContention(exception))
            {
                db.ChangeTracker.Clear();
                cancellationToken.ThrowIfCancellationRequested();

                if (attempt == MaximumAttempts)
                {
                    break;
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }

        db.ChangeTracker.Clear();
        cancellationToken.ThrowIfCancellationRequested();

        return AtomicCandidacyWithdrawalResult.Failure(
            AtomicCandidacyWithdrawalStatus.ConcurrencyConflict);
    }

    private async Task<AtomicCandidacyWithdrawalResult> WithdrawAttemptAsync(
        Guid candidacyId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var candidacy = await db.InternshipCandidacies
            .SingleOrDefaultAsync(item => item.Id == candidacyId, cancellationToken);

        if (candidacy is null)
        {
            return AtomicCandidacyWithdrawalResult.Failure(
                AtomicCandidacyWithdrawalStatus.CandidacyNotFound);
        }

        var previousStatus = candidacy.Status;

        if (!candidacy.Withdraw())
        {
            return AtomicCandidacyWithdrawalResult.InvalidTransition(previousStatus);
        }

        if (previousStatus == InternshipCandidacyStatus.Shortlisted)
        {
            var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
            var notification = NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
                Guid.NewGuid(),
                candidacy.Id,
                candidacy.OfferId,
                candidacy.StudentId,
                occurredAtUtc);

            db.NotificationOutboxEntries.Add(notification);
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AtomicCandidacyWithdrawalResult.Success(candidacy);
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
