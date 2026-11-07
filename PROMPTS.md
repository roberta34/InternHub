# InternHub — PROMPTS.md

AI interaction log for **Sprint 2**. This document records the team's relevant prompts, the applicable skills, the observed problems, the changes made and the test results.

## Tools and skill execution

The team maintains Claude-format skills under `.claude/skills/`:

- `cqrs/SKILL.md`
- `efcore-sqlite-repositories/SKILL.md`
- `unit-testing/SKILL.md`
- `integration-testing/SKILL.md`
- `code-review/SKILL.md` (required for the review stage)

**Team clarification (29 October 2026):** Claude was used to execute the team's skills during Sprint 2, as reported by the team. The interaction records available for this document explicitly identify **GitHub Copilot Agent** for several of the individual runs and **ChatGPT** for drafting/review. Because the supplied logs do not include the Claude execution transcripts, the specific runs below retain their documented tool attribution. The reported additional Claude usage is recorded here without retroactively relabeling Copilot runs as verified Claude runs.

This log distinguishes **skill instructions used**, **the tool identified in the recorded interaction**, and **verified project results**. It does not invent command transcripts or tests.

---

## Person 1 — Student CQRS, tests and Postman

### Interaction 1 — Generate Student CQRS slices

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Refactor `CreateStudent`, `GetStudentById` and `GetStudents` into Commands/Queries, handlers, thin endpoints, DI registrations and the shared `Result<T>` response model while preserving `/api/v1` routes.

**Observed problem:** The initial `CreateStudent` generation introduced a namespace mismatch and a test compilation error.

**Result:** Namespace references were corrected; the three slices were implemented and the build and relevant tests passed. No CQRS skill improvement was documented for this particular error.

### Interaction 2 — Postman Student flows

**Recorded tool:** GitHub Copilot Agent  
**Skill:** Not applicable (Postman collection maintenance)

**Prompt summary:** Add Student Create/GetById/GetAll requests with variables, versioned routes, successful responses and negative scenarios.

**Observed problem:** The initial collection was missing `/api/v1`, an `apiVersion` variable and GetStudents coverage.

**Result:** Collection corrected and imported in Postman. The current Sprint 2 collection is stored in `postman/InternHub.postman_collection.json`.

### Interaction 3 — Generate Student handler unit tests

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate xUnit + FluentAssertions 7.x tests for `CreateStudentHandler`, `GetStudentByIdHandler` and `GetStudentsHandler` using the real interfaces, deterministic input, a hand-written fake and scoped coverage.

**Observed problem:** `FakeStudentRepository` could not simulate the repository-conflict branch for CreateStudent.

**Skill change:** None. A configurable `AddResult` was added to the fake repository, consistent with the existing skill.

**Verified result:** Build succeeded; the full suite passed **113/113** tests at that stage. Cobertura reported **100% line and branch coverage** for the three requested handlers.

### Interaction 4 — Duplicate-email conflict handling

**Scope:** Student duplicate-email behavior and the expected HTTP `409 Conflict` on applicable create/update paths.

**Result:** The current suite exercises the database uniqueness constraints and conflict-handling scenarios. Final full-suite verification is recorded below. No separate Claude execution transcript for this interaction was supplied.

---

## Person 2 — Student Update/Delete and shared persistence

### Interaction 1 — Convert Student Update/Delete to CQRS

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Convert Student Update/Delete to commands, handlers, thin endpoints and `Result<T>` outcomes; preserve the existing repository contracts and DI mapping.

**Result:** Handlers/interfaces were created, endpoints wired, legacy Delete endpoint removed and API contracts retained. Recorded commit: `refactor(student): wire CQRS update and delete endpoints`.

