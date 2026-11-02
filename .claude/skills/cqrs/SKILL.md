---
name: cqrs
description: Generates and converts InternHub vertical slices to CQRS (Commands, Queries, handlers behind interfaces, Result-based outcomes, minimal API endpoints on a shared /api/v1 group). Use when asked to create, convert or regenerate a use case (Create/Update/Delete/GetById/GetAll) for Student, Offer, InternshipCandidacy or any future entity under src/InternHub.Api/Features. Do not use it for persistence (efcore-sqlite-repositories skill), tests (unit-testing / integration-testing skills) or reviews (code-review skill).
---

# CQRS skill — InternHub

You generate production code for one vertical slice at a time:

```
HTTP Endpoint -> Handler Interface -> Handler -> Repository Interface -> Persistence
```

You never edit generated code by hand. If the output is wrong, the fix goes into this file, then the slice is regenerated.

## 1. Before writing anything — read the real code

Never guess names, routes or contracts. Before generating, open and read:

1. `src/InternHub.Api/Program.cs`: how services are registered and how routes are grouped.
2. The existing slice folder under `Features/<Aggregate>/<UseCase>/` (Sprint 1 version): request/response records, endpoint, current behavior.
3. The Domain entity (`Domain/<Aggregate>/`): public constructor/factory, update methods, how invalid data is reported (exception or result).
4. The repository interface (`IStudentRepository`, `IOfferRepository`). Use it exactly as it is; never change it. Mirror its sync/async style (in Sprint 1 it is synchronous and returns `bool` / `T?`).
5. `Features/Shared/` (Result type, HTTP mapping, route group). If it exists, reuse it unchanged. Generate it only when it does not exist.

InternHub specifics (verify, do not assume they never change):
- `Student` is created with `new Student(id, fullName, email)` and normalizes values.
- `Offer` is created with `new Offer(title, companyName, requirements, deadline, maximumAvailableSlots)`, starts at `Version = 0`, and `Offer.Update(...)` increments `Version` by 1.
- Dates are `DateTime` (UTC).

If something you need does not exist, stop and report what is missing instead of inventing it.

## 2. Classification

| Use case | Type |
|---|---|
| Create, Update, Delete | Command |
| GetById, GetAll | Query |

Queries never modify state.

## 3. Folder layout and naming

One folder per use case:

```
src/InternHub.Api/Features/
├── Shared/
│   ├── Result.cs
│   ├── ResultHttpMapping.cs
│   └── ApiRoutes.cs                  (the single /api/v1 group)
└── <Aggregate>/
    ├── <Aggregate>FeaturesExtensions.cs   (DI registration for the aggregate)
    └── <UseCase>/
        ├── <UseCase>Command.cs       (or <UseCase>Query.cs)
        ├── I<UseCase>Handler.cs
        ├── <UseCase>Handler.cs
        ├── <UseCase>Request.cs       (only when the HTTP body needs a contract)
        ├── <UseCase>Response.cs      (only when a body is returned)
        └── Endpoint.cs
```

Names:
- Commands: `CreateStudentCommand`, `UpdateOfferCommand`, `DeleteOfferCommand`
- Queries: `GetStudentByIdQuery`, `GetStudentsQuery`, `GetOffersQuery`
- Handlers: `CreateStudentHandler` with interface `ICreateStudentHandler`
- Namespace = folder path, e.g. `InternHub.Api.Features.Students.CreateStudent`
- Each handler has one public method, `Handle`, returning `Result<T>` (use `Task<Result<T>> HandleAsync` only if the repository interface is async).
- Commands and queries are immutable `sealed record`s; classes are `sealed`.

## 4. Rules

1. No mediator library (no MediatR or similar). Handlers are called directly through their interface.
2. Every handler is behind a small interface and resolved through DI (`AddScoped`). Registration lives in `<Aggregate>FeaturesExtensions.cs`, called once from `Program.cs`.
3. Handlers contain the application behavior: orchestration, repository calls, translation of outcomes to Result.
4. Domain invariants stay in the Domain. Do not duplicate them in the handler. If the Domain signals invalid data by exception, the handler catches only that exception type and returns `Result.Validation`. Never catch in endpoints.
5. Handlers depend only on repository interfaces. Never on `DbContext`, EF Core or SQLite.
6. Endpoints only: bind the request, build the Command/Query, call the handler, map the Result to HTTP. No business rules.
7. Expected outcomes (validation, not found, conflict) are returned as Result, never thrown.
8. Request/Response records are the API contract and are kept unchanged unless the task explicitly changes them. Persistence changes never alter handler interfaces or endpoint contracts.
9. Do not touch slices you were not asked to generate. When you generate a slice, remove the legacy Sprint 1 files that implemented the same slice (delete a legacy file once it has no content left). Never remove legacy files of other slices.
10. Do not generate tests, EF code or Postman here.

