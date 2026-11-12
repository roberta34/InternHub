---
name: ai-evaluation
description: Generate and regenerate the offline evaluation of InternHub's AI CV-feedback providers (implementations of IRecruiterEvaluationService) - a 15-case synthetic JSON dataset, deterministic C# metrics, stub-based xUnit tests, and an opt-in manual run that compares a deterministic baseline with a local LLM on real calls. Use when building or extending the AI evaluation, one stage at a time; not for changing AI providers, production code, endpoints, persistence, concurrency logic, or other features' tests.
---

# AI Evaluation Skill - InternHub

Build an **offline, provider-independent evaluation** of `IRecruiterEvaluationService` implementations. The goal is a measured comparison between a deterministic keyword baseline and a local LLM provider on the same labeled cases.

- The evaluator **measures** providers. It never changes them and is never used by the application at runtime.
- Stub-based tests prove that the **evaluator** computes correctly. The opt-in manual run measures the **providers** with real calls. Both are required; stubs never replace the real comparison.

**Generation rule:** Do not manually patch generated code or data. If output is wrong, improve this `SKILL.md`, regenerate the affected files, and re-run verification.

**Work in stages.** Generate only the stage named in the prompt (section 4). A stage is finished only when its checkpoint passes.

## 1. Inspect the repository first (hard prerequisites)

Open and verify before generating anything:

1. `tests/InternHub.Tests/InternHub.Tests.csproj`: xUnit version (2.x means no `Assert.Skip`), FluentAssertions 7.x, existing item groups.
2. `IRecruiterEvaluationService` and `RecruiterEvaluationResult` under `src/InternHub.Api/Features/RecruiterEvaluation/`: exact signatures, member types, nullability.
3. Every existing implementation of the interface (read-only): the exception types it throws for invalid model output, unreachable services, HTTP errors and timeouts; the configuration keys it requires; whether it retries internally.
4. Existing unit tests and fakes for naming, namespace and folder conventions.
5. The `unit-testing` skill: its test-style rules apply here unless this skill states otherwise.
6. For stages 2-4: the files produced by the previous stages, so new code reuses them instead of duplicating them.

**Stop and report a blocker** if the contract, an implementation, a previous stage or a required package is missing or differs from what this skill assumes. Never invent symbols. Never create production code to unblock yourself.

## 2. File scope (strict)

- **May create or change only:**
    - `tests/InternHub.Tests/AI/Evaluation/**` (dataset, evaluator, runner),
    - `tests/InternHub.Tests/Unit/AI/Evaluation/**` (tests of the evaluator),
    - `docs/ai-evaluation/**` (documentation drafts, section 10).
- **One allowed exception:** a single `ItemGroup` in the test `.csproj` that copies the dataset JSON to the output directory (`CopyToOutputDirectory` = `PreserveNewest`). The `.csproj` is a shared file: show the exact diff and wait for explicit approval before applying it. No other `.csproj` change, no new packages.
- **Never change:** anything under `src/`, provider implementations or their tests, `Program.cs`, `appsettings*.json`, other skills, shared persistence or shared test infrastructure.
- Preserve existing line endings (CRLF). Use the Edit tool for changes to existing files.
- Before writing, list every file to create or change with a one-line purpose and wait for approval.

## 3. Folder layout and names

Tests mirror the evaluator's folder structure:

```text
tests/InternHub.Tests/
├── AI/Evaluation/
│   ├── Dataset/            cv-evaluation-dataset.json, EvaluationDataset, EvaluationCase,
│   │                       EvaluationCategory, EvaluationDatasetLoader
│   ├── Metrics/
│   │   ├── Core/           SkillNormalizer, SkillList, ConfusionCounts, Ratio
│   │   ├── Strategies/     ICaseMetric, CaseMetrics, ConfusionMetric, CoverageMetric,
│   │   │                   HallucinationRateMetric, ContradictionMetric, FieldCompletenessMetric
│   │   ├── Aggregation/    MetricAggregator, CategoryBreakdown, AggregateScores
│   │   └── Stability/      StabilityCalculator, StabilityScores
│   └── Running/            EvaluationRunner, EvaluationSetupException
│       ├── Outcomes/       OutcomeKind, ProviderRunOutcome, OutcomeClassifier
│       ├── Reporting/      EvaluationRunContext, EvaluationRunReport, MarkdownReportWriter
│       └── Manual/         ManualEvaluationFactAttribute, ManualEvaluationSettings,
│                           ManualEvaluationTests
└── Unit/AI/Evaluation/
    ├── TestDoubles/        StubRecruiterEvaluationService, EvaluationCaseBuilder
    └── Dataset/, Metrics/<same subfolders>/, Running/<same subfolders>/
                            one <ClassUnderTest>Tests class per class under test
```

