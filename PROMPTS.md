# InternHub - PROMPTS.md

Short log of relevant Sprint 2 AI interactions. Each member adds their own entries. The team used the Claude-format skill definitions stored under `.claude/skills/` as generation instructions in GitHub Copilot Agent; this is distinct from executing a skill in Claude Code.

## CQRS - Person 1

**Skill referenced:** `.claude/skills/cqrs/SKILL.md`

| Prompt summary | Result |
|---|---|
| Convert `CreateStudent` to Command + Handler + thin Endpoint; reuse `Result<T>` and DI. | CQRS generated; initial namespace mismatch caused a test compilation error. Copilot corrected the references. |
| Convert `GetStudentById` to Query + Handler, returning 200/404. | Implemented; `dotnet build` passed, 60/60 tests passed, including 3 filtered GetById tests. |
| Convert `GetStudents` to read-only Query + Handler, returning 200 even for an empty list. | Implemented; build and tests reported passing. |

**CQRS skill changes:** None documented so far. The namespace error was fixed through Copilot, not by editing and rerunning the skill.

## Postman - Person 1

**Tool:** GitHub Copilot Agent (Postman task, not a Claude skill execution).

- **Prompt:** Add Student Create/GetById/GetAll requests, versioned URLs, variables and response checks.
- **Problem:** Generated collection initially lacked `/api/v1`, `apiVersion` and `GetStudents`.
- **Follow-up:** Asked Copilot to correct the JSON and add missing scenarios.
- **Result:** 7 requests across 3 folders; imported and reported passing in Postman.

## Skill improvement reconciliation

The documented Person 2 unit-testing improvement is a real observed failure and records the exact `SKILL.md` change and verified result. It was performed through Copilot following the skill, not through native Claude execution; no Claude regeneration is claimed. The repository history contains no verified Claude execution to document.


## Person 2 — Student Update/Delete, EF Core + SQLite și unit testing

Exact prompt text was not recorded in project files; the entries below are prompt summaries.

| Prompt summary | Skill referenced | Result |
|---|---|---|
| Convert Student Update/Delete to CQRS commands, handlers, thin endpoints, Result outcomes, DI and API mapping. | `.claude/skills/cqrs/SKILL.md` | Added Update/Delete handlers and interfaces, registered them in DI, mapped endpoints, removed the legacy Delete endpoint, and preserved repository/Result contracts. Commit: `refactor(student): wire CQRS update and delete endpoints`. |
| Replace in-memory persistence with EF Core + SQLite while preserving repository contracts and shared mappings. | `.claude/skills/efcore-sqlite-repositories/SKILL.md` | Added DbContext, entity configurations, EF repositories and persistence DI; configured SQLite `10.0.14`, `EnsureCreated()` at startup, GUID text mappings, candidacy cascade deletes and Offer version concurrency. `dotnet build` passed with 0 warnings and 0 errors. |
| Generate isolated xUnit/FluentAssertions 7.x tests for Student Update/Delete handlers and relevant Student behavior using a hand-written fake repository. | `.claude/skills/unit-testing/SKILL.md` | Added handler tests, Student update behavior test and fake repository. The scoped unit tests passed: 8 passed, 0 failed. |

### Unit-testing skill improvement

**Before:** the generated assertion was:

```csharp
result.Error.Should().Be("Full name is required.");
```

It failed because `Student.ValidateName` throws `ArgumentException("Full name is required.", nameof(value))`, and the handler passes `ex.Message` to `Result.Validation`. The actual message was:

```text
Full name is required. (Parameter 'value')
```

**Skill improvement:** `.claude/skills/unit-testing/SKILL.md` now requires stable domain-message assertions for `ArgumentException` results without depending on runtime-generated parameter suffixes:

```text
When asserting `Result<T>.Error` from an `ArgumentException`, verify the stable domain message without depending on the runtime-generated parameter-name suffix. Prefer a meaningful substring assertion unless the repository explicitly defines the complete error string as a public contract.
```

**After:** the regenerated assertion is:

```csharp
result.Status.Should().Be(ResultStatus.Validation);
result.Error.Should().Contain("Full name is required.");
```

**Verified:** `UpdateStudentHandlerTests` and `DeleteStudentHandlerTests` passed with 7/7 tests; the complete new scoped unit-test set passed with 8/8 tests. API build passed with 0 warnings and 0 errors.

## Person 3 - Offer CQRS, Unit Testing and Coverage

