Summary: 0 Critical, 3 Major, 1 Minor findings.

| File:Line | Severity | Category | Problem | Proposed fix |
|---|---|---|---|---|
| src/InternHub.Api/Features/InternshipCandidacies/CreateInternshipCandidacy/CreateInternshipCandidacyHandler.cs:39 | Major | Repository and persistence | The active-candidacy check occurs separately from insertion, with no database uniqueness constraint or transaction. Concurrent requests can create duplicate active candidacies for the same student and offer. | Enforce the invariant atomically in persistence, such as a transaction plus an appropriate unique constraint/index and conflict translation. |
| src/InternHub.Api/Features/InternshipCandidacies/CreateInternshipCandidacy/CreateInternshipCandidacyHandler.cs:43 | Major | Repository and persistence | The five-active-candidacies limit is checked before insertion and is not protected against concurrent requests. Multiple requests can observe four active candidacies and all insert successfully. | Move the limit enforcement into an atomic transactional persistence operation with appropriate locking or database-side enforcement, returning `Result.Conflict` on failure. |
| src/InternHub.Api/Features/InternshipCandidacies/DecideInternshipCandidacy/DecideInternshipCandidacyHandler.cs:38 | Major | Repository and persistence | Accepted-slot counting and candidacy update are separate operations. Concurrent accept decisions can both observe an available slot and persist `Accepted`, exceeding `MaximumAvailableSlots`. | Perform slot allocation and status transition atomically, using a transaction/concurrency strategy that translates contention into a conflict. |
| src/InternHub.Api/Domain/InternshipCandidacies/InternshipCandidacy.cs:17 | Minor | Domain invariant | The public constructor accepts any castable `InternshipCandidacyStatus` value without validating that it is a defined enum member, allowing invalid domain state and potentially invalid persisted status values. | Reject undefined status values with an argument validation check such as `Enum.IsDefined`. |

**Review scope and verification:** The review covered Sprint 3 CQRS commands, queries, handlers, endpoints, feature modules, `InternshipCandidacy` domain rules and transitions, EF Core/SQLite repositories and configuration, DI and shared `Result<T>` HTTP mapping, unit tests, integration tests, and related Sprint 3 conventions. The solution build succeeded, and the complete test suite passed **314/314** tests with 0 failures and 0 skips. No issues were fixed as part of this review.

## Sprint 4 — Remediation Status

| Issue | Severity | Owner | Status |
|---|---|---|---|
| Duplicate active candidacies | Major | P3 + P1 | Pending |
| Five active candidacies concurrency limit | Major | P3 + P1 | Pending |
| Accepted slots concurrency | Major | P4 | Pending |
| Undefined `InternshipCandidacyStatus` | Minor | P2 | Resolved |

### Minor Issue Remediation (`Undefined InternshipCandidacyStatus` — P2.1)

- **Root cause:** The public `InternshipCandidacy` constructor accepted any castable `InternshipCandidacyStatus` value without verifying that it was a defined enum member, allowing invalid domain state.
- **Remediation:** Added validation in `src/InternHub.Api/Domain/InternshipCandidacies/InternshipCandidacy.cs` (`!Enum.IsDefined(status)`) throwing `ArgumentOutOfRangeException` for the `status` parameter (`nameof(status)`), and aligned the CQRS domain skill specification in `.claude/skills/cqrs/SKILL.md`.
- **Remediation commit:** `c0ff0e9` (`c0ff0e9f8a229bd92a74b44d2619a5ac3df74683` — *fix internship candidacy status*).
- **Testing & regression:**
  - Added unit tests in `tests/InternHub.Tests/Unit/InternshipCandidacies/InternshipCandidacyTests.cs` verifying that all 6 valid `InternshipCandidacyStatus` values (`Submitted`, `Shortlisted`, `Accepted`, `Rejected`, `Waitlisted`, `Withdrawn`) are accepted and that undefined cast values `(InternshipCandidacyStatus)999` and `(InternshipCandidacyStatus)(-1)` throw `ArgumentOutOfRangeException` with `ParamName == "status"`.
  - Existing regression tests continue to pass without modification.
  - Confirmed by live Sprint 4 test execution (`dotnet test`): **349/349** tests passed (0 failures, 0 skips), preserving the historical Sprint 3 baseline of **314/314** tests above.
- **Read-only `code-review` verification:** A read-only verification following `.claude/skills/code-review/SKILL.md` was executed across `InternshipCandidacy`, `InternshipCandidacyTests`, handlers, and EF Core/SQLite persistence contracts, confirming that the Minor `InternshipCandidacyStatus` domain-invariant finding is resolved (0 remaining findings in P2.1 scope) while the three Major concurrency findings remain `Pending` with no verifiable remediation in persistence or handlers yet.