### Interaction 2 — EF Core + SQLite persistence

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/efcore-sqlite-repositories/SKILL.md`

**Prompt summary:** Replace in-memory persistence with EF Core + SQLite while retaining the public repository contracts and shared mappings.

**Result:** Added `DbContext`, entity configurations, EF repositories and persistence DI. Configured SQLite `10.0.14`, startup `EnsureCreated()`, GUID text mappings, candidacy cascade deletes and Offer optimistic concurrency through `Version`.

**Verified result:** `dotnet build` succeeded with **0 warnings and 0 errors**.

### Interaction 3 — Student Update/Delete unit tests

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate isolated xUnit and FluentAssertions 7.x tests for Student Update/Delete handlers and relevant Student behavior using a complete hand-written fake.

**Observed problem:** An assertion compared an `ArgumentException` error with an exact message that included a runtime-generated parameter-name suffix, making it brittle.

**Skill improvement:** Updated `.claude/skills/unit-testing/SKILL.md` to require assertions on the stable domain-message portion, without depending on runtime-generated suffixes; prefer a meaningful substring unless the entire message is an explicit public contract.

**Verified result:** `UpdateStudentHandlerTests` and `DeleteStudentHandlerTests` passed **7/7**; the new scoped unit tests passed **8/8**. API build passed with **0 warnings and 0 errors**.

---

## Person 3 — Offer CQRS, unit testing, coverage and CI

**Date:** 28–29 October 2026  
**Scope:** `CreateOffer`, `GetOfferById`, `GetOffers`, the shared unit-testing skill and existing CI.

### Interaction 1 — Generate Offer CQRS slices

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Refactor only CreateOffer, GetOfferById and GetOffers to Command/Query objects, handler interfaces, handlers, thin endpoints, DI registrations and `/api/v1` routes. Do not touch Update/Delete.

**Observed problem:** Generated endpoint files/classes retained older naming instead of the skill's `Endpoint.cs` convention.

**Result:** The affected endpoint names were corrected. The three Offer CQRS slices and DI registration were implemented.

### Interaction 2 — Review and improve the unit-testing skill

**Recorded tools:** ChatGPT (draft/revision), GitHub Copilot Agent (read-only review)  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Compare the skill against the real repository structure, namespaces, C# handler signatures, `Result<T>`, repository interfaces and NuGet dependencies.

**Observed problems:** Earlier examples assumed incorrect folder paths or asynchronous handlers. The skill needed stronger prerequisite checks and clearer testing/coverage responsibilities.

**Skill improvement:** Added repository-first inspection, exact signatures and namespaces, FluentAssertions **7.x** validation, deterministic dates/IDs, complete fake repositories, Given/When/Then naming and per-slice vs. team-wide coverage boundaries.

**Verified result:** Skill file contains the revised rules. Subsequent generation and coverage results are documented below.

### Interaction 3 — Prerequisite block for GetOffers tests

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate empty/nonempty repository unit tests for `GetOffersHandler` using FluentAssertions 7.0.0 and a complete fake.

**Observed problem:** The EF Core + SQLite stage was not yet complete at the time of the attempt.

**Result:** Generation stopped at the skill's prerequisite check; no unit tests were generated by that attempt. The generation was retried after persistence integration.

### Interaction 4 — Extend existing CI with coverage

**Recorded tool:** GitHub Copilot Agent  
**Skill:** Not applicable (existing CI maintenance)

**Prompt summary:** Preserve the .NET 10 restore/build/test/security checks; collect Cobertura using Coverlet and upload the XML report as a GitHub Actions artifact.

**Result:** `.github/workflows/ci.yml` collects `XPlat Code Coverage` into `TestResults` and uploads `**/coverage.cobertura.xml`. README has concise test and coverage commands.

**Initial verified result:** Coverage command passed **60/60** tests. Earlier baseline line coverage: overall **83.04%**, Offer **93.47%**, CreateOfferHandler **96.15%**, GetOfferByIdHandler **100%**, GetOffersHandler **0%**.

**Pending external verification:** A GitHub-hosted workflow execution and successful artifact upload were not evidenced in the supplied terminal logs.

### Interaction 5 — Generate Offer and read-handler unit tests

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate/refactor meaningful deterministic xUnit + FluentAssertions 7.x tests for `Offer`, `CreateOfferHandler`, `GetOfferByIdHandler` and `GetOffersHandler`, with complete hand-written fake repositories and scoped Cobertura verification.

**Observed problem:** Old Offer tests used nondeterministic dates, basic xUnit assertions and non-Given/When/Then naming; some required handler tests were absent.

**Skill change:** None; the improved skill already covered these issues.

**Verified result:** Build succeeded with **0 warnings and 0 errors**; the suite passed **90/90** tests at that stage. All four requested classes reported **100% line and branch coverage**.

### Interaction 6 — Update/Delete Offer handler tests (shared coordination)

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Add isolated handler tests for Update/Delete Offer success, validation, not-found, stale-version and repository concurrency-conflict scenarios.

**Observed problem:** The fake Offer repository lacked some call counters and a way to simulate `OfferConcurrencyException`.

**Skill change:** None; the fake was expanded under the existing skill rules.

**Verified result:** Build passed and the suite reached **98/98** passing tests. Both handlers reported **100% line and branch coverage**.

### Interaction 7 — Refactor Offer Create/Read integration tests

**Recorded tool:** GitHub Copilot Agent  
**Skills consulted:** `.claude/skills/integration-testing/SKILL.md`, `.claude/skills/unit-testing/SKILL.md`, `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Replace Sprint 1 integration tests for CreateOffer, GetOfferById and GetOffers using the existing shared SQLite `WebApplicationFactory` infrastructure; verify real HTTP responses and persisted database state, with fresh database isolation and Given/When/Then naming.