**Date:** 28 October 2026  
**Tools used:** GitHub Copilot Agent, ChatGPT  
**Scope:** `CreateOffer`, `GetOfferById`, `GetOffers`, reusable unit-testing skill, coverage and existing CI

### Interaction 1 - Generate Offer CQRS slices

**Tool:** GitHub Copilot Agent  
**Skill instructions:** `.claude/skills/cqrs/SKILL.md` (owned by Person 1)

**Request (summary):** Refactor only `CreateOffer`, `GetOfferById` and `GetOffers` to Commands/Queries, handler interfaces and handlers. Reuse the existing `Result<T>` model and `/api/v1` routes. Preserve request/response contracts; do not modify Update/Delete, persistence or tests.

**Result:** Copilot generated the three handlers, their interfaces and Command/Query types, changed the endpoints to call handlers and added DI registrations in `OffersFeaturesExtensions.cs`.

**Observed problem:** Endpoint files/classes retained their old names (`CreateOfferEndpoint.cs`, etc.), contrary to the CQRS skill's `Endpoint.cs` convention.

### Interaction 2 - Regenerate endpoint naming

**Tool:** GitHub Copilot Agent  
**Skill instructions:** `.claude/skills/cqrs/SKILL.md`

**Request (summary):** Regenerate the three Offer endpoints using `Endpoint.cs` and an `Endpoint` class in each use-case namespace. Update `Program.cs` references, keep behavior and routes unchanged, and remove legacy endpoint files.

**Improvement after regeneration:** All three slices now have `Endpoint.cs`; `Program.cs` registers their routes once under the shared `MapV1()` group. The endpoints invoke handlers, not `IOfferRepository` directly. `dotnet build` succeeded and **60/60 existing tests passed**. This is a concrete before/after correction of generated output.

### Interaction 3 - Improve the Unit Testing skill

**Tools:** ChatGPT (draft/revision), GitHub Copilot Agent (read-only review)  
**Skill:** `.claude/skills/unit-testing/SKILL.md` (owned by Person 3)

**Request (summary):** Review the skill against the actual InternHub test layout, Domain entities, CQRS handlers, `Result<T>` contracts and `.csproj`. Identify incompatibilities before generating tests.

**Problems observed in the initial draft:** Some examples assumed an incorrect test-folder layout or asynchronous handler methods; the skill needed explicit dependency checks, complete fake-repository contracts, deterministic dates and a clearer split between per-slice and project-wide coverage.

**Changes made to SKILL.md:** Added repository-first checks; required the real namespaces and signatures; aligned examples with existing `Unit/Offers/` folders and synchronous `Handle`/`ResultStatus`; added a FluentAssertions 7.x prerequisite gate, deterministic tests, complete hand-written fakes, and explicit per-slice coverage scope.

**Verification:** The updated file contains the hard-prerequisite, dependency-blocker and coverage-scope rules. A later Copilot review still reported some already-addressed points; those claims were not accepted as proof of a new defect without matching the current file. **Test-generation improvement is not yet verified.**

### Interaction 4 - Attempt to generate GetOffersHandler tests

**Tool:** GitHub Copilot Agent  
**Skill instructions:** `.claude/skills/unit-testing/SKILL.md`

**Request (summary):** Generate xUnit + FluentAssertions 7.0.0 tests for `GetOffersHandler` with an empty repository and multiple offers, using a complete fake repository and Given/When/Then naming.

**Result:** The skill stopped without generating files because EF Core + SQLite had not yet been applied. Its prerequisite rule requires the earlier CQRS and persistence stages to be complete. FluentAssertions 7.0.0 was already installed. **No new unit tests were generated in this interaction.**

**Next step:** After the shared EF Core stage is integrated, invoke the skill again for `Offer`, `CreateOfferHandler`, `GetOfferByIdHandler` and `GetOffersHandler`; verify the tests and coverage.

### Interaction 5 - Integrate code coverage into CI

**Tool:** GitHub Copilot Agent  
**Skill:** Not applicable; CI maintenance task (existing Sprint 1 responsibility)

**Request (summary):** Retain the existing .NET 10 GitHub Actions build/test/security checks, collect Cobertura coverage via Coverlet and upload the report as an artifact. Keep changes minimal; do not edit production code or tests or enforce a premature 100% threshold.

**Result:** The workflow was updated to run `dotnet test` with `--collect:"XPlat Code Coverage" --results-directory TestResults` and to upload `**/coverage.cobertura.xml` through `actions/upload-artifact@v4`. Short coverage instructions were also added to README. The local coverage command passed **60/60 tests** and produced a Cobertura report. **GitHub-hosted workflow execution and artifact upload still need confirmation.**

