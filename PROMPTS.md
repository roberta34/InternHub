# InternHub - PROMPTS.md

Short log of relevant Sprint 2 AI interactions. Each member adds their own entries.

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

## Skill improvement (required before submission)

**Pending:** Record one *real* failed generation, the exact change to a `SKILL.md` file, regeneration with Claude and the verified before/after result. Do not invent an iteration.

**Compliance note:** Assignment 2 requires Claude-generated production and test code through the team's skills. The Copilot runs above do not fulfill that requirement.

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

- Apply the shared EF Core + SQLite skill before the unit-testing stage, in the required order.
- Regenerate/rework Person 3 unit tests via the unit-testing skill and measure Domain/handler coverage; then run Offer integration tests using Person 4's shared infrastructure.
- Confirm the GitHub Actions workflow and coverage artifact on GitHub.

### Interaction 6 - Generate Person 3 unit tests after persistence integration

**Tool:** GitHub Copilot Agent  
**Skill:** `.claude/skills/unit-testing/SKILL.md`

**Prompt:** Generate/refactor meaningful xUnit + FluentAssertions 7.x tests for Offer, CreateOfferHandler, GetOfferByIdHandler and GetOffersHandler using deterministic inputs and a complete hand-written fake repository; verify the CQRS and EF Core + SQLite prerequisites, then run build, tests and Cobertura coverage.

**Observed problem:** The existing Offer tests used non-deterministic dates, xUnit assertions and non-`Given_When_Then` names, while the requested handler tests were absent.

**Skill change:** None; the current skill already required the repository-first checks, deterministic tests, complete fakes, FluentAssertions 7.x and scoped coverage reporting.

**Verified result:** `dotnet build` passed with 0 warnings and 0 errors; `dotnet test tests\InternHub.Tests\InternHub.Tests.csproj --collect:"XPlat Code Coverage" --results-directory TestResults --no-build` passed 90/90 tests. Cobertura reported 100% line and branch coverage for all four requested classes with no uncovered branches.
- The interactions above used **Copilot following SKILL.md instructions**, not native **Claude Skill execution**. They must not be represented as Claude runs. The assignment's explicit Claude-skill-generation requirement remains to be satisfied before submission.
