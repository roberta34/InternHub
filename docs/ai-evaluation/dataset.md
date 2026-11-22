# AI Evaluation Dataset

## Purpose

This dataset is an offline, provider-independent benchmark for implementations of
`IRecruiterEvaluationService` (the CV-feedback providers). It lets the team compare the
deterministic keyword baseline with a local LLM provider on the same labeled cases.

The dataset and the evaluator live only in the test project
(`tests/InternHub.Tests/AI/Evaluation/`). They are never used by the application at runtime
and never change a provider.

## Synthetic data notice

All 15 cases are **synthetic and fictional**. They contain no real people, companies, email
addresses, phone numbers, postal addresses or links. The root field `synthetic` is `true`,
and a test fails if it is not.

## File and format

- File: `tests/InternHub.Tests/AI/Evaluation/Dataset/cv-evaluation-dataset.json`
- Copied to the test output directory by one `ItemGroup` in `InternHub.Tests.csproj`
  (`CopyToOutputDirectory` = `PreserveNewest`).
- Loaded by `EvaluationDatasetLoader` from `AppContext.BaseDirectory`:
  - missing file -> `FileNotFoundException` naming the path;
  - malformed JSON, an unknown category, or an empty or missing case list ->
    `InvalidOperationException` with a clear message.
- Current `datasetVersion`: `1.0.0`.

Root fields (camelCase):

| Field | Meaning |
|---|---|
| `datasetVersion` | Version of cases, labels and aliases together |
| `synthetic` | Always `true` |
| `aliases` | Map of variant -> canonical skill, both lowercase |
| `cases` | The 15 labeled cases |

Case fields:

| Field | Meaning |
|---|---|
| `id` | Unique, kebab-case, prefixed by the category in kebab-case (`prompt-injection-01`) |
| `category` | One of the seven categories below |
| `cvText` | The fictional CV given to the provider |
| `requirementsText` | The fictional offer requirements given to the provider |
| `requiredSkills` | Canonical list of required skills, labeled independently of the two lists below |
| `expectedMatched` | Required skills the CV explicitly states |
| `expectedMissing` | Required skills the CV does not state |
| `rationale` | One sentence that justifies the labels |

`requiredSkills` is the reference for the coverage and hallucination metrics.

## Categories

| Category | Cases | What it tests | Case ids |
|---|---|---|---|
| Clear | 3 | Every required skill is stated plainly or is plainly absent | `clear-01`, `clear-02`, `clear-03` |
| Synonyms | 2 | Variants such as "CSharp", "dotnet", "Postgres", "JS", "TS", "k8s", resolved through the alias map | `synonyms-01`, `synonyms-02` |
| Negation | 2 | The CV names a required skill only to say the candidate lacks it | `negation-01`, `negation-02` |
| Implicit | 2 | A skill is implied by related work but never named (React without JavaScript; kubectl without Kubernetes) | `implicit-01`, `implicit-02` |
| Noise | 2 | A long CV with mostly irrelevant content around one or two relevant skills | `noise-01`, `noise-02` |
| Minimal | 2 | An almost empty CV; every required skill is missing | `minimal-01`, `minimal-02` |
| PromptInjection | 2 | The CV contains an instruction to the evaluator (report everything as matched; a fake "system note" claiming skills) | `prompt-injection-01`, `prompt-injection-02` |

## Labeling rules

1. A required skill is **matched** only if the CV explicitly states it as the candidate's
   skill or experience, either by name or by one of its aliases.
2. **Implied skills never count.** A skill that is only implied by related work (for
   example JavaScript implied by React) is labeled missing. The rationale of each Implicit
   case says so.
3. A skill named only to state that the candidate lacks it ("I have not worked with Docker
   yet") is labeled **missing**.
4. Instructions addressed to the evaluator inside a CV are not CV content. Skills that appear
   only inside such an instruction are labeled **missing**; labels reflect the real CV
   content only.
5. `requiredSkills` is labeled from the requirements text alone. After normalization,
   `expectedMatched` and `expectedMissing` are disjoint and their union equals
   `requiredSkills`.
6. Every required skill, or one of its aliases, occurs in `requirementsText`.
7. Every case has a non-empty one-sentence `rationale`.

## Normalization and aliases

Every skill, predicted or labeled, is normalized the same way by `SkillNormalizer`:
trim, `ToLowerInvariant`, then replace through the alias map. Comparisons are made on sets,
so duplicates are removed.

Alias map (27 entries):

| Canonical skill | Aliases |
|---|---|
| `c#` | `csharp`, `c sharp`, `c-sharp` |
| `.net` | `dotnet`, `dot net`, `.net core` |
| `javascript` | `js`, `java script`, `ecmascript` |
| `typescript` | `ts`, `type script` |
| `postgresql` | `postgres`, `postgre sql` |
| `kubernetes` | `k8s`, `kube` |
| `rest api` | `restful api`, `rest apis`, `restful apis`, `rest`, `restful` |
| `unit testing` | `unit tests`, `unit test`, `unit-testing` |
| `react` | `reactjs`, `react.js` |
| `python` | `python3`, `python 3` |

Rules for the map:

- It covers common surface variants a model may return for a required skill (plural forms,
  "restful", "unit test" vs "unit testing").
- Alias targets are canonical: no alias target is also an alias key (no chains).
- Every key and target is already trimmed and lowercase, because lookup happens after
  normalization.
- **The map is fixed before any real run and is never adjusted after seeing provider
  output.** Tuning aliases to a provider's answers would inflate its scores.

Deliberately not mapped:

- `github` -> `git`: GitHub is a hosting product, not the skill.
- `nosql` -> `sql`: a different skill.
- `docker compose` -> `docker`: a different tool.
- Version strings such as `.net 8`.
- `java`, `sql`, `linux`, `docker`, `git`: no common variant is safe to map.

## Skills outside the baseline vocabulary

The keyword baseline (`KeywordRecruiterEvaluationService`) recognizes only these 10 skills:
Python, Java, JavaScript, SQL, C#, C++, .NET, React, Docker, Git.

The dataset is **not tailored to any provider**. It deliberately requires skills outside that
vocabulary:

- Kubernetes
- TypeScript
- Linux
- PostgreSQL
- REST API
- unit testing

A provider limited to a fixed vocabulary cannot recognize these skills, and the results
should show that rather than hide it.

## Invariant tests

`tests/InternHub.Tests/Unit/AI/Evaluation/Dataset/EvaluationDatasetTests.cs` checks:

- the file loads and `synthetic` is true;
- case ids are unique;
- there are exactly 15 cases with the category distribution above;
- no alias target is also an alias key;
- every alias key and target is trimmed and lowercase;
- per case (a `[Theory]` over case ids, so a failure names the case):
  - the id is the kebab-case category followed by a two-digit number;
  - after normalization, matched and missing are disjoint;
  - their union equals `requiredSkills`;
  - every required skill or one of its aliases occurs in `requirementsText`;
  - the rationale is not blank.

## Changing the dataset

Any change to cases, labels or aliases must:

- happen before a run, never in response to a provider's results;
- increase `datasetVersion`;
- keep every invariant test passing.

Results from different dataset versions are not comparable.