**Coverage baseline before new handler tests:** Overall line coverage **83.04%**; `Offer` **93.47%**; `CreateOfferHandler` **96.15%**; `GetOfferByIdHandler` **100%**; `GetOffersHandler` **0%**. These percentages are observed line coverage, not proof of complete behavioral or branch coverage.

### Remaining verification / assignment compliance

- The shared EF Core + SQLite stage was applied before the Person 3 unit-testing stage.
- Person 3 unit tests were regenerated/reworked and measured at 100% scoped line and branch coverage; Person 4 integration tests subsequently passed using the shared SQLite factory.
- GitHub-hosted workflow execution and artifact upload remain unconfirmed.

### Interaction 6 - Generate Person 3 unit tests after persistence integration

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt:** Generate/refactor meaningful xUnit + FluentAssertions 7.x tests for Offer, CreateOfferHandler, GetOfferByIdHandler and GetOffersHandler using deterministic inputs and a complete hand-written fake repository; verify the CQRS and EF Core + SQLite prerequisites, then run build, tests and Cobertura coverage.

**Observed problem:** The existing Offer tests used non-deterministic dates, xUnit assertions and non-`Given_When_Then` names, while the requested handler tests were absent.

**Skill change:** None; the current skill already required the repository-first checks, deterministic tests, complete fakes, FluentAssertions 7.x and scoped coverage reporting.

**Verified result:** `dotnet build` passed with 0 warnings and 0 errors; `dotnet test tests\InternHub.Tests\InternHub.Tests.csproj --collect:"XPlat Code Coverage" --results-directory TestResults --no-build` passed 90/90 tests. Cobertura reported 100% line and branch coverage for all four requested classes with no uncovered branches.

### Interaction 7 - Generate Update/Delete Offer handler unit tests

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt:** Generate meaningful isolated xUnit + FluentAssertions 7.x tests for `UpdateOfferHandler` and `DeleteOfferHandler`, including success, validation, not-found, stale-version and repository concurrency-conflict paths, using hand-written fakes.

**Observed problem:** The existing Offer fake did not expose update/delete call counts or a way to simulate `OfferConcurrencyException`; the requested handler tests did not exist.

**Skill change:** None; the current skill already required exact contracts, complete fakes, deterministic IDs/dates, Result assertions and scoped coverage.

**Verified result:** `dotnet build` passed with 0 warnings and 0 errors; `dotnet test tests\InternHub.Tests\InternHub.Tests.csproj --collect:"XPlat Code Coverage" --results-directory TestResults --no-build` passed 98/98 tests. Cobertura reported 100% line and branch coverage for both requested handlers with no uncovered lines.

All Person 3 interactions above used **Copilot following SKILL.md instructions**, not native **Claude Skill execution**.

## Person 4 — Offer Update/Delete, concurrency, integration testing and Postman

**Date:** 28 October 2026  
**Tools used:** GitHub Copilot Agent guided by the project's Claude-format `SKILL.md` files; ChatGPT for drafting the integration-testing skill  
**Scope:** `UpdateOffer`, `DeleteOffer`, optimistic concurrency, integration-test infrastructure and Postman scenarios

### Interaction 1 - Refactor Offer Update/Delete to CQRS

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/cqrs/SKILL.md`

**Request (summary):** Refactor only `UpdateOffer` and `DeleteOffer` to commands and handlers behind interfaces, reuse `Result<T>`, register handlers in DI, preserve repository contracts and map `/api/v1` routes.

**Result:** Added commands, handler interfaces, handlers and CQRS endpoints; registered both handlers in `OffersFeaturesExtensions`; removed the two legacy endpoint implementations. `UpdateOffer` accepts `Version` and maps success, validation, not-found and stale-version conflict outcomes. `dotnet build` passed with 0 warnings and 0 errors.

### Interaction 2 - Add optimistic concurrency handling

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/efcore-sqlite-repositories/SKILL.md`

**Request (summary):** Handle EF Core concurrency failures without coupling `UpdateOfferHandler` to EF Core, preserve `Offer.Version` as a concurrency token, prevent stale overwrites and retain not-found behavior.

**Observed problem:** The existing bool `IOfferRepository.Update` contract could not distinguish not-found from conflict, and the skill required `DbUpdateConcurrencyException` to escape into the handler, conflicting with the EF-independence requirement.