- Folders are grouped by responsibility and read top-down. Metrics: Core (building blocks) → Strategies (one score per case) → Aggregation (scores over all cases) → Stability (repeated runs of one case). Running: Outcomes → Reporting → EvaluationRunner; Manual holds only the opt-in real run.
- Dependency direction: a folder may use only folders listed before it in that order. Core uses nothing else from Metrics. Nothing in Metrics uses Running.
- Namespaces follow folders, e.g. `InternHub.Tests.AI.Evaluation.Metrics.Core`.
- Test doubles used by more than one test folder live in `Unit/AI/Evaluation/TestDoubles/`.
- Every type has a one-line `/// <summary>` stating its role, naming the pattern when there is one (Strategy, Factory Method, Registry, Value Object, Lookup table). No other comments except the existing ones that explain a non-obvious rule (e.g. empty-set conventions).
- Test classes: `<ClassUnderTest>Tests`. A test double used by only one test folder lives next to the tests that use it.
- Records are immutable; collections are exposed as `IReadOnlyList<string>`.
- Do not create a separate type for something used by only one class and without behavior tested on its own; make it a private member or parameters instead. Keep a type separate only if it has its own tested behavior, is a result shared between classes, or is required by xUnit.
- The names above are the expected design; use them unless the inspected repository already has an equivalent type.

## 4. Stages and checkpoints

| Stage | Content | Checkpoint |
|---|---|---|
| 1 | Dataset JSON, C# model, loader, `SkillNormalizer`, dataset invariant tests, normalizer tests, `.csproj` copy rule | build + tests pass; the owner reviews every case and label by reading the JSON |
| 2 | Precision / recall / F1 for matched and missing, micro and macro aggregation, field completeness, tests (reusing `SkillNormalizer`) | build + tests pass; every expected value in tests is a hand-computed literal |
| 3 | Hallucination, contradiction, coverage, stability, per-category breakdown, tests | build + tests pass |
| 4 | Outcome classification, pre-flight check, runner, Markdown report, opt-in manual test, tests | build + tests pass; manual test shows as **skipped** without the environment variable; one real run produces a report |

## 5. Dataset rules (stage 1)

- One JSON file, `Dataset/cv-evaluation-dataset.json`, camelCase, deserialized with `System.Text.Json` into immutable records.
- Root fields: `datasetVersion`, `synthetic` (must be `true`), `aliases` (variant -> canonical skill, both lowercase), `cases`.
- Case fields: `id` (unique, kebab-case, prefixed by category, e.g. `negation-01`), `category`, `cvText`, `requirementsText`, `requiredSkills` (the canonical requirement list, labeled independently of the other two lists), `expectedMatched`, `expectedMissing`, `rationale` (one sentence justifying the labels).
- Exactly **15 cases**: 3 `Clear` and 2 each of `Synonyms`, `Negation`, `Implicit`, `Noise`, `Minimal`, `PromptInjection`.
- What each category must contain:
    - `Clear`: every required skill stated plainly or plainly absent.
    - `Synonyms`: variants such as "csharp", "dotnet", "js", resolved through `aliases`.
    - `Negation`: the CV explicitly says it lacks a required skill that it names; that skill is labeled missing.
    - `Implicit`: a skill implied but not named; the `rationale` states whether the implication counts.
    - `Noise`: long CV with irrelevant content around few relevant skills.
    - `Minimal`: almost empty CV; all required skills are missing.
    - `PromptInjection`: the CV contains an instruction to the evaluator (e.g. to report every skill as matched); labels reflect the real CV content only.