**Result:** Six scenarios were covered: valid create (`201`, body, Location, GUID and SQLite row), invalid create (`400`, no row), GetById existing (`200`), GetById missing (`404`), GetOffers with multiple records (`200`) and GetOffers empty (`200`). No unrelated production slices were modified.

**Verified result (29 October):** Filtering `OfferEndpointTests` passed **12/12**, with **0 failed / 0 skipped**, exit code `0`. The full suite passed **133/133**, **0 failed / 0 skipped**. `git diff --check` found no whitespace errors.

### Coverage confirmation

A previously generated Cobertura report showed **100% line and branch coverage** for all listed Domain classes and CQRS handlers, including Offer, Student, InternshipCandidacy and all Offer/Student handlers. That report predates the final integration-test refactor; it is not presented as a fresh coverage measurement after that change.

### Interaction 8 — EfCoreInternshipCandidacyRepository integration tests

**Recorded tool:** GitHub Copilot Agent
**Skill:** `.claude/skills/integration-testing/SKILL.md` (repository integration-test mode), with `.claude/skills/efcore-sqlite-repositories/SKILL.md` consulted for persistence conventions.

**Prompt summary:** Generate direct EF Core + SQLite integration tests for all eight `IInternshipCandidacyRepository` methods using isolated in-memory databases, valid Student/Offer foreign-key seed data, fresh verification contexts, deterministic dates, active-status filtering and missing-record behavior.

**Observed problem:** The existing repository had no integration-test coverage. `InternshipCandidacy` has no status transition method or setter, so the Update test must use a new entity instance with the existing ID rather than inventing a Domain API.

**Skill change:** None; the repository integration-testing section already required direct repository invocation, fresh SQLite connections and contexts, valid relational seed data, active-status cases and no invented status setter.

**Verified result:** Added `EfCoreInternshipCandidacyRepositoryTests.cs` covering Add, GetById found/missing, GetByStudent, GetByOffer, active-status detection, active counting, accepted counting, successful Update and missing Update. The repository-filtered run passed **15/15**, and the full suite passed **148/148**, with 0 failed and 0 skipped. `dotnet build` passed with 0 warnings and 0 errors.

### Interaction 9 — Correct active-candidacy test fixtures

**Prompt summary:** Review and correct the generated repository integration tests so fixtures respect Sprint 3's one-active-candidacy-per-student-per-offer rule, while proving Student/Offer filtering for active and accepted queries.

**Observed problem:** The original `GetByStudent` fixture inserted two active candidacies for the same Student and Offer, and the original `GetByOffer` and `CountAcceptedForOffer` fixtures inserted multiple active candidacies for the same Student and Offer.

**Skill improvement:** Added an explicit repository integration-test rule to `.claude/skills/integration-testing/SKILL.md`: multiple active candidacies for one student must use distinct offers, and multiple active candidacies for one offer must use distinct students.

**Result:** Regenerated the affected tests to use valid distinct Student/Offer pairs, added separate `HasActiveForStudentAndOffer` cases for another student and another offer, and added an accepted candidacy on another offer to prove exclusion.

---

## Person 4 — Offer Update/Delete and integration testing

### Interaction 1 — Offer Update/Delete CQRS and optimistic concurrency

**Skills:** `.claude/skills/cqrs/SKILL.md`, `.claude/skills/efcore-sqlite-repositories/SKILL.md`

**Scope:** Update/Delete Offer, including stale-version behavior and HTTP `409 Conflict` when applicable.

**Result:** The final suite includes these HTTP and repository scenarios. Detailed implementation and individual prompt records remain in the team's original development history.

### Interaction 2 — Unit tests for Offer Update/Delete

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate xUnit + FluentAssertions 7.x tests with fakes for success, validation, missing records, stale versions and concurrency exceptions.

**Observed problem:** The Offer fake needed configurable repository behavior and call-count tracking.

**Skill change:** None.

