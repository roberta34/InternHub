using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence.AtomicSubmission;

public sealed class EfCoreAtomicCandidacySubmission(
    InternHubDbContext db,
    TimeProvider timeProvider) : IAtomicCandidacySubmission
{
    private const int MaximumAttempts = 3;
    private const int RetryDelayMilliseconds = 25;
    private const int SqliteBusyErrorCode = 5;
    private const int SqliteLockedErrorCode = 6;
    private const int SqliteUniqueConstraintExtendedErrorCode = 2067;

    public async Task<AtomicCandidacySubmissionResult> SubmitAsync(
        Guid studentId,
        Guid offerId,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            db.ChangeTracker.Clear();

            try
            {
                return await SubmitAttemptAsync(
                    studentId,
                    offerId,
                    cancellationToken);
            }
            catch (DbUpdateException exception)
                when (IsSqliteUniqueConstraint(exception))
            {
                db.ChangeTracker.Clear();
                try
                {
                    if (await HasActiveCandidacyAsync(
                            studentId,
                            offerId,
                            cancellationToken))
                    {
                        return AtomicCandidacySubmissionResult.Failure(
                            AtomicCandidacySubmissionStatus.DuplicateActive);
                    }
                }
                catch (Exception verificationException)
                    when (IsSqliteContention(verificationException))
                {
                    if (attempt == MaximumAttempts)
                    {
                        throw CreateContentionException(verificationException);
                    }

                    await DelayBeforeRetryAsync(cancellationToken);
                    continue;
                }

                throw;
            }
            catch (DbUpdateException exception)
                when (IsSqliteContention(exception))
            {
                if (attempt == MaximumAttempts)
                {
                    throw CreateContentionException(exception);
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
            catch (SqliteException exception)
                when (IsSqliteContention(exception))
            {
                if (attempt == MaximumAttempts)
                {
                    throw CreateContentionException(exception);
                }

                await DelayBeforeRetryAsync(cancellationToken);
            }
        }

        throw new InvalidOperationException("Submission attempts were exhausted.");
    }

    private async Task<AtomicCandidacySubmissionResult> SubmitAttemptAsync(
        Guid studentId,
        Guid offerId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        var studentExists = await db.Students
            .AnyAsync(student => student.Id == studentId, cancellationToken);

        if (!studentExists)
        {
            return AtomicCandidacySubmissionResult.Failure(
                AtomicCandidacySubmissionStatus.StudentNotFound);
        }

        var offer = await db.Offers
            .SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);

        if (offer is null)
        {
            return AtomicCandidacySubmissionResult.Failure(
                AtomicCandidacySubmissionStatus.OfferNotFound);
        }

        if (offer.IsClosed)
        {
            return AtomicCandidacySubmissionResult.Failure(
                AtomicCandidacySubmissionStatus.OfferClosed);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (offer.Deadline <= now)
        {
            return AtomicCandidacySubmissionResult.Failure(
                AtomicCandidacySubmissionStatus.DeadlineExpired);
        }

        var duplicateActive = await db.InternshipCandidacies
            .AnyAsync(candidacy =>
                candidacy.StudentId == studentId &&
                candidacy.OfferId == offerId &&
                (candidacy.Status == InternshipCandidacyStatus.Submitted ||
                 candidacy.Status == InternshipCandidacyStatus.Shortlisted ||
                 candidacy.Status == InternshipCandidacyStatus.Accepted ||
                 candidacy.Status == InternshipCandidacyStatus.Waitlisted),
                cancellationToken);

        if (duplicateActive)
        {
            return AtomicCandidacySubmissionResult.Failure(
                AtomicCandidacySubmissionStatus.DuplicateActive);
        }

        var activeCount = await db.InternshipCandidacies
            .CountAsync(candidacy =>
                candidacy.StudentId == studentId &&
                (candidacy.Status == InternshipCandidacyStatus.Submitted ||
                 candidacy.Status == InternshipCandidacyStatus.Shortlisted ||
                 candidacy.Status == InternshipCandidacyStatus.Accepted ||
                 candidacy.Status == InternshipCandidacyStatus.Waitlisted),
                cancellationToken);

        if (activeCount >= 5)
        {
            return AtomicCandidacySubmissionResult.Failure(
                AtomicCandidacySubmissionStatus.ActiveLimitReached);
        }

        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            studentId,
            offerId,
            now,
            InternshipCandidacyStatus.Submitted);

        db.InternshipCandidacies.Add(candidacy);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return AtomicCandidacySubmissionResult.Success(candidacy);
    }

    private async Task<bool> HasActiveCandidacyAsync(
        Guid studentId,
        Guid offerId,
        CancellationToken cancellationToken) =>
        await db.InternshipCandidacies.AnyAsync(
            candidacy =>
                candidacy.StudentId == studentId &&
                candidacy.OfferId == offerId &&
                (candidacy.Status == InternshipCandidacyStatus.Submitted ||
                 candidacy.Status == InternshipCandidacyStatus.Shortlisted ||
                 candidacy.Status == InternshipCandidacyStatus.Accepted ||
                 candidacy.Status == InternshipCandidacyStatus.Waitlisted),
            cancellationToken);

    private static bool IsSqliteUniqueConstraint(DbUpdateException exception) =>
        exception.InnerException is SqliteException sqliteException &&
        sqliteException.SqliteErrorCode == 19 &&
        sqliteException.SqliteExtendedErrorCode ==
        SqliteUniqueConstraintExtendedErrorCode;

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

    private static InvalidOperationException CreateContentionException(
        Exception exception) =>
        new(
            "SQLite remained busy or locked after the maximum candidacy submission retries.",
            exception);

    private static Task DelayBeforeRetryAsync(
        CancellationToken cancellationToken) =>
        Task.Delay(RetryDelayMilliseconds, cancellationToken);
}