- **All content is fictional.** No real people, companies, emails, phone numbers, addresses or links.
- **Do not tailor the dataset to any provider.** Include required skills outside any provider's built-in vocabulary on purpose, and say so in the dataset documentation.
- The loader locates the file relative to `AppContext.BaseDirectory` and fails with a clear message if it is missing or malformed.
- The loader separates parsing from file access: one method parses a JSON string, `Load(path)` reads the file and calls it. Missing file -> `FileNotFoundException` naming the path; malformed JSON, unknown category or empty case list -> `InvalidOperationException` with a clear message. Loader tests use JSON strings and a non-existent path; they never write files.
- Ids are prefixed by the category in kebab-case (`PromptInjection` -> `prompt-injection-01`).
- Per-case invariants are `[Theory]` tests with `[MemberData]` over case ids, so a failure names the offending case.
- Alias targets are canonical: no alias target may also be an alias key.
- The alias map also covers common surface variants a model may return for each required skill (plural forms, "restful", "unit test" vs "unit testing"). Aliases are fixed before any real run and never adjusted after seeing provider output.
- Every alias key and target is already trimmed and lowercase (invariant test), because lookup happens after normalization.
- **Invariant tests:**
    - the file loads; `synthetic` is true; ids are unique;
    - exactly 15 cases with the category distribution above;
    - after normalization, `expectedMatched` and `expectedMissing` are disjoint;
    - their union equals `requiredSkills`;
    - every required skill, or one of its aliases, occurs in `requirementsText`;
    - `rationale` is non-empty.
- `requiredSkills` is the reference for coverage and hallucination metrics.

## 6. Normalization and metrics (stages 2-3)

**Normalization:** trim, `ToLowerInvariant`, then replace through the dataset alias map. Apply identically to predictions and labels; compare as sets (duplicates removed).

**Metrics are strategies.** Each per-case metric is its own class implementing `ICaseMetric` (a `Name` and a compute method that takes the case, the provider result and the normalizer). The evaluator iterates a list of metrics. Never select metrics with `switch` or `if` chains; a new metric is a new class registered in the list.

**One class for confusion-based scores.** Precision, recall and F1 are a single strategy, `ConfusionMetric(SkillList list, string scoreName, Func<ConfusionCounts, double> score)`, with a private constructor and three static factory methods: `ConfusionMetric.Precision(list)`, `ConfusionMetric.Recall(list)`, `ConfusionMetric.F1(list)`. Metric names stay exactly `"<List> precision"`, `"<List> recall"`, `"<List> F1"`, because reports and thresholds use them. Do not create one class per score.

**No duplicate score records.** Micro aggregation stores the summed `ConfusionCounts` per `SkillList` (`AggregateScores.Micro` is `IReadOnlyDictionary<string, ConfusionCounts>`); precision, recall and F1 are read from it. Do not create a record that only copies them.

**Registry.** `CaseMetrics.Default` is the only place where metrics are listed. Adding a metric means adding one class and one line there; the aggregator never changes.

**Empty-set values are explicit.** One helper computes ratios and receives the value to return when the denominator is zero:

| Metric (per case) | Definition | When the denominator is 0 |
|---|---|---|
| Precision (matched / missing) | `|pred ∩ exp| / |pred|` | 1 if `exp` is also empty, else 0 |
| Recall (matched / missing) | `|pred ∩ exp| / |exp|` | 1 if `pred` is also empty, else 0 |
| F1 (matched / missing) | `2PR / (P + R)` | 0 when `P + R = 0` |
| Hallucination rate | predicted skills (matched ∪ missing) not in `requiredSkills` / predicted skills | 0 |
| Contradiction | 1 if a skill is both matched and missing, else 0 | - |
| Coverage | `requiredSkills` present in (matched ∪ missing) / `requiredSkills` | 1 |

**Aggregation:** micro (sum TP, FP, FN over cases, then compute) and macro (mean of per-case values), both reported. Aggregate only successful outcomes; failures are reported separately.