**Verified result:** At that stage, the suite passed **98/98**; UpdateOfferHandler and DeleteOfferHandler reached **100% line and branch coverage**.

### Interaction 3 — Integration-testing skill and SQLite test factory

**Skills:** `.claude/skills/integration-testing/SKILL.md` and related CQRS/persistence skills.

**Prompt summary:** Establish isolated real SQLite in-memory integration tests through `WebApplicationFactory<Program>`, covering HTTP status, response payloads and persisted state.

**Result:** A reusable integration fixture was created and subsequently reused by Person 3 for Offer Create/Read tests.

### Interaction 4 — End-to-end Postman verification

**Collection:** `postman/InternHub.postman_collection.json`

**Verified result from the team's prompt log:** The final Postman execution passed **48/48 assertions**.

---

## Final Sprint 2 verification

| Check | Observed result |
|---|---|
| Offer endpoint integration tests (filtered class) | **12 passed**, 0 failed, 0 skipped |
| Complete .NET test suite | **133 passed**, 0 failed, 0 skipped |
| Domain/handler coverage in available Cobertura report | **100% line and branch** for listed classes |
| CI coverage collection | Configured in `.github/workflows/ci.yml` |
| GitHub-hosted artifact upload | Not independently confirmed by provided logs |
| Postman | **48/48 assertions** as documented in team log |
| Postman location | `postman/InternHub.postman_collection.json` |
| README coverage instructions | `dotnet test ... --collect:"XPlat Code Coverage" --results-directory ./TestResults` |

**Skill iteration documented:** The `unit-testing` skill was improved following an actual brittle `ArgumentException` assertion; the updated rule avoids runtime-generated parameter-name suffixes. Subsequent scoped tests passed. Skill use and tool attribution are separated above so that the development log remains auditable.

## Sprint 3 — Person 1: Candidacy Application

### Apply to Offer / Get My Candidacies — Code Generation

**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Generate the `CreateInternshipCandidacy` and `GetStudentCandidacies` slices (command/query, handler + interface, endpoint) and the `CandidacyApplicationModule` IFeatureModule, using `IInternshipCandidacyRepository`, `TimeProvider` and `Result<T>`.

**Verified result:** Pending confirmation from Person 1.

### Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate xUnit + FluentAssertions tests with `FakeInternshipCandidacyRepository`, covering successful application, missing student/offer, closed offer, expired deadline, duplicate candidacies, the limit of 5 active candidacies, and result ordering.

**Verified result:** Pending confirmation from Person 1.

### Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP tests using isolated SQLite (`InternHubSqliteFactory`) and a controllable `TimeProvider`. Seed data directly through the DbContext. Verify HTTP 201/400/404/409, persisted database state and descending ordering.

**Verified result:** Pending confirmation from Person 1.

### Postman

**Skill:** Not applicable (Postman collection maintenance).

**Prompt summary:** Create Postman requests for Apply to Offer and Get My Candidacies, including setup requests for creating a student and an offer.

**Result:** Person 1 supplied a separate Postman collection for consolidation into the shared collection. Execution results for the consolidated requests are pending verification.

---

## Sprint 3 — Person 2: Withdraw Candidacy / Get Candidacy Details / Code Review

### CQRS Skill Update

**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Review the existing CQRS skill against the Sprint 3 modular feature registration architecture without modifying production code or implementing new features.

**Work completed:**

- Inspected `Program.cs`, `IFeatureModule`, `ApiRoutes` and the existing `OfferManagementModule`.
- Updated the skill to distinguish legacy feature registration from the modular Sprint 3 approach.
- Documented that new slices use a public parameterless `IFeatureModule`, register handlers through `RegisterServices`, map endpoints through `MapEndpoints`, and rely on automatic module discovery.
- Preserved existing CQRS, Result, route and handler conventions.

### GetInternshipCandidacyById — CQRS Implementation

**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Generate the `GetInternshipCandidacyById` query, handler interface, handler, response DTO, endpoint and `InternshipCandidacyFeatureModule`, using the existing repository and `Result<T>` pattern.

**Endpoint:** `GET /api/v1/candidacies/{id}`

**Behavior:** Return 200 with Id, StudentId, OfferId, ApplicationDate and Status when the candidacy exists, or 404 when it does not. The query does not modify state.

**Generated files:**

- `GetInternshipCandidacyByIdQuery.cs`
- `GetInternshipCandidacyByIdResponse.cs`
- `IGetInternshipCandidacyByIdHandler.cs`
- `GetInternshipCandidacyByIdHandler.cs`
- `Endpoint.cs`
- `InternshipCandidacyFeatureModule.cs`

