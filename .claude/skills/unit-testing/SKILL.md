---
name: unit-testing
description: Generate and regenerate meaningful xUnit unit tests for InternHub Domain entities and CQRS handlers, using FluentAssertions 7.x, after the CQRS and EF Core + SQLite stages. Use for a requested Domain type or vertical slice, not for HTTP/SQLite integration tests, production changes, persistence generation, or code review.
---

# Unit Testing Skill - InternHub

Generate **unit tests only** for a named Domain entity or CQRS vertical slice. Each teammate owns tests for their own slices; do not generate the entire project's tests in one request.

**Generation rule:** Do not manually patch generated production or test code. If generated tests are wrong, improve this `SKILL.md`, regenerate the affected tests, and re-run verification. This skill may change unit-test files and their unit-test-support fakes only; it must not change production code, dependencies, or integration tests.

## 1. Inspect the repository first (hard prerequisites)

Before generating any code, open and verify:

1. `tests/InternHub.Tests/InternHub.Tests.csproj`: xUnit, FluentAssertions, and existing test dependencies.
2. The actual target Domain type **or** target CQRS handler, its interface, and its Command/Query under `src/InternHub.Api/`.
3. The exact repository interface used by the handler, including *every* member and whether its methods are synchronous.
4. The real Result type (`Result<T>`, `ResultStatus`) and actual domain constructor, validation, nullability, and update behavior.
5. Existing unit tests, their classes, namespaces, and directories, to prevent duplicates and preserve meaningful cases.
6. Confirm that the CQRS and EF Core + SQLite stages have been applied as required by the sprint before starting its unit-testing stage.

**Never assume a single namespace or folder convention.** This project may have namespaces such as `InternHub.Domain.Students` and `InternHub.Api.Features.Offers`; use the actual declarations in each file. Never infer a class name from a file name without opening it.

**Stop and report a blocker without generating tests** if the requested target class, handler, repository interface, Result contract, or required earlier stage is missing or inconsistent. Do not invent symbols or create production code to unblock yourself.

**Dependency blocker:** The assignment requires **FluentAssertions 7.x** (not 8.x). Check its actual package reference in the test `.csproj`. If absent or incompatible, report the missing dependency and stop; do not silently install packages or edit `.csproj` from this skill. The team must add an agreed fixed 7.x version through its approved workflow before regeneration.

## 2. Test framework and naming

- Use **xUnit** and the installed **FluentAssertions 7.x** APIs.
- Class names: `<ClassUnderTest>Tests`, e.g. `OfferTests`, `CreateOfferHandlerTests`.
- Methods: `Given_<state>_When_<action>_Then_<expected>`.
- Use `[Fact]` for a single scenario; `[Theory]` plus `[InlineData]` for variations of the same behavior.
- Every test explicitly separates `// Arrange`, `// Act`, `// Assert` with a **blank line** between these sections.
- Cover one observable behavior per test; multiple assertions may support that same behavior.
- Do not use `if`, `for`, `foreach`, `while`, or `switch` within test methods.
- Prefer public behavior and resulting state over private implementation details.

## 3. Exact contracts: sync, Result, and nullable input

- Copy public signatures from the **real production code**. Current `IOfferRepository` / `IStudentRepository` contracts may be synchronous; do not assume otherwise.
- For synchronous repositories use the real synchronous handler call, e.g. `handler.Handle(query)`. **Never generate `HandleAsync` unless the target handler actually declares it.**
- Assert existing `Result<T>` members, such as `result.Status.Should().Be(ResultStatus.NotFound)`, **only if** the inspected Result implementation exposes them. Do not invent `IsSuccess`, `IsNotFound`, or other convenience members.
- When asserting `Result<T>.Error` from an `ArgumentException`, verify the stable domain message without depending on the runtime-generated parameter-name suffix. Prefer a meaningful substring assertion unless the repository explicitly defines the complete error string as a public contract.
- Match actual nullable signatures. If a command or Domain constructor accepts `string?`, test null, empty, whitespace, and other invalid/boundary values **where the implementation defines behavior**. Do not assume all strings are non-nullable.
- Do not infer validation rules that do not exist. A Domain exception can be asserted in a Domain test when that is its documented behavior; handler tests should verify how expected failures become Results.

## 4. Isolation, fakes, and deterministic input

- Unit tests run in memory and **must not** start `WebApplicationFactory<Program>`, use `IClassFixture` for an HTTP host, call APIs, open SQLite or EF Core databases, or contact files, networks, Ollama, or external services.
- `WebApplicationFactory` / HTTP / real database tests belong to the **integration-testing skill**. Keep existing integration tests unchanged.
- Use small **hand-written fake repositories** for handler isolation; do not add Moq, NSubstitute, or other mocking libraries.
- A fake must implement **every member of the exact interface** inspected in source. Do not leave `NotImplementedException` in any exercised path. Reuse a shared fake only if doing so does not introduce shared mutable state.
- Keep test-data seeding separate from fake operation counters: seed arranged state through a dedicated helper, or capture counter baselines after Arrange before asserting handler interactions.
- Create new entities, IDs, and repository state per test. Use `Guid.NewGuid()` for valid generated IDs; use `Guid.Empty` only when testing rejection.
- Use fixed explicit UTC dates for constructor/update scenarios, e.g. `new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc)`. Do not base expected values on `DateTime.UtcNow`/`DateTime.Now`, arbitrary sleeps, or elapsed wall time.
- When tested production behavior depends on the current time, use its existing injected `TimeProvider` / clock seam with a fake. If no controllable seam exists, **report the testability blocker**; do not modify production code with this skill.
- No test may depend on test execution order or another test's data.

