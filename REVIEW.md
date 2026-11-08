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
| Accepted slots concurrency | Major | P4 | Resolved |
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

### Major Issue Remediation (`Accepted slots concurrency` — P4.1)

- **Root cause:** `DecideInternshipCandidacyHandler` counted `Accepted` candidacies (`CountAcceptedForOffer`) and saved the decision (`Update`) in separate repository operations, each with its own `SaveChanges` (check-then-act / write skew). Concurrent accept decisions on the last slot could both observe a free slot and persist `Accepted`, exceeding `MaximumAvailableSlots` (US5 AC3, BR-09, BR-10).
- **Remediation:**
  - Added the atomic persistence contract `IAtomicCandidacyDecision.DecideAsync(candidacyId, decision, cancellationToken)` and the typed `AtomicCandidacyDecisionResult` (`Decided`, `CandidacyNotFound`, `OfferNotFound`, `InvalidTransition`, `ConcurrencyConflict`) in `src/InternHub.Api/Features/InternshipCandidacies/AtomicDecision/`.
  - `DecideInternshipCandidacyHandler` now depends only on `IAtomicCandidacyDecision` and exposes `HandleAsync`. It validates the action before calling the contract and maps each status to exactly one `Result` through a lookup table (`Decided` → 200, `CandidacyNotFound` / `OfferNotFound` → 404, `InvalidTransition` / `ConcurrencyConflict` → 409).
  - `EfCoreAtomicCandidacyDecision` (`src/InternHub.Api/Infrastructure/Persistence/AtomicDecision/`) runs each attempt in one transaction. It loads the candidacy; for `Accept` it loads the offer, re-counts `Accepted` candidacies inside the transaction and calls `Accept(acceptedCount < MaximumAvailableSlots)` (→ `Accepted` or `Waitlisted`); it then performs a single `SaveChangesAsync` + `CommitAsync`. It clears the change tracker and retries up to 3 attempts on `DbUpdateConcurrencyException` and SQLite busy/locked (codes 5/6); after the last attempt it returns `ConcurrencyConflict` (HTTP 409). `IsClosed` and `Deadline` are not checked (BR-11).
  - Added `Offer.RegisterAcceptedSlot()` in `src/InternHub.Api/Domain/Offers/Offer.cs`, which increments the `Version` concurrency token. It is called only when the candidacy becomes `Accepted`, in the same `SaveChanges`, so concurrent slot-consuming accepts collide on the same `Offers` row.
  - Registered `IAtomicCandidacyDecision` → `EfCoreAtomicCandidacyDecision` as Scoped in `PersistenceExtensions.AddPersistence`.
  - Updated `.claude/skills/cqrs/SKILL.md` §13, `.claude/skills/efcore-sqlite-repositories/SKILL.md` §11/§20/§21 and `.claude/skills/integration-testing/SKILL.md` §4b/§6b; `.claude/skills/unit-testing/SKILL.md` was not changed.
  - The HTTP contract is unchanged: route `POST /api/v1/candidacies/{id}/decision`, `DecideInternshipCandidacyRequest`, `DecideInternshipCandidacyResponse` and status codes 200/400/404/409. Only two error texts changed: `Candidacy {id} cannot be {verb} from its current status.` and `Offer of candidacy {id} not found.`
- **Remediation commits:**
  - `6064a64` (`6064a6412cdc2915ae5eccb31de95448533db290` — *Extend skills for atomic candidacy decision (Sprint 4 P4.1)*).
  - `34051a2` (`34051a291f4ceda0e1b800e2f84acbd688cc7380` — *feat: atomic candidacy decision with Offer.Version concurrency and tests (P4.1)*).
- **Testing & regression:**
  - Unit: `DecideInternshipCandidacyHandlerTests` regenerated with a hand-written `FakeAtomicCandidacyDecision`: one test per status, invalid actions (`"1"`, `"Shortlist,Accept"`, `""`, `"   "`, `null`, `"foo"`) never call the contract, case-insensitive action parsing and cancellation-token pass-through. `OfferTests`: `RegisterAcceptedSlot()` increments `Version` by exactly 1 and changes nothing else.
  - Sequential integration (`EfCoreAtomicCandidacyDecisionTests`, in-memory SQLite per test): accept with a free slot (`Version` + 1), accept without a free slot (`Waitlisted`, `Version` unchanged), shortlist/reject (`Version` unchanged), invalid transition, missing candidacy, closed offer (BR-11).
  - Concurrency (`EfCoreAtomicCandidacyDecisionConcurrencyTests`, temporary file-based SQLite, own `DbContext` and connection per request): 2 and 5 concurrent accepts on the last slot (never more `Accepted` than `MaximumAvailableSlots`); a 20-accepts / 3-slots stress test asserting only the invariant; one-shot and always-on `Version` interceptor tests proving the `DbUpdateConcurrencyException` → retry → `ConcurrencyConflict` path.
  - HTTP: one error-message assertion in `DecideInternshipCandidacyEndpointTests` (`Given_RejectedCandidacy_When_Accepting_Then_ReturnsConflictAndPreservesStatus`) was updated to the new 409 text; its status-code and persisted-state assertions are unchanged.
  - Confirmed by live Sprint 4 test execution (`dotnet test`): **389/389** tests passed (0 failures, 0 skips), compared with the historical Sprint 3 baseline of **314/314** tests above. The AtomicDecision filter (`FullyQualifiedName~AtomicDecision`, 34 tests) passed 34/34 in each of 3 consecutive runs.
  - Manual Postman run of the shared collection on the new code: all requests passed; the collection was not modified because the HTTP contract is unchanged.
- **Read-only `code-review` verification:** A read-only verification following `.claude/skills/code-review/SKILL.md` was executed across `Offer`, the `AtomicDecision` contract, the `DecideInternshipCandidacy` slice, `EfCoreAtomicCandidacyDecision`, `PersistenceExtensions` and their tests, confirming that the Major `Accepted slots concurrency` finding at `DecideInternshipCandidacyHandler.cs:38` is resolved (0 remaining findings in P4.1 scope): the handler no longer counts or updates, and the slot count and status write now run in one transaction guarded by `Offer.Version`.