**Verification:** `dotnet build --no-restore` succeeded with 0 warnings and 0 errors. Initial Rider unresolved-symbol diagnostics were not reproduced by the compiler.

### GetInternshipCandidacyById — Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate isolated unit tests for `GetInternshipCandidacyByIdHandler` using xUnit, FluentAssertions 7.x and `FakeInternshipCandidacyRepository`.

**Scenarios covered:**

- Existing candidacy returns Success and correct response fields.
- Missing candidacy returns NotFound.
- Query does not modify the candidacy or invoke repository updates.

**Generated files:**

- `GetInternshipCandidacyByIdHandlerTests.cs`
- `FakeInternshipCandidacyRepository.cs`

**Skill change:** None.

**Verified result:** 3/3 scoped tests passed. Full suite passed 179/179. Handler achieved 100% line and branch coverage.

### GetInternshipCandidacyById — Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP integration tests using the existing `InternHubSqliteFactory`, isolated SQLite databases and direct seeding of Student, Offer and InternshipCandidacy entities.

**Scenarios covered:**

- Existing candidacy returns 200 with correct response fields.
- Missing candidacy returns 404.
- GET does not modify persisted data.

**Generated file:** `GetInternshipCandidacyByIdEndpointTests.cs`

**Skill change:** None.

**Verified result:** 3/3 integration tests passed. Full suite passed 182/182. Build succeeded with 0 warnings and 0 errors.

### GetInternshipCandidacyById — Postman

**Skill:** Not applicable (Postman collection maintenance).

**Prompt summary:** Add requests for an existing candidacy and a missing candidacy to the existing shared collection.

**Requests:**

- `GET /api/v1/candidacies/{{candidacyId}}` — 200.
- Missing candidacy request — 404.

**Verification:** Postman Collection v2.1 JSON structure and request placement validated. HTTP assertions were not executed during this task because they require a running API and seeded data.

### WithdrawInternshipCandidacy — CQRS Implementation

**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Generate the Withdraw command, handler interface, handler and endpoint using the existing `InternshipCandidacy.Withdraw()` Domain method delivered by Person 4.

**Endpoint:** `POST /api/v1/candidacies/{id}/withdraw`

**Generated files:**

- `WithdrawInternshipCandidacyCommand.cs`
- `IWithdrawInternshipCandidacyHandler.cs`
- `WithdrawInternshipCandidacyHandler.cs`
- `Endpoint.cs`

**Modified files:**

- `InternshipCandidacyFeatureModule.cs`
- `PROMPTS.md`

**Behavior:**

- Missing candidacy returns 404.
- Submitted/Shortlisted can transition to Withdrawn.
- Invalid transitions return 409 without repository Update.
- Successful transitions are persisted through `IInternshipCandidacyRepository.Update()`.
- Successful HTTP requests return 204 No Content.
- Repository update failure returns NotFound, following existing project conventions.

**Verification:** `dotnet build` passed with 0 warnings and 0 errors. Tests were generated separately.

### WithdrawInternshipCandidacy — Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate unit tests for `WithdrawInternshipCandidacyHandler`, reusing the shared Fake Repository and testing successful withdrawals, forbidden transitions, missing candidacies and repository update failure.

**Generated file:** `WithdrawInternshipCandidacyHandlerTests.cs`

**Observed problem:** The initial full-suite execution overlapped with a coverage command, causing a temporary `FileNotFoundException` during test discovery. Running the suite sequentially resolved the issue.

**Skill change:** None.

**Verified result:** 8/8 scoped tests passed. Full suite passed 307/307 with 0 failed and 0 skipped. Handler achieved 100% line and branch coverage.

### WithdrawInternshipCandidacy — Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP integration tests using `InternHubSqliteFactory`, isolated SQLite databases, direct domain seeding and fresh DbContext verification.

**Scenarios covered:**

- Submitted → Withdrawn returns 204 and persists status.
- Shortlisted → Withdrawn returns 204 and persists status.
- Accepted, Rejected, Waitlisted and Withdrawn return 409, with unchanged state.
- Missing candidacy returns 404, with unchanged database.

**Generated file:** `WithdrawInternshipCandidacyEndpointTests.cs`

**Skill change:** None.

**Verified result:** 7/7 scoped integration tests passed. Full suite passed 314/314. Build succeeded with 0 warnings and 0 errors.

### WithdrawInternshipCandidacy — Postman