## 5. Meaningful scenarios and coverage responsibility

For **the requested target only**, choose relevant cases from the real implementation:

- Domain: valid and invalid construction, null/empty/whitespace/boundary inputs, update invariants, initial state, version changes, and state transitions **when applicable**.
- Command handlers: successful operation, validation failure, real state conflict, and resulting repository state **where applicable**.
- Query handlers: found/not-found, empty/nonempty collections, correct response values, and no state mutation.
- Verify `Success`, `Validation`, `NotFound`, `Conflict` only for **reachable paths** of that handler. Do not invent a `409` branch in a read handler.
- Avoid redundant non-null-only assertions and tests that mirror the implementation without checking observable behavior.

**Coverage scope:** Generate and verify tests for the explicitly requested class/slice, then report which branches remain uncovered. The **team-level** target is 100% meaningful Domain and handler coverage across InternHub. One slice's tests cannot be claimed to achieve global 100%; other members cover their own slices. Do not exclude actual business logic merely to raise the percentage.

HTTP status codes, `Location` headers, real SQLite persistence, and HTTP response bodies are integration-test concerns.

## 6. Folder and namespace conventions

Follow the **actual repository layout first**. Existing tests may live under:

```text
tests/InternHub.Tests/Unit/Offers/OfferTests.cs
tests/InternHub.Tests/Unit/Students/StudentTest.cs
```

Do **not** unilaterally move existing files into a new `Unit/Domain/...` tree or create a parallel test class. For new handler tests, mirror the target slice **as closely as practical within the current unit-test structure**, for example:

```text
src/InternHub.Api/Features/Offers/CreateOffer/CreateOfferHandler.cs
  -> tests/InternHub.Tests/Unit/Offers/CreateOffer/CreateOfferHandlerTests.cs
```

Inspect real namespaces and use the matching test namespace convention. If an existing test class name is inconsistent with `<ClassUnderTest>Tests`, regenerate/rename it only when it belongs to the requested ownership scope and update references as needed. Preserve the tested behavior; never leave duplicate definitions.

## 7. Expected output examples (verify APIs before reuse)

**Domain test:**

```csharp
using FluentAssertions;
using InternHub.Api.Domain.Offers;
using Xunit;

namespace InternHub.Tests.Unit.Offers;

public sealed class OfferTests
{
    [Fact]
    public void Given_ValidOfferData_When_CreatingOffer_Then_StartsWithVersionZero()
    {
        // Arrange
        var deadline = new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var offer = new Offer("Backend Intern", "Example Co", "C#", deadline, 2);

        // Assert
        offer.Version.Should().Be(0);
    }
}
```

**Handler assertion pattern** (only when those signatures and members exist in the real code):

```csharp
// Act
var result = handler.Handle(new GetOfferByIdQuery(offerId));

// Assert
result.Status.Should().Be(ResultStatus.NotFound);
```

## 8. Self-check, execution, and report

Before finishing:

- [ ] Required earlier stages, actual classes/interfaces/Result and package references were verified; blockers reported rather than guessed.
- [ ] No production source, dependencies, integration tests, or other member's slices were changed.
- [ ] Namespaces, class names, signatures, sync/async, and nullability match the real code.
- [ ] xUnit + FluentAssertions 7.x; `Given_..._When_..._Then_...`; AAA with blank lines.
- [ ] One meaningful behavior per test; no flow-control logic in test bodies.
- [ ] Fakes completely implement real repository interfaces; no mocking package, shared mutable state, or external service.
- [ ] Dates/IDs are deterministic and meaningful, tests independent.
- [ ] Tests check relevant Domain rules and Result statuses without invented branches.
- [ ] No duplicate test classes or unrequested folder restructuring.

Run `dotnet test tests/InternHub.Tests/InternHub.Tests.csproj` after generating tests, and generate/report scoped coverage when tooling is available. If a test fails due to generated code: report the failure, revise **this skill**, regenerate, and rerun. Never silently edit generated test bodies.

End with: files created/changed, scenarios, test command results, actual coverage and uncovered branches (or why coverage was not measured), and prerequisites/blockers. Provide a short **truthful** `PROMPTS.md` log entry: prompt, tool and skill used, observed problem, skill change, reason, and **verified** before/after result. Never claim a Claude execution, successful regeneration, or coverage percentage that did not happen.