**Skill improvement:** The EF repository now translates `DbUpdateConcurrencyException` into domain-level `OfferConcurrencyException`; `UpdateOfferHandler` catches that exception and returns `Result.Conflict`, which maps to HTTP 409. `Offer.Version` remains configured with `.IsConcurrencyToken()`, and the repository interface remains unchanged.

**Verified result:** `dotnet build` passed with 0 warnings and 0 errors; `dotnet test` passed 90/90 tests. No unrelated slices or tests were changed in this interaction.

### Interaction 3 - Generate Update/Delete handler unit tests

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Request (summary):** Generate isolated xUnit and FluentAssertions 7.x tests using a hand-written fake for successful update/delete, validation, not-found, stale-version and concurrency-conflict paths.

**Observed problem:** The existing Offer fake did not expose repository call counts or simulate `OfferConcurrencyException`; handler tests were absent.

**Skill change:** None; the existing skill already specified exact contracts, complete fakes, deterministic inputs, Result assertions and scoped coverage.

**Verified result:** `dotnet build` passed with 0 warnings and 0 errors; `dotnet test tests\InternHub.Tests\InternHub.Tests.csproj` passed **98/98 tests**. Cobertura reported 100% line and branch coverage for both handlers.

### Interaction 4 - Create integration-testing skill and SQLite WebApplicationFactory infrastructure

**Tool:** ChatGPT (skill content), GitHub Copilot Agent (implementation)

**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Request (summary):** Define and implement reusable integration-test infrastructure using `WebApplicationFactory<Program>`, a fresh open SQLite in-memory connection per test, DI replacement of the production DbContext and fresh-context database assertions.

**Skill content:** ChatGPT created the integration-testing skill guidance after inspecting the project conventions and contracts. It requires real HTTP requests, SQLite relational behavior, isolated connections, response/status assertions, persisted-state checks and deterministic stale-version scenarios. This was not a Claude execution.

**Result:** Copilot added `InternHubSqliteFactory` and the requested integration-test folder structure without modifying production code or existing integration tests.

