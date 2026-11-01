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

## Sprint 3 — Person 3: Offer Management

### CloseOffer — Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Generate xUnit and FluentAssertions tests for `Offer.Close()`, covering successful closure, version increment, repeated closure (idempotency), and preservation of existing Offer properties.

**Verified result:** Generated unit tests passed. The complete test suite was also executed successfully.

### CloseOffer — Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP integration tests for `POST /api/v1/offers/{id}/close`, using WebApplicationFactory and isolated SQLite in-memory databases. Verify successful closure (204), missing offers (404), idempotency, persisted `IsClosed` and `Version` values.

**Verified result:** The generated integration tests and the complete test suite passed.

### Browse Active Offers — Unit Testing

**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt summary:** Extend `GetOffersHandlerTests` to verify `ActiveOnly` filtering with a deterministic TimeProvider. Cover open future offers, closed offers, expired offers, deadline equality, unfiltered queries, and empty results.

**Verified result:** Generated tests passed after adapting existing tests to the new TimeProvider constructor dependency.

### Browse Active Offers — Integration Testing

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Prompt summary:** Generate HTTP integration tests for `GET /api/v1/offers?activeOnly=true`, using WebApplicationFactory, isolated SQLite databases and a controllable TimeProvider. Verify filtering, backward compatibility and empty results.

**Verified result:** The generated integration tests and the complete test suite passed.

### Postman Verification

**Collection:** `postman/InternHub.postman_collection.json`

The shared collection was extended with CloseOffer and Browse Active Offers scenarios.

**Verified result:** Manual Postman tests passed for both features. A single shared collection is retained instead of separate person-specific collections.

## Sprint 3 — Person 2

### CQRS skill update

**Prompt summary:** Review `.claude/skills/cqrs/SKILL.md` against the Sprint 3 modular feature registration architecture. Do not implement a vertical slice or modify production code, tests, Domain, repositories, or `Program.cs`.

**Work completed:**

- Read the existing CQRS skill and verified its rules against `Program.cs`, `IFeatureModule`, `ApiRoutes`, and the existing `OfferManagementModule`.
- Updated the skill to distinguish legacy feature wiring from new Sprint 3+ modules.
- Documented that new slices must implement a public parameterless `IFeatureModule`, register handlers in `RegisterServices`, map endpoints in `MapEndpoints`, and rely on automatic module discovery instead of editing `Program.cs`.
- Preserved the existing CQRS, Result, route, handler, and compatibility rules.

### GetInternshipCandidacyById

**Skill:** `.claude/skills/cqrs/SKILL.md`

**Prompt summary:** Implement only the `GetInternshipCandidacyById` query for `GET /api/v1/candidacies/{id}` using `IInternshipCandidacyRepository`, the existing `Result<T>` pattern and the Sprint 3 `IFeatureModule` architecture. Do not implement Withdraw or generate tests.

**Generated files:**

- `src/InternHub.Api/Features/InternshipCandidacies/GetInternshipCandidacyById/GetInternshipCandidacyByIdQuery.cs`
- `src/InternHub.Api/Features/InternshipCandidacies/GetInternshipCandidacyById/GetInternshipCandidacyByIdResponse.cs`
- `src/InternHub.Api/Features/InternshipCandidacies/GetInternshipCandidacyById/IGetInternshipCandidacyByIdHandler.cs`
- `src/InternHub.Api/Features/InternshipCandidacies/GetInternshipCandidacyById/GetInternshipCandidacyByIdHandler.cs`
- `src/InternHub.Api/Features/InternshipCandidacies/GetInternshipCandidacyById/Endpoint.cs`
- `src/InternHub.Api/Features/InternshipCandidacies/InternshipCandidacyFeatureModule.cs`

**Validation:** `dotnet build --no-restore` succeeded with 0 warnings and 0 errors. No tests were run. Rider file inspection still reports unresolved-symbol diagnostics in the newly created files, although the compiler resolves them successfully; this appears to be an IDE index issue rather than a build error.