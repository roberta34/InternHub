# Database schema updates

The API currently initializes SQLite with `Database.EnsureCreated()`. `EnsureCreated`
creates the current model only when the database schema does not already exist; it
does not apply later model changes to an existing `internhub.db`.

The active-candidacy uniqueness index must therefore be applied to an existing
database with a non-destructive schema operation:

```sql
SELECT StudentId, OfferId, COUNT(*) AS ActiveCount
FROM InternshipCandidacies
WHERE Status IN ('Submitted', 'Shortlisted', 'Accepted', 'Waitlisted')
GROUP BY StudentId, OfferId
HAVING COUNT(*) > 1;
```

The query must return no rows before creating the index. Resolve any reported
duplicates using an approved data-retention decision; do not delete or recreate
the database automatically. After the duplicate check succeeds, apply:

```sql
CREATE UNIQUE INDEX IF NOT EXISTS IX_InternshipCandidacies_StudentId_OfferId
ON InternshipCandidacies (StudentId, OfferId)
WHERE Status IN ('Submitted', 'Shortlisted', 'Accepted', 'Waitlisted');
```

Back up the database first and verify the index afterward with:

```sql
SELECT name, sql
FROM sqlite_master
WHERE type = 'index'
  AND name = 'IX_InternshipCandidacies_StudentId_OfferId';
```

This index protects the one-active-candidacy rule for each student/offer pair. It
does not resolve the separate maximum-five-active-candidacies concurrency issue.
