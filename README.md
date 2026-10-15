# InternHub - Sprint 1 Plan

## 1. Sprint Goal

Build the initial **domain foundation** of InternHub and expose two core resources through:

- Domain entities and relationships

- In-memory repositories

- Dependency Injection

- CRUD vertical slices

- Correct HTTP status codes

- Unit / integration-level verification where applicable

- Postman requests and minimal README instructions

The two CRUD resources for this sprint are:

- **Student**

- **Offer**

The **InternshipCandidacy** entity is introduced in the Domain layer to model the relationship between Student and Offer.

---

## 2. Scope of Sprint 1

This sprint is focused on the assignment requirements:

### Domain Model

The Domain layer must:

- use `Guid Id` identity;

- keep value objects immutable (`record`);

- model at least:

- one one-to-many relationship;

- one many-to-many relationship through a join entity;

- keep entities valid through constructors / methods;

- use `private set`;

- avoid public mutable collections;

- not depend on ASP.NET;

- live under `Domain/<Aggregate>/`.

For InternHub:

```text
Student 1 ---- * InternshipCandidacy * ---- 1 Offer
```

Therefore:

- one Student can have many InternshipCandidacies;

- one Offer can have many InternshipCandidacies;

- Student and Offer form a many-to-many relationship through InternshipCandidacy.

---

### In-Memory Repositories

The sprint uses two repository resources:

- `Student`

- `Offer`

For each resource:

- define the repository interface near the Domain type;

- implement the repository in `Infrastructure/`;

- use:

```csharp
ConcurrentDictionary<Guid, T>
```

- register the repository in DI as `Singleton`;

- reject duplicate IDs;

- return missing resources consistently;

- make `GetAll()` return a snapshot.

No EF Core and no file persistence are required in this sprint.

---

### Vertical Slices

CRUD operations are exposed as independent vertical slices.

Example structure:

```text
Features/
├── Students/
│   ├── CreateStudent/
│   ├── GetStudentById/
│   ├── GetStudents/
│   ├── UpdateStudent/
│   └── DeleteStudent/
│
└── Offers/
    ├── CreateOffer/
    ├── GetOfferById/
    ├── GetOffers/
    ├── UpdateOffer/
    └── DeleteOffer/
```

Each slice should contain its own:

- request model;

- response model;

- route / endpoint;

- `Map()` method.

Endpoints depend on repository **interfaces**, never directly on the in-memory implementation.

Business rules belong in Domain/Application code, not in endpoints.

Use `TypedResults` and the required status codes:

- `201 Created` + `Location`

- `200 OK`

- `204 No Content`

- `400 Bad Request`

- `404 Not Found`

---

## 3. Team Distribution

### Person 1 - Student Domain + Create / GetById

### Main responsibilities

#### Domain

Create the initial Student aggregate:

```text
Domain/
└── Students/
    ├── Student.cs
    └── IStudentRepository.cs
```

Responsibilities:

- define `Guid Id`;

- keep state valid through constructor / methods;

- use `private set`;

- avoid public mutable collections;

- define the Student repository interface next to the Domain type.

> The current project requirements do not fully define all Student Profile fields.

> The team should agree on the minimum Student fields before implementation instead of inventing unsupported profile attributes.

#### Repository

Implement:

```text
Infrastructure/
└── Repositories/
    └── InMemoryStudentRepository.cs
```

with:

```csharp
ConcurrentDictionary<Guid, Student>
```

Required operations:

- `Add`

- `GetById`

- `GetAll`

- `Update`

- `Delete`

#### Vertical slices

Implement:

```text
Features/
└── Students/
    ├── CreateStudent/
    └── GetStudentById/
```

Expected behavior:

```text
Create Student         -> 201 Created + Location
Get existing Student   -> 200 OK
Get missing Student    -> 404 Not Found
Duplicate Student Id   -> rejected consistently
```

### Testing / verification

Person 1 verifies:

- valid Student creation;

- invalid Student state is rejected;

- duplicate IDs are rejected;

- existing Student can be retrieved;

- missing Student returns 404.

### Secondary responsibility

**API contract / DTO consistency**

Person 1 checks that Student request / response contracts are consistent across Person 1 and Person 2 slices.

---

## 4. Person 2 - Student Remaining CRUD + InternshipCandidacy Relationship

### Main responsibilities

#### Vertical slices

Implement:

```text
Features/
└── Students/
    ├── GetStudents/
    ├── UpdateStudent/
    └── DeleteStudent/
```

Expected behavior:

```text
Get all Students       -> 200 OK
Update existing        -> 204 No Content
Update missing         -> 404 Not Found
Delete existing        -> 204 No Content
Delete missing         -> 404 Not Found
```

#### Domain relationship

Introduce the `InternshipCandidacy` entity:

```text
Domain/
└── InternshipCandidacies/
    └── InternshipCandidacy.cs
```

InternshipCandidacy connects Student and Offer.

Core data already supported by the InternHub requirements:

- `Id`

- `StudentId`

- `OfferId`

- `InternshipCandidacyDate`

- `Status`

Allowed statuses:

- `Submitted`

- `Shortlisted`