## 5. Result model

Categories: `Success`, `Validation`, `NotFound`, `Conflict`. A failure carries a message; Validation may carry several.

HTTP mapping (implemented once in `Features/Shared/ResultHttpMapping.cs`, used by every endpoint):

| Outcome | Status |
|---|---|
| Create success | 201 Created + `Location: /api/v1/<resource>/{id}` |
| Read success | 200 OK |
| Update / Delete success | 204 No Content |
| Validation | 400 Bad Request |
| NotFound | 404 Not Found |
| Conflict | 409 Conflict |

`Conflict` is produced only where a real conflict exists (stale `Offer.Version`, concurrency conflict, duplicate resource). Never replace 400 or 404 with 409, and never add a Conflict path to a use case without a conflict rule (e.g. Student Update/Delete). `UpdateOfferHandler` returns `Conflict` when the request `Version` differs from the stored one.

## 6. Routes

- All endpoints map on one shared group: `app.MapGroup("/api/v1")` defined in `Features/Shared/ApiRoutes.cs`.
- Slices map relative routes only (`"/students"`, `"/students/{id:guid}"`). Never repeat `"/api/v1"` in a slice, except to build the `Location` value, which comes from a shared helper.

| Verb | Route |
|---|---|
| POST | `/api/v1/<resources>` |
| GET | `/api/v1/<resources>/{id}` |
| GET | `/api/v1/<resources>` |
| PUT | `/api/v1/<resources>/{id}` |
| DELETE | `/api/v1/<resources>/{id}` |

## 7. Identifiers

- New entities use `Guid.NewGuid()`. Never `new Guid()`. `Guid.Empty` is never a valid generated id.
- Create requests do not contain an id. The id is generated in the handler (when the Domain constructor takes an id, as `Student` does) or inside the Domain (when it generates its own, as `Offer` does).
- Update/Delete/GetById take the id from the route, never from the body.

## 8. Example of expected output

`Features/Shared/Result.cs`

```csharp
namespace InternHub.Api.Features.Shared;

public enum ResultStatus { Success, Validation, NotFound, Conflict }

public sealed class Result<T>
{
    private Result(ResultStatus status, T? value, string? error)
    {
        Status = status;
        Value = value;
        Error = error;
    }

    public ResultStatus Status { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, null);
    public static Result<T> Validation(string error) => new(ResultStatus.Validation, default, error);
    public static Result<T> NotFound(string error) => new(ResultStatus.NotFound, default, error);
    public static Result<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
}
```

`Features/Shared/ResultHttpMapping.cs`

```csharp
namespace InternHub.Api.Features.Shared;

public static class ResultHttpMapping
{
    public static IResult ToFailure<T>(this Result<T> result) => result.Status switch
    {
        ResultStatus.Validation => Results.BadRequest(result.Error),
        ResultStatus.NotFound => Results.NotFound(result.Error),
        ResultStatus.Conflict => Results.Conflict(result.Error),
        _ => throw new InvalidOperationException("Success is not a failure.")
    };
}
```

`Features/Students/CreateStudent/CreateStudentCommand.cs`

```csharp
namespace InternHub.Api.Features.Students.CreateStudent;

public sealed record CreateStudentCommand(string FullName, string Email);
```

`Features/Students/CreateStudent/ICreateStudentHandler.cs`

```csharp
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.CreateStudent;

public interface ICreateStudentHandler
{
    Result<CreateStudentResponse> Handle(CreateStudentCommand command);
}
```

`Features/Students/CreateStudent/CreateStudentHandler.cs`

```csharp
using InternHub.Api.Domain.Students;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.CreateStudent;

public sealed class CreateStudentHandler(IStudentRepository students) : ICreateStudentHandler
{
    public Result<CreateStudentResponse> Handle(CreateStudentCommand command)
    {
        Student student;

        try
        {
            student = new Student(Guid.NewGuid(), command.FullName, command.Email);
        }
        catch (ArgumentException exception)
        {
            return Result<CreateStudentResponse>.Validation(exception.Message);
        }

        students.Add(student);

        return Result<CreateStudentResponse>.Success(new CreateStudentResponse(student.Id));
    }
}
```

