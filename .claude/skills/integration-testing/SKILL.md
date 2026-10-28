---
name: integration-testing
description: Generates and regenerates InternHub ASP.NET Core integration tests using xUnit, WebApplicationFactory<Program>, real HTTP requests, and a fresh SQLite in-memory database per test. Use after CQRS, EF Core SQLite persistence, and unit-testing skills have been applied, when testing Student, Offer, or future vertical slices. Do not use for production CQRS/persistence changes, unit tests, or code review.
---

# Integration-testing skill — InternHub

You generate **integration-test code**, not production behavior. Follow the project architecture and the actual implemented API contracts. Every generated test must exercise HTTP endpoints and assert the resulting SQLite state.

If generation is incorrect, **improve this `SKILL.md`, then regenerate**. Do not manually patch generated production or test code. Do not claim a test passed unless it was executed successfully.

## 1. Before generating — inspect the repository

Read the real files; never invent an endpoint, DTO, field, service registration, or Result shape:

1. `src/InternHub.Api/Program.cs` — startup, `/api/v1` route group, `partial class Program`, `EnsureCreated()` behavior, and DI registration.
2. `src/InternHub.Api/Infrastructure/Persistence/` — `InternHubDbContext`, database-registration extension, EF model configurations, and repository implementations.
3. The requested `Features/<Aggregate>/<UseCase>/` folders — HTTP method, route, request/response, handler, and expected result mapping.
4. Relevant Domain models and repository contracts, especially `Offer.Version` and concurrency behavior.
5. `tests/InternHub.Tests/InternHub.Tests.csproj`, existing `Integration/` tests and fixtures, plus `.claude/skills/unit-testing/SKILL.md` for naming/assertion conventions.
6. The current shared `Result` and HTTP mapping, if relevant to failure assertions.

Reuse existing test packages and infrastructure when correct. Do not duplicate fixtures or add dependencies without a demonstrated need. If a required contract or dependency is missing, report it instead of guessing.

## 2. Scope and generation rules

- Generate only the integration fixture and test slices explicitly requested.
- Keep existing vertical slices and production endpoints unchanged. If a production defect is discovered, report the file and problem; fix production code only through its owning skill in the required order.
- Do not refactor unit tests, other members' slices, CI, README, or Postman as a side effect.
- Use **xUnit**, `WebApplicationFactory<Program>`, `HttpClient`, **EF Core + real SQLite**, and **FluentAssertions 7.*** when available in the project.
- Tests must run with `dotnet test` and require no running API, Ollama instance, network service, or other external dependency.
- No `UseInMemoryDatabase()` or mocked repository for HTTP integration tests. These would bypass real relational behavior.
- No mocking library. Do not use sleeps, retries, real-time assumptions, or test-order dependence to make tests pass.

## 3. Folder layout and naming

Mirror production use cases as closely as practical:

```text
tests/InternHub.Tests/
└── Integration/
    ├── Infrastructure/
    │   └── InternHubSqliteFactory.cs
    └── Offers/
        ├── UpdateOffer/
        │   └── UpdateOfferEndpointTests.cs
        └── DeleteOffer/
            └── DeleteOfferEndpointTests.cs
```

For other aggregates, replace `Offers` with their actual feature folder. Preserve a compatible existing project convention where already established.

- Test class: `<UseCase>EndpointTests` (or the consistent existing project convention).
- Test method: `Given_<state>_When_<action>_Then_<expected>`.
- Every test visibly separates **Arrange**, **Act**, and **Assert**, with blank lines between sections.
- One meaningful behavior per test. Use `[Theory]` for variations of the *same* behavior.
- Do not write `if`, `for`, `while`, or `switch` into test bodies to compute expected outcomes.
- Use `Guid.NewGuid()` for unique identifiers; never `new Guid()` or magic shared IDs.
- If production behavior is time-dependent, use an explicitly controllable clock; do not rely on current system time to determine the outcome.

## 4. A real, fresh SQLite database for each test

Each individual `[Fact]` / `[Theory]` case MUST have its **own factory instance** and **its own in-memory SQLite connection**:

1. Open a `Microsoft.Data.Sqlite.SqliteConnection` with `Data Source=:memory:` before starting the test host.
2. Keep this **same open connection** alive until the test and all its assertions complete; closing it destroys the in-memory database.
3. Override `WebApplicationFactory<Program>.ConfigureWebHost` and replace the **production EF Core DbContext options/configuration** in the test DI container with `UseSqlite(openConnection)`.
4. Ensure `Program.cs` and all request scopes resolve this test-configured `InternHubDbContext`, never the development/production database.
5. Create the schema for this test database after the test DI replacement. Reuse `Program.cs`'s `EnsureCreated()` startup behavior if it correctly creates the schema; otherwise initialize it explicitly once after host startup.
6. Dispose `HttpClient`, scopes, factory, and the SQLite connection after the test. No static connection, shared database, test-order dependency, or cross-test fixture that reuses state.
7. Use a **new DI scope and a new DbContext** for database assertions after HTTP requests, with `AsNoTracking()` where appropriate. Do not rely on previously tracked objects to prove persistence.

When replacing the EF registration, inspect the actual EF Core version and registration code. Remove relevant `DbContextOptions<InternHubDbContext>` **and** `IDbContextOptionsConfiguration<InternHubDbContext>` registrations if present before adding the SQLite registration. Verify the app cannot accidentally use the configured on-disk SQLite database.

