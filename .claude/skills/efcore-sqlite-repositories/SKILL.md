---
name: efcore-sqlite-repositories
description: >
  Replaces the InternHub in-memory repositories with EF Core + SQLite implementations
  that satisfy the same IStudentRepository and IOfferRepository interfaces.
  Use this skill when asked to implement, regenerate or fix EfCoreStudentRepository,
  EfCoreOfferRepository, InternHubDbContext, or any persistence-layer configuration
  under src/InternHub.Api/Infrastructure/Persistence.
  Do NOT use it for CQRS slices (cqrs skill), tests (unit-testing / integration-testing
  skills), or code reviews (code-review skill).
  Run this skill AFTER the cqrs skill and BEFORE the unit-testing skill.
---

# EF Core + SQLite Repositories skill — InternHub

## 0. Skill position in the required execution order

```
1. cqrs                         <- already done (Person 1)
2. efcore-sqlite-repositories   <- THIS SKILL
3. unit-testing
4. integration-testing
5. code-review
```

Never run this skill before the CQRS skill output has been reviewed and merged.

---

## 1. Before writing anything — read the real code

Read the following files before generating any code. Never guess names or signatures.

| File | What to verify |
|---|---|
| `src/InternHub.Api/Domain/Students/Student.cs` | Constructor signature, property names, `_internshipCandidacies` backing field, `IReadOnlyCollection<InternshipCandidacy>` navigation |
| `src/InternHub.Api/Domain/Students/IStudentRepository.cs` | Exact method signatures — sync, returns `bool` / `Student?` / `IReadOnlyList<Student>` |
| `src/InternHub.Api/Domain/Offers/Offer.cs` | Constructor, `Version` property (starts at `0`, incremented by `Update`), `IReadOnlyCollection<InternshipCandidacy>` navigation |
| `src/InternHub.Api/Domain/Offers/IOfferRepository.cs` | Exact method signatures — sync, returns `bool` / `Offer?` / `IReadOnlyCollection<Offer>` |
| `src/InternHub.Api/Domain/InternshipCandidacies/InternshipCandidacy.cs` | Constructor, `StudentId`, `OfferId` foreign-key properties, `Status` enum type |
| `src/InternHub.Api/Domain/InternshipCandidacies/InternshipCandidacyStatus.cs` | Enum values |
| `src/InternHub.Api/Program.cs` | How `IStudentRepository` and `IOfferRepository` are currently registered; where to add `DbContext` and swap registrations |
| `src/InternHub.Api/Infrastructure/Repositories/InMemoryStudentRepository.cs` | Behavior to replicate per method |
| `src/InternHub.Api/Infrastructure/Repositories/InMemoryOfferRepository.cs` | Behavior to replicate per method |
| `src/InternHub.Api/InternHub.Api.csproj` | Current package references; add EF Core + SQLite packages only if not already present |

If any file is missing or a signature differs from what is documented here, stop and
report the discrepancy instead of guessing.

---

## 2. Entity facts (verify — do not assume)

### Student (`InternHub.Domain.Students` namespace)
- `sealed class`
- Properties: `Guid Id`, `string FullName`, `string Email`
- Navigation: `IReadOnlyCollection<InternshipCandidacy> InternshipCandidacies` backed by `private readonly List<InternshipCandidacy> _internshipCandidacies`
- Constructor: `Student(Guid id, string? fullName, string? email)` — guards against `Guid.Empty`

### Offer (`InternHub.Api.Domain.Offers` namespace)
- `class` (not sealed)
- Properties: `Guid Id`, `string Title`, `string CompanyName`, `string Requirements`, `DateTime Deadline`, `int MaximumAvailableSlots`, `bool IsClosed`, `int Version`
- Navigation: `IReadOnlyCollection<InternshipCandidacy> InternshipCandidacies` backed by `private readonly List<InternshipCandidacy> _internshipCandidacies`
- Constructor generates its own `Id` via `Guid.NewGuid()`; sets `Version = 0`
- `Update(...)` increments `Version++`