`Features/Students/CreateStudent/Endpoint.cs`

```csharp
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.CreateStudent;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/students", (CreateStudentRequest request, ICreateStudentHandler handler) =>
        {
            var result = handler.Handle(new CreateStudentCommand(request.FullName, request.Email));

            return result.Status == ResultStatus.Success
                ? Results.Created($"/api/v1/students/{result.Value!.Id}", result.Value)
                : result.ToFailure();
        });
}
```

`Features/Students/StudentFeaturesExtensions.cs`

```csharp
using InternHub.Api.Features.Students.CreateStudent;

namespace InternHub.Api.Features.Students;

public static class StudentFeaturesExtensions
{
    public static IServiceCollection AddStudentFeatures(this IServiceCollection services)
    {
        services.AddScoped<ICreateStudentHandler, CreateStudentHandler>();

        return services;
    }
}
```

## 9. Self-check before finishing

- [ ] Real names read from the code (domain constructors, repository methods, request/response records).
- [ ] Each use case has its own folder, command/query, handler interface and handler.
- [ ] Handlers registered in DI; no mediator package added.
- [ ] Queries do not call `Add`, `Update` or `Delete`.
- [ ] No exceptions escape handlers for expected outcomes; no try/catch in endpoints.
- [ ] Endpoints contain no business rules and use `ResultHttpMapping`.
- [ ] Routes live on the shared `/api/v1` group; `Location` uses `/api/v1/...`.
- [ ] 400 / 404 / 409 mapping respected; no artificial 409.
- [ ] IDs come from `Guid.NewGuid()`; no `new Guid()`; create requests carry no id.
- [ ] Repository interfaces and request/response contracts unchanged.
- [ ] Legacy Sprint 1 files of the generated slice removed; other slices untouched.
- [ ] `dotnet build` succeeds.

## 10. Report after generating

End with a short report:
1. Files created, changed and deleted.
2. A table `Use case | Type | Handler | Route | Statuses mapped`.
3. Any production problem noticed (file, expected vs. actual) and any assumption made.


## 11. Sprint 3 — InternshipCandidacy slices (override of rules 2, 5 and 6 for these slices)

- Folder: `Features/InternshipCandidacies/<UseCase>/`. Namespace = folder path.
- Registration: one public sealed class `<Name>Module : IFeatureModule` with a public parameterless
  constructor. `RegisterServices` does `AddScoped<I...Handler, ...Handler>()`; `MapEndpoints` calls
  `<UseCase>.Endpoint.Map(group)`. Do NOT create `<Aggregate>FeaturesExtensions` and do NOT edit
  `Program.cs`: modules are discovered automatically.
- Routes are relative to the shared group: `POST /candidacies`,
  `GET /students/{studentId:guid}/candidacies`.
- Use `IInternshipCandidacyRepository`, `IStudentRepository`, `IOfferRepository` exactly as they are.
- Time: inject `TimeProvider` and use `timeProvider.GetUtcNow().UtcDateTime`. Never `DateTime.UtcNow`.
- Create: `CreateInternshipCandidacyRequest(StudentId, OfferId)` is separate from the Command.
  The handler generates `Guid.NewGuid()`, sets `ApplicationDate` from TimeProvider and status
  `Submitted`. Success -> 201 with `Location: /api/v1/candidacies/{id}`.
- Result mapping for Apply: empty ids -> Validation; missing student/offer -> NotFound;
  closed offer, deadline <= now, duplicate active candidacy for student+offer, or 5 active
  candidacies -> Conflict (409). These are real business conflicts and are allowed.
- Query (Get student candidacies): read-only; 404 if the student does not exist; items contain
  candidacy id, offer id, offer title, company name, application date and status; ordered by
  `ApplicationDate` descending, then `Id`.
- `Features/Shared` already exists: reuse it, never regenerate it.



## 12. Sprint 3 — InternshipCandidacy status transitions and decision slices (Person 4)

### 12.1 Domain: `Domain/InternshipCandidacies/InternshipCandidacy.cs`

Extend the existing entity; never rewrite it. Keep the existing constructor, properties and
private setters unchanged (P1's handler, P3's EF mapping and existing tests depend on them).
The Domain must not reference `Features.Shared` or `Result<T>`.

Add:
- `public bool IsActive => IsActiveStatus(Status);` (expression-bodied, no setter, so EF does not map it)
- `public static bool IsActiveStatus(InternshipCandidacyStatus status)`: true only for
  `Submitted`, `Shortlisted`, `Accepted`, `Waitlisted`. This is the single definition of "active".