Do not run integration tests in parallel against a shared database file. Separate factory + connection per test enables isolation even with xUnit parallel execution.

## 5. What every HTTP integration test asserts

Every test must assert all applicable parts of the complete behavior:

1. **HTTP status**: exact code, not only `IsSuccessStatusCode`.
2. **Response contract**: parse/assert relevant fields for `200`/`201`; assert `Location` for a created resource; assert meaningful error response for `400`/`404`/`409`; assert no response body for `204` where appropriate.
3. **SQLite state**: query through a fresh scope and verify inserted/changed/deleted/unchanged data, including `Version` where relevant.

At least **one happy-path and one failure-path test per use case**. Add a separate `409 Conflict` scenario only where an actual conflict rule exists. A passing status-only test is incomplete.

For a configured SQLite unique index such as `Students.Email`, test duplicate
creation through real HTTP and require the documented `409 Conflict` response,
a meaningful conflict body, and unchanged SQLite state. If the request instead
propagates `DbUpdateException` or returns `500`, record that as a production
defect; do not rewrite the test to expect the defect. The persistence
implementation must translate only the known unique-index violation, not swallow
unrelated database exceptions.

Use real calls such as `client.PutAsJsonAsync`, `client.DeleteAsync`, `client.GetAsync`, `client.PostAsJsonAsync` with the actual contract and `/api/v1` route. Use JSON helpers only for response shapes that the API really returns.

## 6. InternHub Offer Update / Delete scenarios

When asked to generate Person 4's tests, cover at least:

| Slice | Scenario | HTTP | Required SQLite assertion |
|---|---|---|---|
| UpdateOffer | Existing Offer, valid version and valid data | `204` | New values persist; `Version` increments exactly once |
| UpdateOffer | Missing Offer | `404` | No Offer inserted or changed |
| UpdateOffer | Invalid data | `400` | Existing Offer fields and `Version` unchanged |
| UpdateOffer | Stale `Offer.Version` | `409` | Latest fields and `Version` remain unchanged |
| DeleteOffer | Existing Offer | `204` | Offer no longer exists |
| DeleteOffer | Missing Offer | `404` | Database state unchanged |

For the stale-version case, use a **deterministic real-HTTP sequence**: create/read an Offer, perform one successful update with the current version, then send a second update with the *old* version. Assert `409`, its error body, and final persisted state via a fresh DbContext.

Also test a true persistence-level conflict if the code supports a deterministic interleaving using distinct DbContexts. Do not infer that a sequential stale-version test by itself proves the `DbUpdateConcurrencyException` catch path. Avoid timing-based concurrency tests.

The handler should map a stale version or a caught Offer concurrency failure to a Conflict Result; the endpoint maps it to `409`. Test the observable contract, not internal implementation calls.

## 7. Example of expected output

Example fixture pattern (adapt registration details and namespaces to **actual** project files):

`tests/InternHub.Tests/Integration/Infrastructure/InternHubSqliteFactory.cs`

```csharp
using InternHub.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternHub.Tests.Integration.Infrastructure;

public sealed class InternHubSqliteFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public InternHubSqliteFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<InternHubDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<InternHubDbContext>>();
            services.AddDbContext<InternHubDbContext>(options => options.UseSqlite(_connection));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
```

Example isolated test for a missing Offer (status + error body + database state):

```csharp
using System.Net;
using FluentAssertions;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InternHub.Tests.Integration.Offers.DeleteOffer;

public sealed class DeleteOfferEndpointTests
{
    [Fact]
    public async Task Given_MissingOffer_When_Deleting_Then_ReturnsNotFoundAndPreservesDatabase()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offerId = Guid.NewGuid();

        // Act
        using var response = await client.DeleteAsync($"/api/v1/offers/{offerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain(offerId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }
}
```

The example is a pattern, not authorization to invent DTO names, packages, or production behavior. The repository's actual response format determines whether to read a string, JSON payload, or `ProblemDetails`. Check this before generating real tests.

## 8. Self-check before finishing

- [ ] Production startup and EF registrations inspected before editing.
- [ ] Tests use actual request/response records and `/api/v1` routes.
- [ ] `WebApplicationFactory<Program>` uses real HTTP and the application's actual DI setup.
- [ ] Every test owns a fresh, open in-memory SQLite connection and disposes it.
- [ ] No test connects to on-disk development/production SQLite.
- [ ] Real relational constraints and concurrency are exercised (not EF InMemory provider).
- [ ] Status, response body where applicable, and fresh-context SQLite state are asserted.
- [ ] Happy path and failure path exist for each requested slice.
- [ ] Stale Offer update asserts `409` and preserves current data.
- [ ] Every method follows `Given_When_Then`; AAA sections are visible.
- [ ] No shared mutable state, live services, mocks, sleeps, or manually patched generated tests.
- [ ] `dotnet build` and `dotnet test` pass, or exact failures are reported.

## 9. Report after generation

Return a concise report containing:

1. Files added, changed, and deleted.
2. A table `Test | Endpoint | Expected HTTP | Database assertion`.
3. `dotnet build` and `dotnet test` outcomes, including passed/failed counts.
4. Actual defects, missing contracts, assumptions, or untested concurrency paths.
5. Any skill improvement (problem observed, change in `SKILL.md`, and improvement after regeneration) that should be logged in `PROMPTS.md`. Do **not** fabricate prompt history or outcomes.

Do not commit unless explicitly requested.
