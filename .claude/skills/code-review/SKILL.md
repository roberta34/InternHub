---
name: code-review
description: Performs a strictly read-only review of InternHub or a classmate's InternHub repository for CQRS architecture, vertical-slice boundaries, domain invariants, persistence usage, DI, error handling, naming, and test quality. Reports only evidence-based findings with exact file lines and actionable fixes; never edits reviewed files.
---

# Code Review Skill — InternHub

This skill performs review only. It must never edit, generate fixes in, delete, rename, format, or otherwise mutate reviewed source files, tests, configuration, documentation, or skills. It may read repository files, inspect Git state and diffs, and run non-mutating inspection commands. Do not create `REVIEW.md` unless the user explicitly requests it.

The review supports both this InternHub repository and a classmate's InternHub repository. Always review the repository supplied by the user or the current repository when no other repository is specified. Do not assume that a classmate's branch, folder, or implementation matches this repository; inspect its actual contracts first.

## 1. Review prerequisites

Before reporting findings, inspect the real implementation and its surrounding contracts:

1. Read the relevant Git diff or requested files. Do not review only a summary or file name.
2. Read `src/InternHub.Api/Program.cs`, shared `Result` and HTTP mapping, and the applicable feature module.
3. For each requested slice, inspect its endpoint, request/response records, handler interface, handler, repository interface, and concrete persistence implementation.
4. Inspect the relevant Domain entities, constructors, update/state-transition methods, validation, and invariants.
5. Inspect EF Core `DbContext`, entity configurations, DI registration, and test fixtures when persistence or integration behavior is in scope.
6. Inspect relevant unit, integration, and Postman tests without changing them.
7. Check existing conventions in `.claude/skills/`, including CQRS, EF Core + SQLite, unit-testing, and integration-testing rules.
8. If a required file or contract is unavailable, state that limitation and do not invent a finding.

The review must distinguish requested scope from unrelated pre-existing code. Do not report a problem solely because code differs from a preferred style unless it violates an established project convention or creates a concrete defect.

## 2. Sprint 3 architecture rules

Review against the actual code and these project conventions:

- One vertical slice owns one use case under `src/InternHub.Api/Features/<Aggregate>/<UseCase>/`.
- Commands and queries are immutable records; handlers have small interfaces and one public `Handle` method.
- Handlers depend on repository interfaces, not `DbContext`, EF Core, SQLite, HTTP, or endpoint types.
- Endpoints bind input, create a command/query, invoke the handler, and map `Result<T>` to HTTP. Business rules do not belong in endpoints.
- New Sprint 3 modules implement a public parameterless `IFeatureModule`, register handlers in `RegisterServices`, and map endpoints in `MapEndpoints` through the shared `/api/v1` group. They must not duplicate route mappings or require unrelated `Program.cs` edits.
- Domain constructors and state-transition methods own domain invariants. Handlers translate expected domain failures into `Result.Validation`, `Result.NotFound`, or `Result.Conflict` as appropriate.
- Queries do not mutate entities or call repository write operations.
- Persistence uses the real repository contracts and EF Core + SQLite configuration; it must preserve relational constraints and concurrency behavior.
- Tests use the real public behavior, xUnit/FluentAssertions conventions, isolated SQLite for HTTP integration tests, and hand-written fakes for handler unit tests.

These rules are review criteria, not permission to change implementation. Report the exact violated contract and the smallest concrete fix.

## 3. YAML-based review rules

The review rule set is expressed in YAML so it can be checked consistently across InternHub repositories:

```yaml
review_rules:
  read_only: true
  severities:
    - Critical
    - Major
    - Minor
  categories:
    - CQRS architecture
    - Vertical-slice boundary
    - Domain invariant
    - Repository and persistence
    - Dependency injection
    - Error handling
    - Naming and conventions
    - Test quality
  required_finding_fields:
    - file
    - line
    - severity
    - category
    - problem
    - proposed_fix
  ordering:
    - severity_descending
    - file_ascending
    - line_ascending
  output:
    summary_first: true
    single_markdown_table: true
    columns:
      - File:Line
      - Severity
      - Category
      - Problem
      - Proposed fix
```

Only the listed categories may be used. Do not create additional severity levels or categories in the review output.

## 4. Severity definitions