### Interaction 5 - Generate Update/Delete Offer integration tests

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/integration-testing/SKILL.md`

**Request (summary):** Add real HTTP tests for successful, invalid, missing and stale-version Offer updates plus successful and missing Offer deletion, asserting statuses, response bodies and SQLite state.

**Result:** Added isolated SQLite `WebApplicationFactory` tests. Successful updates verify persisted fields and exactly one version increment; failed updates verify unchanged state; stale updates verify HTTP 409 and preservation of the newer data.

**Verified result:** `dotnet build` passed with 0 warnings and 0 errors; `dotnet test` passed **104/104 tests**.

### Interaction 6 - Update Postman Offer scenarios

**Tool:** GitHub Copilot Agent  
**Request (summary):** Extend the existing collection with UpdateOffer 204/400/404/409 and DeleteOffer 204/404 scenarios, reuse `offerId` and version variables, and order the stale-version conflict before deletion.

**Result:** Added Offer setup and ordered Offer write requests using `/api/{{apiVersion}}/offers`, saved and reused `offerId`, `offerVersion` and `staleOfferVersion`, and added status/body assertions. The collection JSON was validated after modification.

**Verified result:** Final Postman execution passed **48/48 assertions**.

**Claude skills usage:** Person 4 used the project's Claude-format skill instructions (`cqrs`, `efcore-sqlite-repositories`, `unit-testing`, `integration-testing`) through Copilot Agent. The integration-testing skill content was drafted with ChatGPT. Native Claude Code execution has not been documented and must not be asserted without an actual run.

## Person 1 — Student Create/GetById/GetAll unit testing

| Prompt summary | Tool and skill | Observed problem | Skill change | Verified result |
|---|---|---|---|---|
| Generate meaningful xUnit + FluentAssertions 7.x tests for `CreateStudentHandler`, `GetStudentByIdHandler` and `GetStudentsHandler`; inspect contracts, use the existing fake, run build/test and measure scoped coverage. | GitHub Copilot Agent following `.claude/skills/unit-testing/SKILL.md` | The existing `FakeStudentRepository` could not simulate the Create handler's repository-conflict branch. | None; added only a configurable `AddResult` to the existing test fake. | `dotnet build` passed with 0 warnings and 0 errors; `dotnet test` passed 113/113 tests. Cobertura reported 100% line and branch coverage for all three requested handlers with no uncovered lines or paths. Existing Student domain tests cover constructor validation, normalization, update atomicity and candidacy collection behavior. |

## Person 1 — Student duplicate-email conflict handling

| Prompt summary | Skills used | Observed problem | Skill improvement | Verified before/after result |
|---|---|---|---|---|
| Fix duplicate Student email handling so SQLite uniqueness conflicts become `Result.Conflict` and HTTP 409; preserve the repository contract and verify SQLite state. | `.claude/skills/efcore-sqlite-repositories/SKILL.md`, `.claude/skills/cqrs/SKILL.md`, `.claude/skills/integration-testing/SKILL.md` | Before the fix, `EfCoreStudentRepository.Add` let `DbUpdateException` escape from SQLite's unique `Students.Email` index; the integration test observed the unhandled exception instead of HTTP 409. | EF persistence guidance now requires translating only SQLite error code 19 identifying `Students.Email` into `false`, while unrelated database errors propagate. Integration guidance now requires duplicate-resource tests to assert 409 and unchanged state rather than codifying a 500/exception defect. | Before: duplicate request propagated `DbUpdateException`; after: duplicate request returns HTTP 409 with conflict text, and fresh SQLite state contains only the original student. `dotnet build` passed with 0 warnings/errors; `dotnet test` passed 120/120. |

## Person 2 — Student Update/Delete integration tests

| Prompt summary | Skill used | Observed problem | Skill change | Verified result |
|---|---|---|---|---|
| Add isolated SQLite HTTP integration tests for UpdateStudent and DeleteStudent, including duplicate-email update investigation. | `.claude/skills/integration-testing/SKILL.md`, `.claude/skills/efcore-sqlite-repositories/SKILL.md` | `EfCoreStudentRepository.Update` propagates `DbUpdateException` for a duplicate `Students.Email` update, so no HTTP 409 is produced. | None; production changes were outside this test-only task. The intended 409 regression remains explicitly skipped rather than treating the exception as correct behavior. | Five required scenarios pass with fresh SQLite state. Full suite: 126 passed, 1 skipped. The unskipped duplicate-email regression was independently verified to fail with SQLite error 19; the suite was restored to a passing state. |

## Person 2 — Fix Student duplicate-email update conflict

| Prompt summary | Skills used | Observed problem | Skill improvement | Verified result |
|---|---|---|---|---|
| Fix `UpdateStudent` duplicate-email handling so SQLite uniqueness conflicts produce `Result.Conflict` and HTTP 409 while preserving both database rows. | `.claude/skills/efcore-sqlite-repositories/SKILL.md`, `.claude/skills/cqrs/SKILL.md`, `.claude/skills/integration-testing/SKILL.md` | Before the fix, `EfCoreStudentRepository.Update` propagated SQLite error 19 (`UNIQUE constraint failed: Students.Email`) and `UpdateStudentHandler` ignored the repository's boolean result. | EF persistence guidance now requires the same narrow SQLite `Students.Email` translation for both `Add` and `Update`, with fresh-context verification that rejected updates preserve persisted rows. | Enabled duplicate-email integration test now passes with HTTP 409 and unchanged students. Added a handler conflict unit test. `dotnet build` passed with 0 warnings/errors; `dotnet test` passed 128/128 with 0 skipped. |

## Person 2 — Student–InternshipCandidacy persistence integrity

| Prompt summary | Skills read | Result |
|---|---|---|
| Complete persistence integrity verification for Student–InternshipCandidacy using the existing SQLite integration factory, without adding an API endpoint or changing production code. | `.claude/skills/integration-testing/SKILL.md`, `.claude/skills/efcore-sqlite-repositories/SKILL.md` | Added three focused integration tests using fresh factory databases and fresh DbContext scopes: valid Student/Offer/InternshipCandidacy persistence and reload, Student deletion cascading to candidacies while preserving the Offer, and SQLite foreign-key rejection for a candidacy referencing a missing Student. Tests use existing domain constructors and repository contracts, Given/When/Then names, AAA sections, xUnit and FluentAssertions 7.x. |

## Postman — Student duplicate-email update conflict

| Prompt summary | Result |
|---|---|
| Add a repeatable Postman scenario for updating one Student with another Student's email, without changing existing requests or assertions. | Added a scenario before `DeleteStudent` that creates two students with GUID-generated email addresses, updates the second with the first email, asserts HTTP 409, and GETs both IDs to verify their original names and emails remain unchanged. All requests use `/api/{{apiVersion}}/students`; generated emails prevent conflicts with prior collection runs. The collection JSON was validated successfully. |