- `Accepted`

- `Rejected`

- `Waitlisted`

- `Withdrawn`

For Sprint 1, this entity is used mainly to establish the Domain relationship.

The complete Apply / Withdraw / Decide business flows are **not implemented yet** in this sprint.

### Repository behavior checks

Person 2 also verifies Student repository behavior for:

- `GetAll()` snapshot semantics;

- update behavior;

- delete behavior;

- consistent handling of missing resources.

### Testing / verification

Person 2 verifies:

- GetAll returns the current snapshot;

- update works for existing Students;

- missing update returns 404;

- delete works for existing Students;

- missing delete returns 404;

- Student ↔ InternshipCandidacy relationship is represented without exposing mutable collections.

### Secondary responsibility

**Domain integrity / relationships**

Person 2 checks that the Student-InternshipCandidacy-Offer relationship remains consistent with the InternHub domain model.

---

## 5. Person 3 - Offer Domain + Create / GetById

### Main responsibilities

#### Domain

Create the Offer aggregate:

```text
Domain/
└── Offers/
    ├── Offer.cs
    └── IOfferRepository.cs
```

Offer data required by the InternHub requirements includes:

- `Id`

- `Title`

- `CompanyName`

- `Requirements`

- `Deadline`

- `MaximumAvailableSlots`

- `IsClosed`

- `Version`

Responsibilities:

- keep Offer state valid;

- use `private set`;

- expose behavior through methods instead of public setters;

- define the Offer repository interface next to the Domain type;

- keep Domain independent from ASP.NET.

#### Repository

Implement:

```text
Infrastructure/
└── Repositories/
    └── InMemoryOfferRepository.cs
```

with:

```csharp
ConcurrentDictionary<Guid, Offer>
```

Required operations:

- `Add`

- `GetById`

- `GetAll`

- `Update`

- `Delete`

#### Vertical slices

Implement:

```text
Features/
└── Offers/
    ├── CreateOffer/
    └── GetOfferById/
```

Expected behavior:

```text
Create Offer         -> 201 Created + Location
Get existing Offer   -> 200 OK
Get missing Offer    -> 404 Not Found
Duplicate Offer Id   -> rejected consistently
```

### Testing / verification

Person 3 verifies:

- Offer cannot be created in an invalid state;

- required Offer data is validated;

- duplicate IDs are rejected;

- existing Offer can be retrieved;

- missing Offer returns 404.

### Secondary responsibility

**Infrastructure / Dependency Injection**

Person 3 ensures both repositories are registered correctly as `Singleton` services and that endpoints depend only on repository interfaces.

---

## 6. Person 4 - Offer Remaining CRUD + Recruiter Evaluation Preparation + Integration / Delivery

### Main responsibilities

#### Vertical slices

Implement:

```text
Features/
└── Offers/
    ├── GetOffers/
    ├── UpdateOffer/
    └── DeleteOffer/
```

Expected behavior:

```text
Get all Offers       -> 200 OK
Update existing      -> 204 No Content
Update missing       -> 404 Not Found
Delete existing      -> 204 No Content
Delete missing       -> 404 Not Found
```

#### Offer / InternshipCandidacy relationship

Verify the Offer side of the relationship:

```text
Offer 1 ---- * InternshipCandidacy
```

and make sure the Domain model supports:

```text
Student * ---- * Offer
          through
        InternshipCandidacy
```

without exposing mutable collections publicly.

#### Recruiter evaluation preparation

Prepare the shared contract and decision boundary required for the future recruiter InternshipCandidacy evaluation flow.

The intended future flow is:

```text
Recruiter selects InternshipCandidacy
        |
        v
Optional AI analysis
        |
        v
CV + Offer Requirements
        |
        v
matchedSkills
missingOrUnclearSkills
evidence
summary
        |
        v
Recruiter reviews the information
        |
        v
Recruiter decides manually
        |
   Accept / Reject
```

Sprint 1 responsibilities are limited to preparing the architecture and contracts for this flow.

Person 4 must ensure that:

- recruiter AI assistance is optional;
- the AI will compare a candidate CV with the requirements of the selected Offer;
- AI output is advisory information only;
- AI must not automatically shortlist a candidate;
- AI must not automatically accept a candidate;
- AI must not automatically reject a candidate;
- AI must not automatically assign `Waitlisted`;
- AI must not change `InternshipCandidacy.Status`;
- the future `DecideInternshipCandidacy` operation remains separate from AI analysis;
- Accept and Reject always require an explicit recruiter action;
- recruiter decisions remain possible even if AI is unavailable.

No live Ollama call or AI model execution is required in Sprint 1.

#### Integration

Person 4 performs the first full-team integration check:

- Student CRUD slices work together;
- Offer CRUD slices work together;
- both repositories resolve through DI;
- repository implementations are not referenced directly by endpoints;
- the application starts successfully;
- endpoint routes do not conflict;
- expected status codes are returned;
- the shared InternshipCandidacy and Offer contracts do not couple recruiter decisions to AI.

#### Recruiter AI Architecture Readiness