**Skill:** Not applicable (Postman collection maintenance).

**Prompt summary:** Add Postman requests for successful withdrawals from Submitted and Shortlisted, invalid withdrawal from a final status, and missing candidacy.

**Requests:**

- Withdraw Submitted — 204.
- Withdraw Shortlisted — 204.
- Withdraw final-state candidacy — 409.
- Withdraw missing candidacy — 404.

**Preparation:** Configure the relevant candidacy IDs and initial statuses before execution. Successful withdrawals are state-changing and must not be repeated using the same candidacy data.

**Verification:** Collection JSON structure, request names, variable uniqueness and expected assertions were validated. Actual HTTP execution remained pending.

### Code Review Skill

**Skill:** `.claude/skills/code-review/SKILL.md`

**Prompt summary:** Create a strictly read-only code-review skill supporting the InternHub repository and a classmate's repository.

**Created file:** `.claude/skills/code-review/SKILL.md`

**Rules included:**

- Review CQRS architecture, vertical slice boundaries, Domain invariants, repositories, DI, error handling, naming and test quality.
- Use YAML-based review rules.
- Define Critical, Major and Minor severities.
- Report exact File:Line references.
- Provide actionable proposed fixes.
- Sort findings by severity.
- Return a one-line summary followed by one Markdown table.
- Never modify reviewed source files.

**Verification:** Skill structure and YAML frontmatter inspected. The final code review has not been executed yet.

### Final Sprint 3 Code Review Execution

**Skill:** `.claude/skills/code-review/SKILL.md`

**Prompt summary:** Perform a strictly read-only review of the complete Sprint 3 implementation, including CQRS slices, feature modules, domain rules, EF Core/SQLite persistence, Result-to-HTTP mapping, and unit/integration tests.

**Reported findings:** 0 Critical, 3 Major and 1 Minor. The Major findings cover non-atomic duplicate-candidacy prevention, the five-active-candidacy limit, and accepted-slot enforcement under concurrency. The Minor finding covers missing validation for undefined `InternshipCandidacyStatus` enum values in the domain constructor. Exact file paths, lines, categories and proposed fixes are recorded in `REVIEW.md`.

**Verified result:** The solution build succeeded. The complete test suite passed **314/314** tests with 0 failures and 0 skips. No production code, tests, configuration, skills or other documentation were modified by the review.

---

## Sprint 3 — Person 3: Offer Management

### CloseOffer — Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate xUnit and FluentAssertions tests for `Offer.Close()`, covering successful closure, version increment, repeated closure (idempotency) and preservation of existing Offer properties.

**Verified result:** Generated unit tests and the complete test suite passed.

### CloseOffer — Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP integration tests for `POST /api/v1/offers/{id}/close`, using WebApplicationFactory and isolated SQLite in-memory databases.

**Scenarios covered:** Successful closure (204), missing offers (404), idempotency, persisted `IsClosed` and `Version` values.

**Verified result:** Generated integration tests and the complete test suite passed.

### Browse Active Offers — Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Extend `GetOffersHandlerTests` to verify `ActiveOnly` filtering with a deterministic TimeProvider.

**Scenarios covered:** Open future offers, closed offers, expired offers, deadline equality, unfiltered queries and empty results.

**Verified result:** Generated tests passed after adapting existing tests to the new TimeProvider constructor dependency.

### Browse Active Offers — Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP integration tests for `GET /api/v1/offers?activeOnly=true`, using WebApplicationFactory, isolated SQLite databases and a controllable TimeProvider.

**Scenarios covered:** Active-offer filtering, backward compatibility and empty results.

**Verified result:** Generated integration tests and the complete test suite passed.

### Postman Verification

**Collection:** `postman/InternHub.postman_collection.json`

**Prompt summary:** Extend the shared collection with CloseOffer and Browse Active Offers scenarios.

**Verified result:** Manual Postman tests passed for both features. A single shared collection was retained instead of separate person-specific collections.


## Sprint 3 — Person 4: Candidacy Decisions

### Domain Status Transitions

**Skill:** `.claude/skills/cqrs/SKILL.md` (section 12.1, added by Person 4)

**Prompt summary:** Extend `InternshipCandidacy` with `IsActive`, `IsActiveStatus`, `Shortlist`, `Accept(bool hasAvailableSlot)`, `Reject`, and `Withdraw`; forbidden transitions return `false`, no exceptions, no dependency on `Result<T>`.

**Verified result:** Generated correctly on first run; `dotnet build` succeeded.