### InternshipCandidacy (`InternHub.Api.Domain.InternshipCandidacies` namespace)
- `sealed class`
- Properties: `Guid Id`, `Guid StudentId`, `Guid OfferId`, `DateTime ApplicationDate`, `InternshipCandidacyStatus Status`
- No navigation references back to `Student` or `Offer` (FK-only join entity)

### Relationship
```
Student  1 ---- * InternshipCandidacy * ---- 1  Offer
```

---

## 3. Repository interface contracts (do not change these)

```csharp
// IStudentRepository — InternHub.Domain.Students namespace
bool Add(Student student);
Student? GetById(Guid id);
IReadOnlyList<Student> GetAll();
bool Update(Student student);
bool Delete(Guid id);

// IOfferRepository — InternHub.Api.Domain.Offers namespace
bool Add(Offer offer);
Offer? GetById(Guid id);
IReadOnlyCollection<Offer> GetAll();
bool Update(Offer offer);
bool Delete(Guid id);
```

Both interfaces are **synchronous**. EF Core repository implementations call
`SaveChanges()` (not `SaveChangesAsync`). Do not change the interface signatures.

---

## 4. NuGet packages to add

Add to `src/InternHub.Api/InternHub.Api.csproj`:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite"
                  Version="9.*" />
```

`Microsoft.EntityFrameworkCore` is pulled in transitively.
Do not add `Microsoft.EntityFrameworkCore.Design` to the API project unless
running migrations from the command line; it is a build-time tool.

> **Version pin**: use `9.*` to stay compatible with .NET 10 until EF Core 10
> is released. Verify the latest stable `9.x` on NuGet before writing the version.

---

## 5. Folder layout

```
src/InternHub.Api/
+-- Infrastructure/
    +-- Repositories/
    |   +-- InMemoryStudentRepository.cs   <- keep; used only by in-memory DI registration
    |   +-- InMemoryOfferRepository.cs     <- keep; used only by in-memory DI registration
    +-- Persistence/
        +-- InternHubDbContext.cs
        +-- StudentConfiguration.cs
        +-- OfferConfiguration.cs
        +-- InternshipCandidacyConfiguration.cs
        +-- EfCoreStudentRepository.cs
        +-- EfCoreOfferRepository.cs
        +-- PersistenceExtensions.cs       <- AddPersistence(IServiceCollection, IConfiguration)