- Transition methods that return `bool`: `true` if the status changed, `false` if the transition
  is not allowed (status stays unchanged). They never throw for a forbidden transition.

| Method | Allowed from | Result |
|---|---|---|
| `Shortlist()` | `Submitted` | `Shortlisted` |
| `Accept(bool hasAvailableSlot)` | `Submitted`, `Shortlisted` | `Accepted` if `hasAvailableSlot`, otherwise `Waitlisted` |
| `Reject()` | `Submitted`, `Shortlisted` | `Rejected` |
| `Withdraw()` | `Submitted`, `Shortlisted` | `Withdrawn` |

`Accepted`, `Rejected`, `Waitlisted`, `Withdrawn` are final in Sprint 3 (no waitlist promotion).
The Domain decides Accepted vs Waitlisted; it receives only whether a slot is available.
Do not add new persisted properties (e.g. `DecidedAt`); EF configuration is owned by Person 3.

Example of expected output:

```csharp
public bool IsActive => IsActiveStatus(Status);

public static bool IsActiveStatus(InternshipCandidacyStatus status) =>
    status is InternshipCandidacyStatus.Submitted
        or InternshipCandidacyStatus.Shortlisted
        or InternshipCandidacyStatus.Accepted
        or InternshipCandidacyStatus.Waitlisted;

public bool Shortlist()
{
    if (Status != InternshipCandidacyStatus.Submitted)
        return false;

    Status = InternshipCandidacyStatus.Shortlisted;
    return true;
}
```

### 12.2 Slice `DecideInternshipCandidacy` (Command)

- Folder `Features/InternshipCandidacies/DecideInternshipCandidacy/`.
- Route: `POST /candidacies/{id:guid}/decision` (relative to the shared group). Id comes from the route.
- `DecideInternshipCandidacyRequest(string? Action)`; `DecideInternshipCandidacyCommand(Guid CandidacyId, string? Action)`.
- `CandidacyDecision` enum in the slice folder: `Shortlist`, `Accept`, `Reject`.
- Handler order:
  1. Parse `Action` case-insensitively into `CandidacyDecision`. Null, empty, unknown or numeric
     values (check `Enum.IsDefined` after `TryParse`) -> `Validation`.
  2. `candidacies.GetById(id)` is null -> `NotFound("Candidacy {id} not found.")`.
  3. For `Accept` only: load the offer with `offers.GetById(candidacy.OfferId)`; null -> `NotFound`.
     `hasAvailableSlot = candidacies.CountAcceptedForOffer(offer.Id) < offer.MaximumAvailableSlots`.
  4. Call the Domain method. `false` -> `Conflict("Candidacy {id} cannot be {action} from status {status}.")`
     and do NOT call `Update`.
  5. `candidacies.Update(candidacy)`; `false` -> `NotFound`.
  6. `Success(new DecideInternshipCandidacyResponse(candidacy.Id, candidacy.Status))`.
- A closed offer does not block decisions (existing candidacies keep moving through evaluation).
- Endpoint: success -> `200 OK` with the response body; failures via `ToFailure()`.
- Sequential capacity check only; concurrent last-slot acceptance is out of Sprint 3 scope.
- The decision is always a human HTTP request; never call any AI service.

### 12.3 Slice `GetOfferCandidacies` (Query)

- Folder `Features/InternshipCandidacies/GetOfferCandidacies/`.
- Route: `GET /offers/{offerId:guid}/candidacies`. `GetOfferCandidaciesQuery(Guid OfferId)`.
- Offer missing -> `NotFound("Offer {offerId} not found.")`.
- Items: `GetOfferCandidaciesResponse(Guid Id, Guid StudentId, string StudentFullName, DateTime ApplicationDate, InternshipCandidacyStatus Status)`.
  Read the student name through `IStudentRepository` (verify the real property name on `Student`).
  Follow the same broken-reference convention as `GetStudentCandidaciesHandler`.
- Ordered by `ApplicationDate` descending, then `Id` (in the handler, do not rely on the repository order).
- Read-only: never call `Add` or `Update`. Success -> `200 OK` (empty list when there are no candidacies).

### 12.4 Registration

One module: `Features/InternshipCandidacies/CandidacyDecisionModule.cs`, public sealed, parameterless,
implementing `IFeatureModule`; registers both handlers with `AddScoped` and maps both endpoints.
Do not edit `Program.cs`, `CandidacyApplicationModule` or other people's slices.