- [ ] Future recruiter AI analysis is documented as optional and advisory.
- [ ] AI analysis is separate from the future `DecideInternshipCandidacy` operation.
- [ ] AI output cannot change `InternshipCandidacy.Status`.
- [ ] Accept / Reject remain explicit recruiter actions.
- [ ] Recruiter decision logic can operate without AI.
- [ ] No live Ollama or AI model dependency is introduced in Sprint 1.

#### Delivery support

Prepare / coordinate:

- Postman collection structure;
- required requests for each implemented slice;
- minimal README run instructions;
- a short README note describing the future recruiter AI-assisted evaluation rule and the human-controlled final decision.

### Testing / verification

Person 4 verifies:

- Offer GetAll snapshot behavior;
- update / delete behavior;
- missing resources return 404;
- DI integration works;
- all endpoints map correctly;
- application starts and all CRUD flows can be exercised;
- no Sprint 1 code allows AI to mutate InternshipCandidacy status;
- the recruiter decision boundary is documented as human-controlled.

### Secondary responsibility

**Integration / delivery quality**

Person 4 coordinates the final Sprint 1 integration and delivery checks and keeps the future recruiter AI-assisted evaluation contract consistent with the human-controlled decision rule.

---

## 7. Workload Balance

The sprint is balanced by complexity rather than by number of files.

| Person | Main Domain Work | CRUD Slices | Extra Responsibility |

|---|---|---:|---|

| Person 1 | Student aggregate + repository | 2 | API / DTO consistency |

| Person 2 | InternshipCandidacy relationship | 3 | Domain integrity |

| Person 3 | Offer aggregate + repository | 2 | Infrastructure / DI |

| Person 4 | Offer relationship verification + recruiter evaluation contract preparation | 3 | Integration / delivery |

The team should still estimate the work during Sprint Planning.

If one task turns out to be significantly larger than expected, it should be split before implementation begins.

---

## 8. Integration Strategy

The team should define the shared Domain contracts before coding.

### Shared Student contract

Used by:

- Person 1

- Person 2

### Shared Offer contract

Used by:

- Person 3

- Person 4

### Shared InternshipCandidacy contract

Connects:

- Student

- Offer

The preferred workflow is:

```text
Agree Domain Contracts
        |
        v
Implement independently
        |
        v
Merge Student slices
        |
        v
Merge Offer slices
        |
        v
Integrate InternshipCandidacy relationship
        |
        v
Run full CRUD verification
```

Each person should work in a separate feature branch to reduce merge conflicts.

---

## 9. Sprint 1 Definition of Done

A Sprint 1 slice is Done only when all applicable criteria below are met.

### Domain

- [ ] Entity uses `Guid Id`.

- [ ] State is validated in constructors / methods.

- [ ] No public setters for mutable state.

- [ ] No public mutable collections.

- [ ] Value objects are immutable.

- [ ] Domain has no ASP.NET dependency.

- [ ] One-to-many relationship is represented.

- [ ] Many-to-many relationship is represented through `InternshipCandidacy`.

### Repository

- [ ] Repository interface is defined near the Domain type.

- [ ] In-memory implementation lives under `Infrastructure`.

- [ ] `ConcurrentDictionary<Guid, T>` is used.

- [ ] Repository is registered as `Singleton`.

- [ ] Duplicate IDs are rejected.

- [ ] Missing resources are handled consistently.

- [ ] `GetAll()` returns a snapshot.

### Vertical Slice

- [ ] Slice has its own folder.

- [ ] Slice contains request / response / route mapping.

- [ ] Endpoint depends on repository interface.

- [ ] No Domain business rules live inside the endpoint.

- [ ] Correct `TypedResults` are used.

- [ ] Correct status code is returned.

### Testing / Verification

- [ ] Happy path is verified.

- [ ] Missing-resource case is verified.

- [ ] At least one edge case is verified.

- [ ] Existing behavior is not broken.

- [ ] Application builds and starts successfully.

### Integration

- [ ] Student CRUD works end-to-end.

- [ ] Offer CRUD works end-to-end.

- [ ] Student-InternshipCandidacy-Offer relationship is represented correctly.

- [ ] DI resolves both repositories.

- [ ] No endpoint depends on concrete in-memory repository classes.

### Delivery

- [ ] Git repository is ready.

- [ ] Postman collection contains at least 3 requests per slice:

- happy path;

- 404 or 400;

- one additional edge case.

- [ ] README contains at least:

- how to run the API;

- how to import the Postman collection;

- any required setup command / note.

---

## 10. Out of Scope for Sprint 1

The following InternHub features should **not** be forced into this sprint unless the assignment changes:

- real database / EF Core;

- full Apply business flow;

- Withdraw InternshipCandidacy;

- recruiter Accept / Reject;

- slot concurrency behavior;

- Close Offer workflow;

- AI / Ollama integration;

- AI / ML evaluation;

- frontend application.

These remain part of the overall InternHub project, but Sprint 1 is specifically focused on **Domain + in-memory repositories + CRUD vertical slices**.

This means the general team plan where all members eventually work with frontend, backend, database, AI/ML, and testing still applies across the project, but not every technology needs to appear in every single sprint.
