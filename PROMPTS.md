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