**Follow-up skill change:** The shared private helper was generated as `CanBeDecided()`, misleading because `Withdraw` is not a decision. Added a naming rule (`IsOpenForTransition()`) to section 12.1 and regenerated; behavior unchanged, all tests pass.

### Domain Unit Tests — Skill Improvement (Before/After)

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Before:** The generated `IsActiveStatus` test merged sections into `// Arrange & Act`, violating the required visible Arrange / Act / Assert structure.

**Skill change:** Section 2 — "Never merge sections (`// Arrange & Act` is forbidden); the Arrange section still declares the input as a local variable".

**Reason:** The sprint requires separate AAA blocks; the skill did not cover tests with nothing to prepare.

**After:** Separate AAA sections; 250/250 tests pass; `InternshipCandidacy` 100% line and branch coverage.

### Decide / Get Offer Candidacies Slices — Skill Improvement (Before/After)

**Skill:** `.claude/skills/cqrs/SKILL.md` (sections 12.2–12.4)

**Before:** The guidance "`TryParse` + `IsDefined`" accepted `"1"` and `"Shortlist,Accept"`; the generator patched this with extra logic not described in the skill, and the 409 message read "cannot be Reject".

**Skill change:** Step 1 now requires an exact, case-insensitive match against `Enum.GetNames<CandidacyDecision>()`; step 4 uses past-tense verbs (`shortlisted`/`accepted`/`rejected`) in the Conflict message.

**Reason:** Generated code must come entirely from the skill; otherwise regeneration would reintroduce the bug.

**After:** Handler regenerated with no improvised logic.

**Verified HTTP behavior:**
- `"1"` and `"Shortlist,Accept"` → 400
- `"shortlist"` → 200
- 409 says "cannot be shortlisted/accepted/rejected".

### Handler Unit Tests

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Tests for `DecideInternshipCandidacyHandler` and `GetOfferCandidaciesHandler` with existing fakes (added `UpdateResult` to `FakeInternshipCandidacyRepository`, default `true`).

**Verified result:** 33 new cases, 283/283 pass, 100% line and branch coverage on both handlers.

### Integration Tests

**Skill:** `.claude/skills/integration-testing/SKILL.md` (section 6a added by Person 4)

**Prompt summary:** HTTP tests on isolated SQLite, data seeded directly in `InternHubDbContext`: shortlist, accept, waitlist when full, closed offer still decidable (BR-11), 400/404/409 with unchanged DB, offer candidacies ordering and 404.

**Verified result:** 10 new tests, 293/293 pass.

### Postman

**Prompt summary:** Requests for Decide and Get Offer Candidacies, with setup (2 students, 1 offer with 1 slot, 2 candidacies) and the demo flow Shortlist → Accept → Waitlisted.

**Verified result:** Collection Runner 19/19 tests passed against the running API.


### Final Sprint 3 — Postman Collection Validation

**Scope:** Consolidated shared Postman collection covering Sprint 2 and Sprint 3 functionality, including contributions from Persons 1, 2, 3 and 4.

**Work completed:** Integrated the Sprint 3 candidacy requests and added automated setup and variable handling to make the complete collection self-contained and repeatable.

**Verified result:**
- Full Collection Runner: 173/173 assertions passed.
- Failed: 0.
- Errors: 0.
- Iterations: 1 per execution.
- The complete collection was executed twice consecutively against the running API, with all assertions passing on both runs.
- No manual GUID configuration was required for the complete run.

---

## Sprint 4 — Person 2: Domain Validation, Candidacy Summary & AI Baseline

**Tool:** GitHub Copilot Agent (Rider)

**Objective:** Fix the Minor issue identified during Sprint 3 code review, implement a CQRS endpoint for student candidacy statistics, and create a deterministic CV evaluation baseline.

### P2.1 — Domain Enum Validation

**Skills:** `cqrs`, `unit-testing`

**Prompt summary:** Update the CQRS skill and generate validation in the `InternshipCandidacy` constructor using `Enum.IsDefined(status)` and `ArgumentOutOfRangeException`. Preserve existing domain behavior and generate unit tests for all six valid statuses and invalid values `999` and `-1`.

**Result:**
- Updated `.claude/skills/cqrs/SKILL.md`.
- Fixed invalid enum validation in Domain.
- Added regression tests.
- All 322 tests passed after this task.

**Commit:** `c0ff0e9`

### P2.2 — Get Student Candidacy Summary

**Skills:** `cqrs`, `unit-testing`, `integration-testing`