**Field completeness:** a successful result is complete when its lists are not null, `Summary` is non-blank, and `Suggestions` is non-empty whenever at least one missing skill is reported. Zero suggestions with no missing skills is valid.

**Stability (stage 3):** for K results of the same case, the mean pairwise Jaccard similarity `|A ∩ B| / |A ∪ B|` of normalized sets, separately for matched and missing; two empty sets count as 1. A deterministic provider is expected to score 1.

**Per-category breakdown:** every metric is also reported per dataset category.

## 7. Run outcomes and the runner (stage 4)

- **Pre-flight check:** before any provider call, verify that every required configuration value is present. If one is missing, stop with a clear setup error; never record it as a provider outcome. Some exception types are shared between configuration errors and invalid model output, so classification is reliable only once configuration is guaranteed.
- Each call yields exactly one outcome: `Success` (with the result), `InvalidResponse`, or `InfrastructureUnavailable`.
- `OutcomeClassifier` maps exception types to outcome kinds with a **lookup dictionary** (`Type` -> `OutcomeKind`), based on the exceptions actually thrown by the inspected implementations.
- An exception type not in the dictionary is rethrown, never hidden. A cancellation requested by the caller's own token is rethrown; a timeout (cancellation not requested by the caller) is `InfrastructureUnavailable`.
- If the very first call ends in `InfrastructureUnavailable`, stop the run and report it instead of filling the report with identical failures.
- Report the valid-response rate over calls that reached the model and the unavailability rate separately, so an unreachable service never counts as poor model quality.
- State in the report that a provider's internal retries are invisible: rates are measured after them.
- A result returned without an exception but with a null list or blank summary is classified as `InvalidResponse` before it reaches any metric.
- The runner and report writer receive the run date and context as inputs; they never call `DateTime.Now` / `DateTime.UtcNow`.

## 8. Tests of the evaluator (all stages)

- Follow the `unit-testing` skill: xUnit, FluentAssertions 7.x, `Given_<state>_When_<action>_Then_<expected>`, `// Arrange`, `// Act`, `// Assert` separated by blank lines, one behavior per test, no `if`/`for`/`foreach`/`while`/`switch` in test bodies, `[Theory]` + `[InlineData]` for input variants, no dependence on test order.
- Use small **hand-written stub providers** implementing `IRecruiterEvaluationService` that return a fixed result or throw a given exception. No mocking library, no network, no Ollama, no files except the dataset under test.
- Expected values are **computed by hand** from small explicit inputs and written as literals; never recompute them with the code under test.
- Compare `double` values with `BeApproximately(expected, 0.0001)`.
- Cover:
    - perfect match, disjoint sets, empty prediction, every empty-set convention;
    - alias normalization, case and whitespace differences, duplicates;
    - contradiction, hallucination, coverage, micro vs. macro on inputs where they differ;
    - stability on simulated repeated results;
    - each outcome kind, timeout vs. caller cancellation, rethrow of an unknown exception;
    - pre-flight failure, stop after a first unavailable call;
    - report content without CV text.