- **Critical** — A release-blocking defect with a high-impact consequence, such as a security exposure, data loss/corruption, broken persistence invariant, or a core endpoint that cannot operate as specified.
- **Major** — A substantial functional, architectural, or test-isolation defect that breaks a required use case, violates a Sprint 3 boundary, loses expected error/concurrency behavior, or makes important behavior unverified.
- **Minor** — A limited correctness, maintainability, naming, consistency, or test-quality issue that does not by itself block the use case.

Severity must reflect impact and evidence, not the size of the diff. If evidence is insufficient, do not report the suspected issue.

## 5. Review procedure

1. Establish scope from the user's request, repository state, and diff.
2. Trace each changed endpoint through its handler interface, handler, repository interface, persistence implementation, and Domain entity.
3. Check success and reachable failure paths, including `ResultStatus` mapping and error response shape.
4. Check DI lifetime and module discovery/mapping for new Sprint 3 features.
5. Check that domain invariants are enforced once in the Domain and are not bypassed by persistence or endpoints.
6. Check repository calls for unintended writes in queries, missing concurrency handling, and accidental direct EF access from handlers.
7. Check tests for independent state, real public behavior, exact status/response assertions, fresh database verification, and meaningful branch coverage.
8. Verify every finding against the current file content and record an exact 1-based line number. A finding without an existing file and exact line is invalid.
9. Sort findings by severity in this order: Critical, Major, Minor; then by file and line.
10. Do not propose speculative fixes, unrelated refactors, or changes outside the reviewed scope.

## 6. Required output

The first line must be a one-line count summary in this form:

`Summary: X Critical, Y Major, Z Minor findings.`

Then produce exactly one Markdown table with these columns:

| File:Line | Severity | Category | Problem | Proposed fix |
|---|---|---|---|---|

Every row must:

- reference an existing repository-relative file and exact 1-based line, such as `src/InternHub.Api/Features/Offers/GetOfferById/Endpoint.cs:12`;
- use one defined severity and one defined category;
- describe the observed, concrete problem and its impact;
- provide a specific, actionable proposed fix;
- be ordered by severity, then file, then line.

When no evidence-based issues are found, report:

`Summary: 0 Critical, 0 Major, 0 Minor findings.`

and still produce the required table with a single row:

| File:Line | Severity | Category | Problem | Proposed fix |
|---|---|---|---|---|
| — | — | — | No issues found in the reviewed scope. | No action required. |

Do not include invented findings, vague warnings, a second findings table, or a fix patch. Mention review limitations briefly after the table only when they materially affect confidence.

## 7. Expected output example

```markdown
Summary: 0 Critical, 0 Major, 0 Minor findings.

| File:Line | Severity | Category | Problem | Proposed fix |
|---|---|---|---|---|
| — | — | — | No issues found in the reviewed scope. | No action required. |
```

Example with an evidence-based finding:

```markdown
Summary: 0 Critical, 1 Major, 1 Minor findings.

| File:Line | Severity | Category | Problem | Proposed fix |
|---|---|---|---|---|
| src/InternHub.Api/Features/Offers/GetOfferById/Handler.cs:14 | Major | Repository and persistence | The query handler resolves `InternHubDbContext` directly, bypassing the `IOfferRepository` boundary required by the slice. | Inject `IOfferRepository` and move the EF query into the concrete persistence repository; keep the handler dependent only on the interface. |
| tests/InternHub.Tests/Integration/Offers/GetOfferById/Tests.cs:31 | Minor | Test quality | The test asserts only `IsSuccessStatusCode` and does not verify the response contract or persisted state. | Assert the exact `200` status, response fields, and the unchanged row through a fresh `AsNoTracking()` context. |
```

The examples illustrate formatting only. Findings must be based on the reviewed repository's actual files and lines.

## 8. Read-only self-check

- [ ] No reviewed file was edited, generated, deleted, renamed, formatted, or fixed.
- [ ] The actual repository and requested scope were inspected.
- [ ] Existing contracts and Sprint 3 module conventions were read before judging violations.
- [ ] Every finding uses an existing file and exact 1-based line.
- [ ] Only Critical, Major, or Minor severities are used.
- [ ] Only the fixed YAML categories are used.
- [ ] Findings are sorted by severity, file, and line.
- [ ] The first line is the finding-count summary.
- [ ] Exactly one Markdown findings table is produced.
- [ ] A no-issues result is explicit and uses the required empty row.
- [ ] Every finding has a concrete proposed fix.
- [ ] No review was run merely by creating this skill.