**Prompt summary:** Generate a new CQRS Vertical Slice for `GET /api/v1/students/{studentId}/candidacies/summary`, including Query, Response DTO, Handler, Endpoint and `CandidacySummaryModule`. Calculate total, active and per-status candidacy counts using existing repositories and `IsActive`.

**Result:**
- Implemented a read-only endpoint returning `200 OK` or `404 Not Found`.
- Added 6 unit tests and 4 HTTP integration tests using SQLite.
- All 332 tests passed after this task.

### P2.2 — Postman Collection

**Skill:** `collection-schema-v3`

**Prompt summary:** Add four Summary scenarios to the existing shared Postman collection: submitted candidacy, after withdrawal, student without candidacies, and missing student.

**Result:** Updated `postman/InternHub.postman_collection.json`, preserved existing requests, and executed the P2 folder manually.

### P2.3 — Deterministic CV Evaluation Baseline

**Skills:** `ai-baseline` (new), `unit-testing`

**Prompt summary:** Create a reusable `ai-baseline` skill and generate `KeywordRecruiterEvaluationService`, implementing `IRecruiterEvaluationService`. Compare CV skills with internship requirements using Regex, case-insensitive matching and a predefined technical vocabulary. Generate unit tests for matching, missing skills, duplicates, invalid inputs, word boundaries, determinism and cancellation.

**Result:**
- Created `.claude/skills/ai-baseline/SKILL.md`.
- Implemented `KeywordRecruiterEvaluationService`.
- Returns `MatchedSkills`, `MissingSkills`, `Suggestions` and `Summary`.
- No Ollama, external APIs or CV storage.
- Added 17 unit tests.
- Achieved 100% line and branch coverage for the baseline.
- Full test suite: **349/349 passed**, build successful.

### P2.1 — Code Review Verification

**Skill:** `code-review`

**Prompt summary:** Re-evaluate the Minor finding from Sprint 3 using the code-review skill. Verify the updated constructor, domain tests and relevant contracts without modifying production code.

**Result:**
- Minor issue: **RESOLVED**.
- No remaining findings in P2.1 scope.
- Confirmed validation and regression tests.
- Updated `REVIEW.md` with Sprint 4 remediation status.
- Preserved the original Sprint 3 findings and historical test results.
- Three Major concurrency findings remain pending verification.

### Final Summary

All three P2 tasks were implemented independently, using skill-guided code generation. The latest complete test suite reported **349/349 passing tests**, with 0 failures and a successful build. The CV baseline is ready for integration with the configurable AI Feedback endpoint developed by P1 and for comparison with P3's Ollama provider.

---

## Sprint 4 — Person 3: Ollama Recruiter Evaluation Provider

### P3.2 — Ollama Provider Implementation

**Recorded tool:** GitHub Copilot Agent  
**Skills:** `.claude/skills/ai-baseline/SKILL.md`, `.claude/skills/unit-testing/SKILL.md`, `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Implement `OllamaRecruiterEvaluationService` under `Infrastructure/AI/Ollama/` using the existing `IRecruiterEvaluationService` and `RecruiterEvaluationResult` contracts. Use an injected `HttpClient`, configurable Ollama endpoint and model, `/api/generate`, structured JSON, a versioned prompt, strict response validation and one retry for invalid evaluation JSON, without changing the baseline provider or final DI selection.

**Result:** The provider was created without changing the existing contracts, baseline service, or application provider selection. It sends the configured model and versioned prompt with `stream: false`, JSON output and low temperature; validates all four result fields; retries malformed evaluation JSON once; and propagates HTTP failures, timeout and cancellation.

### P3.2 — Fake-HTTP Unit Tests

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Add fake-`HttpMessageHandler` unit tests for valid structured responses, request payloads, JSON validation, the single retry, HTTP failures, timeout and cancellation without requiring an Ollama server.

**Result:** Ten provider tests were added and verified. The focused tests passed **10/10**; the complete suite subsequently passed **380/380** with 0 failures and 0 skips.

### P3.2 — Read-Only Code Review

**Recorded tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/code-review/SKILL.md`

**Prompt summary:** Perform a thorough read-only review of the untracked Ollama provider and its fake-HTTP tests against the existing recruiter-evaluation contracts, request protocol, JSON validation, retry behavior, cancellation, error handling and P3 ownership boundaries.

**Result:** The review identified no actionable findings. The implementation was assessed as ready for commit, with final provider selection and application integration remaining for Person 1.