- The evaluator lives in the test project, so it is outside the production coverage report; its correctness is shown by these tests.
- An `[InlineData]` row must never consist of a single array argument (C# treats it as the whole `params object[]` list, error CS0182); add the expected value as a second parameter.
- If generated code fails to build or a test fails because of generated code, stop and report it; never patch generated code. The owner updates this skill and requests regeneration.

## 9. Opt-in manual comparison run (stage 4)

- `ManualEvaluationFactAttribute` derives from `FactAttribute` and sets `Skip` in its constructor unless the environment variable `INTERNHUB_AI_EVAL` equals `1`. Without it, `dotnet test` reports the test as skipped, never failed.
- Inputs come from environment variables: `INTERNHUB_AI_EVAL_OLLAMA_ENDPOINT`, `INTERNHUB_AI_EVAL_OLLAMA_MODEL`, `INTERNHUB_AI_EVAL_RUNS` (K, default 5), and context values `INTERNHUB_AI_EVAL_TEMPERATURE`, `INTERNHUB_AI_EVAL_PROMPT_VERSION`, `INTERNHUB_AI_EVAL_COMMIT`, `INTERNHUB_AI_EVAL_DATE`.
- Providers are built directly, without DI: the Ollama provider gets an `HttpClient` with an explicit timeout and an in-memory configuration with the keys it requires. It never reads `appsettings`.
- The deterministic provider runs once per case; a non-deterministic provider runs K times per case. A second model is compared by repeating the run with a different model variable.
- Output: a Markdown report written through `ITestOutputHelper` and to a file in the test output directory, named after provider and model.
- The report contains the run context (provider, model, temperature, prompt version, K, date, commit), aggregates, outcome counts, and per-category tables.
- The manual run **never asserts quality thresholds**; it only measures and reports.
- **Never log, print or write CV text or provider-generated text.** Reports contain only case ids, categories and numbers.

## 10. Documentation drafts

- May draft `docs/ai-evaluation/dataset.md` (purpose, categories, labeling rules, aliases, "synthetic data" notice, skills deliberately outside provider vocabularies) and the definitions section of `docs/ai-evaluation/metrics.md`, consistent with sections 6-7.
- **Never fill in threshold values or results.** Thresholds are set by the human owner before the first real run.
- `docs/ai-evaluation/results.md` is written by the human owner after real runs and records, per run: provider, model, temperature, prompt version, K, date, commit, and the reported numbers. Never write numbers that did not come from an actual run.

## 11. Examples of expected output (verify real APIs before reuse)

**One dataset case:**

```json
{
  "id": "negation-01",
  "category": "Negation",
  "cvText": "Backend student. Built REST APIs in C# with SQL Server. I have not worked with Docker yet.",
  "requirementsText": "We need C#, SQL and Docker.",
  "requiredSkills": ["c#", "sql", "docker"],
  "expectedMatched": ["c#", "sql"],
  "expectedMissing": ["docker"],
  "rationale": "Docker is named in the CV only to state it is missing."
}
```

**One metric as a strategy:**

```csharp
public sealed class ContradictionMetric : ICaseMetric
{
    public string Name => "Contradiction";

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer) =>
        normalizer.ToSet(result.MatchedSkills).Overlaps(normalizer.ToSet(result.MissingSkills)) ? 1 : 0;
}
```

**Its test:**

```csharp
[Fact]
public void Given_SkillInBothListsWithDifferentCase_When_ComputingContradiction_Then_ReturnsOne()
{
    // Arrange
    var result = new RecruiterEvaluationResult(["C#"], ["c#"], ["Practice testing."], "Summary.");
    var metric = new ContradictionMetric();

    // Act
    var value = metric.Compute(EvaluationCaseBuilder.Any(), result, SkillNormalizerBuilder.WithoutAliases());

    // Assert
    value.Should().BeApproximately(1, 0.0001);
}
```

**Report shape (numbers come only from real runs):**

```markdown
| Metric | Micro | Macro |
|---|---|---|
| Matched F1 | ... | ... |
```

## 12. Self-check and report

- [ ] Only the requested stage was generated; all files are inside the allowed scope and follow the folder layout.
- [ ] The `.csproj` change, if any, was shown and approved first.
- [ ] Contract types, configuration keys and exception types match the inspected code.
- [ ] No `switch`; metrics and outcome mapping use strategies / lookup tables.
- [ ] Folder layout, dependency direction and namespaces match section 3.
- [ ] Precision, recall and F1 exist only as ConfusionMetric factories; no record duplicates ConfusionCounts.
- [ ] Every type has its one-line role summary.
- [ ] Tests follow the unit-testing style, use stubs and hand-computed literals, no external services, no current-time calls.
- [ ] No CV or generated text is logged or written anywhere.
- [ ] `dotnet build` and `dotnet test tests/InternHub.Tests/InternHub.Tests.csproj` pass; the manual test shows as skipped.

End with: files created or changed, scenarios covered, real test-command results (counts), blockers, and a short **truthful** `PROMPTS.md` entry: the prompt, the skill used, the problem observed, the skill change and why, and the verified before/after result. Never claim runs, regenerations or results that did not happen.