```

Do not create files outside this layout.
Do not delete the in-memory files; they remain for reference and test helpers.

---

## 6. Namespace conventions

| File | Namespace |
|---|---|
| `InternHubDbContext.cs` | `InternHub.Api.Infrastructure.Persistence` |
| `*Configuration.cs` | `InternHub.Api.Infrastructure.Persistence` |
| `EfCoreStudentRepository.cs` | `InternHub.Api.Infrastructure.Persistence` |
| `EfCoreOfferRepository.cs` | `InternHub.Api.Infrastructure.Persistence` |
| `PersistenceExtensions.cs` | `InternHub.Api.Infrastructure.Persistence` |

---

## 7. DbContext

```csharp
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class InternHubDbContext(DbContextOptions<InternHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<InternshipCandidacy> InternshipCandidacies => Set<InternshipCandidacy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new OfferConfiguration());
        modelBuilder.ApplyConfiguration(new InternshipCandidacyConfiguration());
    }
}
```

Rules:
- Primary constructor (`DbContextOptions` parameter) — no field injection.
- `DbSet<T>` properties use expression-body `=> Set<T>()`.
- `OnModelCreating` delegates entirely to `IEntityTypeConfiguration<T>` classes.
- No business logic in `DbContext`.

---

## 8. Entity configurations

### 8.1 StudentConfiguration

```csharp
using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(s => s.FullName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(s => s.Email)
               .HasMaxLength(320)
               .IsRequired();

        builder.HasIndex(s => s.Email)
               .IsUnique();

        // Map the private backing field for InternshipCandidacies
        builder.HasMany(s => s.InternshipCandidacies)
               .WithOne()
               .HasForeignKey(c => c.StudentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.InternshipCandidacies)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

**GUID persistence rule**: Store `Guid` as `string` in SQLite using `.HasConversion<string>()`.
Apply this to **every** `Guid` primary key and foreign key in all three configurations.
SQLite has no native UUID type; storing as `TEXT` (string) preserves round-trip fidelity.

**Private backing field rule**: `Student._internshipCandidacies` is `private readonly`.
EF Core must use `.UsePropertyAccessMode(PropertyAccessMode.Field)` on the navigation
so it can populate the field without a public setter.

### 8.2 OfferConfiguration

```csharp
using InternHub.Api.Domain.Offers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(o => o.Title)
               .IsRequired();

        builder.Property(o => o.CompanyName)
               .IsRequired();

        builder.Property(o => o.Requirements)
               .IsRequired();

        builder.Property(o => o.Deadline)
               .IsRequired();

        builder.Property(o => o.MaximumAvailableSlots)
               .IsRequired();

        builder.Property(o => o.IsClosed)
               .IsRequired();

        // Optimistic concurrency token — mapped to Offer.Version (int, managed by Domain)
        builder.Property(o => o.Version)
               .IsConcurrencyToken();

        // Map the private backing field for InternshipCandidacies
        builder.HasMany(o => o.InternshipCandidacies)
               .WithOne()
               .HasForeignKey(c => c.OfferId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.InternshipCandidacies)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
```

**Concurrency rule**: `Offer.Version` is the optimistic concurrency token.
Mark it with `.IsConcurrencyToken()`. EF Core will include `Version` in the `WHERE`
clause of every `UPDATE` and raise `DbUpdateConcurrencyException` when the stored
version differs from the tracked value.

Note: `.IsRowVersion()` is for SQL Server `rowversion` columns. Do NOT use it for SQLite.
For SQLite, `.IsConcurrencyToken()` alone is the correct annotation.

### 8.3 InternshipCandidacyConfiguration

```csharp
using InternHub.Api.Domain.InternshipCandidacies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class InternshipCandidacyConfiguration
    : IEntityTypeConfiguration<InternshipCandidacy>
{
    public void Configure(EntityTypeBuilder<InternshipCandidacy> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(c => c.StudentId)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(c => c.OfferId)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(c => c.ApplicationDate)
               .IsRequired();

        builder.Property(c => c.Status)
               .HasConversion<string>()
               .IsRequired();
    }
}
```

`InternshipCandidacyStatus` is stored as a string for readability. No navigation
properties are declared on `InternshipCandidacy` itself because the entity has none.

---

## 9. EfCoreStudentRepository

```csharp
using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class EfCoreStudentRepository(InternHubDbContext db) : IStudentRepository
{
    public bool Add(Student student)
    {
        db.Students.Add(student);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException exception)
            when (IsDuplicateStudentEmailConflict(exception))
        {
            return false;
        }

        return true;
    }

    private static bool IsDuplicateStudentEmailConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19
        } sqliteException
        && sqliteException.Message.Contains(
            "Students.Email",
            StringComparison.OrdinalIgnoreCase);

    public Student? GetById(Guid id)
    {
        return db.Students
                 .Include(s => s.InternshipCandidacies)
                 .FirstOrDefault(s => s.Id == id);
    }

    public IReadOnlyList<Student> GetAll()
    {
        return db.Students
                 .Include(s => s.InternshipCandidacies)
                 .ToList();
    }

    public bool Update(Student student)
    {
        var exists = db.Students.Any(s => s.Id == student.Id);
        if (!exists) return false;

        db.Students.Update(student);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException exception)
            when (IsDuplicateStudentEmailConflict(exception))
        {
            return false;
        }

        return true;
    }

    public bool Delete(Guid id)
    {
        var student = db.Students.FirstOrDefault(s => s.Id == id);
        if (student is null) return false;

        db.Students.Remove(student);
        db.SaveChanges();
        return true;
    }
}
```

When `StudentConfiguration` declares `Students.Email` as a unique index, `Add`
and `Update` must translate only the known SQLite unique-constraint violation for
that index into `false`. This preserves the unchanged `IStudentRepository`
contract so CreateStudent and UpdateStudent handlers can return
`Result.Conflict`, which the endpoints map to HTTP 409. Do not catch every
`DbUpdateException`, every SQLite error code 19, or an arbitrary message:
unrelated schema, connection, foreign-key, and other database failures must
still propagate instead of being misreported as a duplicate email. A rejected
update must leave the database row unchanged; callers verify this with a fresh
`DbContext`.

Rules:
- Constructor injects `InternHubDbContext` (not an interface); primary constructor syntax.
- `Include` loads `InternshipCandidacies` eagerly where needed.
- Returns `bool` exactly as `IStudentRepository` requires.
- Does **not** expose `DbContext` to callers; handlers never see it.

---

## 10. EfCoreOfferRepository

```csharp
using InternHub.Api.Domain.Offers;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class EfCoreOfferRepository(InternHubDbContext db) : IOfferRepository
{
    public bool Add(Offer offer)
    {
        db.Offers.Add(offer);
        db.SaveChanges();
        return true;
    }

    public Offer? GetById(Guid id)
    {
        return db.Offers
                 .Include(o => o.InternshipCandidacies)
                 .FirstOrDefault(o => o.Id == id);
    }

    public IReadOnlyCollection<Offer> GetAll()
    {
        return db.Offers
                 .Include(o => o.InternshipCandidacies)
                 .ToList();
    }

    public bool Update(Offer offer)
    {
        var exists = db.Offers.Any(o => o.Id == offer.Id);
        if (!exists) return false;

        db.Offers.Update(offer);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new OfferConcurrencyException(offer.Id, exception);
        }
        return true;
    }

    public bool Delete(Guid id)
    {
        var offer = db.Offers.FirstOrDefault(o => o.Id == id);
        if (offer is null) return false;

        db.Offers.Remove(offer);
        db.SaveChanges();
        return true;
    }
}
```

**`Update` catches `DbUpdateConcurrencyException` here and translates it to the
domain-level `OfferConcurrencyException`.** The repository interface remains
synchronous and unchanged, while the handler remains independent of EF Core and
maps that domain exception to `Result.Conflict`.

---

## 11. Concurrency conflict handling — handler pattern

The handler that performs an Offer update must:

1. Read the stored `Offer` from the repository.
2. Compare the incoming `Version` from the request against `offer.Version`.
3. If they differ — return `Result.Conflict` **before** calling `Update`.
4. Call `offer.Update(...)` (which increments `Version` on the Domain object).
5. Call `repository.Update(offer)`.
6. Catch `OfferConcurrencyException` from the repository — return `Result.Conflict`.

```csharp
// UpdateOfferHandler.cs (CQRS slice — generated by the cqrs skill)
// Shown here only to document the concurrency interaction.
public Result<UpdateOfferResponse> Handle(UpdateOfferCommand command)
{
    var offer = _offers.GetById(command.Id);
    if (offer is null)
        return Result<UpdateOfferResponse>.NotFound("Offer not found.");

    if (offer.Version != command.Version)
        return Result<UpdateOfferResponse>.Conflict(
            $"Offer version mismatch. Expected {offer.Version}, received {command.Version}.");

    try
    {
        offer.Update(
            command.Title,
            command.CompanyName,
            command.Requirements,
            command.Deadline,
            command.MaximumAvailableSlots);
    }
    catch (ArgumentException ex)
    {
        return Result<UpdateOfferResponse>.Validation(ex.Message);
    }

    try
    {
        _offers.Update(offer);
    }
    catch (OfferConcurrencyException)
    {
        return Result<UpdateOfferResponse>.Conflict("Offer was modified concurrently.");
    }

    return Result<UpdateOfferResponse>.Success(new UpdateOfferResponse());
}
```

**Important constraints:**
- This handler is generated by the **cqrs skill**, not this skill.
- This skill documents the pattern so the cqrs skill output is compatible.
- The handler must not reference `Microsoft.EntityFrameworkCore`, `DbContext`,
  `DbSet<T>`, or SQLite. It catches only `OfferConcurrencyException`.
- `OfferConcurrencyException` is the repository/domain boundary translation of
  `DbUpdateConcurrencyException`; the EF repository remains the only production
  code that references the EF exception.

---

## 12. DI registration — PersistenceExtensions

```csharp
using Microsoft.EntityFrameworkCore;
using InternHub.Domain.Students;
using InternHub.Api.Domain.Offers;

namespace InternHub.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InternHub")
            ?? "Data Source=internhub.db";

        services.AddDbContext<InternHubDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IStudentRepository, EfCoreStudentRepository>();
        services.AddScoped<IOfferRepository, EfCoreOfferRepository>();

        return services;
    }
}
```

**DbContext lifetime**: `AddDbContext` registers `InternHubDbContext` as **Scoped** by
default. Do not override this. Scoped DbContext means one context per HTTP request,
which is the correct EF Core lifetime for web APIs.

---

## 13. Program.cs changes

Replace the in-memory singleton registrations with the persistence extension call.

```csharp
// REMOVE:
builder.Services.AddSingleton<IOfferRepository, InMemoryOfferRepository>();
builder.Services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();

// ADD (before var app = builder.Build()):
builder.Services.AddPersistence(builder.Configuration);
```

Also add the `using` for the persistence namespace:

```csharp
using InternHub.Api.Infrastructure.Persistence;
```

And add a call to `EnsureCreated` after `builder.Build()` so the SQLite schema is
created on first run (acceptable for Sprint 2; migrations are out of scope):

```csharp
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
    db.Database.EnsureCreated();
}
```

**Do not** remove the endpoint `Map` calls or the `partial class Program` declaration.

---

## 14. appsettings.json — connection string

Add to `src/InternHub.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "InternHub": "Data Source=internhub.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

The database file `internhub.db` is written to the application working directory.
Add `internhub.db` to `.gitignore` if it is not already there.

---

## 15. Rules summary

| # | Rule |
|---|---|
| R1 | Handlers depend on `IStudentRepository` and `IOfferRepository` only. Never on `InternHubDbContext`, `DbSet<T>`, or `Microsoft.EntityFrameworkCore` (except the one permitted catch in `UpdateOfferHandler`). |
| R2 | Repository interfaces (`IStudentRepository`, `IOfferRepository`) are never changed. Their method signatures are frozen. |
| R3 | Repository implementations are synchronous (`SaveChanges`, not `SaveChangesAsync`) to match the synchronous interface contracts. |
| R4 | All `Guid` properties are stored as `string` in SQLite via `.HasConversion<string>()`. |
| R5 | `Offer.Version` is marked `.IsConcurrencyToken()` in `OfferConfiguration`. EF Core adds a `WHERE Version = @p` guard on every `UPDATE` to `Offers`. |
| R6 | `DbUpdateConcurrencyException` is caught in `EfCoreOfferRepository`, translated to `OfferConcurrencyException`, and mapped to `Result.Conflict` by `UpdateOfferHandler`. |
| R7 | Private backing fields (`_internshipCandidacies`) are mapped via `.UsePropertyAccessMode(PropertyAccessMode.Field)`. |
| R8 | `InternHubDbContext` lifetime is Scoped (the `AddDbContext` default). Never Singleton or Transient. |
| R9 | `db.Database.EnsureCreated()` is called once during startup. No EF Core migrations are used in Sprint 2. |
| R10 | In-memory repository files are kept; they are only unregistered from DI, not deleted. |
| R11 | Endpoint contracts, request/response records, handler interfaces, and route patterns do not change because persistence changes. |
| R12 | `InternshipCandidacyStatus` is stored as `string` via `.HasConversion<string>()`. |
| R13 | `EfCoreOfferRepository.Update` catches `DbUpdateConcurrencyException` and translates it to `OfferConcurrencyException`; it does not overwrite newer data. |
| R14 | `dotnet build` must succeed after all changes. |

---

## 16. What this skill does NOT do

- Does not generate CQRS slices, commands, queries, or handlers (cqrs skill).
- Does not generate unit tests (unit-testing skill).
- Does not generate integration tests (integration-testing skill).
- Does not generate EF Core migrations.
- Does not add a separate `InternHub.Infrastructure` project; everything lives in `InternHub.Api/Infrastructure/Persistence/`.
- Does not modify Domain entities.
- Does not add `Microsoft.EntityFrameworkCore.Design` to the API project.

---

## 17. Self-check checklist

Before finishing, verify every item:

- [ ] `Microsoft.EntityFrameworkCore.Sqlite` NuGet package added to `InternHub.Api.csproj`.
- [ ] `InternHubDbContext` exists in `Infrastructure/Persistence/` with three `DbSet<T>` properties.
- [ ] `StudentConfiguration` maps `Id` as `string`, `FullName` (max 200), `Email` (max 320, unique), and the `HasMany` / `UsePropertyAccessMode(Field)` navigation.
- [ ] `OfferConfiguration` maps all properties, marks `Version` with `.IsConcurrencyToken()` (NOT `.IsRowVersion()`), and maps the navigation with `UsePropertyAccessMode(Field)`.
- [ ] `InternshipCandidacyConfiguration` maps `Id`, `StudentId`, `OfferId` as `string`, and `Status` as `string`.
- [ ] `EfCoreStudentRepository` implements all five `IStudentRepository` methods with correct `bool` / `Student?` / `IReadOnlyList<Student>` return types.
- [ ] `EfCoreStudentRepository.Add` and `Update` translate only SQLite error code 19 whose message identifies `Students.Email` into `false`; unrelated `DbUpdateException`s still propagate and rejected updates leave persisted rows unchanged.
- [ ] `EfCoreOfferRepository` implements all five `IOfferRepository` methods with correct `bool` / `Offer?` / `IReadOnlyCollection<Offer>` return types.
- [ ] `EfCoreOfferRepository.Update` catches `DbUpdateConcurrencyException` and translates it to `OfferConcurrencyException`.
- [ ] `PersistenceExtensions.AddPersistence` registers `DbContext` as Scoped and both repositories as Scoped.
- [ ] `Program.cs` calls `builder.Services.AddPersistence(builder.Configuration)` and no longer registers in-memory singletons.
- [ ] `Program.cs` calls `db.Database.EnsureCreated()` during startup.
- [ ] `appsettings.json` contains the `ConnectionStrings:InternHub` key.
- [ ] `internhub.db` is in `.gitignore`.
- [ ] No handler file references EF Core, `DbContext`, `DbSet<T>`, or SQLite.
- [ ] `IStudentRepository` and `IOfferRepository` signatures are unchanged.
- [ ] `dotnet build` succeeds with no errors or warnings.

---

## 18. Example of expected output

### `Infrastructure/Persistence/InternHubDbContext.cs`

```csharp
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class InternHubDbContext(DbContextOptions<InternHubDbContext> options)
    : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<InternshipCandidacy> InternshipCandidacies => Set<InternshipCandidacy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new OfferConfiguration());
        modelBuilder.ApplyConfiguration(new InternshipCandidacyConfiguration());
    }
}
```

### `Infrastructure/Persistence/EfCoreOfferRepository.cs` (key method only)

```csharp
public bool Update(Offer offer)
{
    var exists = db.Offers.Any(o => o.Id == offer.Id);
    if (!exists) return false;

    db.Offers.Update(offer);
    try
    {
        db.SaveChanges();
    }
    catch (DbUpdateConcurrencyException exception)
    {
        throw new OfferConcurrencyException(offer.Id, exception);
    }
    return true;
}
```

### `Infrastructure/Persistence/PersistenceExtensions.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using InternHub.Domain.Students;
using InternHub.Api.Domain.Offers;

namespace InternHub.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InternHub")
            ?? "Data Source=internhub.db";

        services.AddDbContext<InternHubDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IStudentRepository, EfCoreStudentRepository>();
        services.AddScoped<IOfferRepository, EfCoreOfferRepository>();

        return services;
    }
}
```

### `Program.cs` diff (persistence swap)

```diff
-builder.Services.AddSingleton<IOfferRepository, InMemoryOfferRepository>();
-builder.Services.AddSingleton<IStudentRepository, InMemoryStudentRepository>();
+builder.Services.AddPersistence(builder.Configuration);

 var app = builder.Build();
+
+using (var scope = app.Services.CreateScope())
+{
+    var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
+    db.Database.EnsureCreated();
+}
```

---

## 19. Report after generating

End with a short report:

1. Files created, updated and (if any) deleted.
2. NuGet packages added.
3. A table: `Entity | Table name | PK type | Concurrency token`.
4. Any problem observed (missing file, unexpected signature, assumption made).
