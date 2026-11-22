# AI Evaluation Metrics

This document defines how the evaluator measures a provider. It contains no results. The code
is in `tests/InternHub.Tests/AI/Evaluation/Metrics/` (`Core/`, `Strategies/`, `Aggregation/`,
`Stability/`) and `tests/InternHub.Tests/AI/Evaluation/Running/` (`EvaluationRunner` at the root,
with `Outcomes/`, `Reporting/` and `Manual/`); the dataset is described in `dataset.md`.

## Scope

For each dataset case the provider returns a `RecruiterEvaluationResult` with
`MatchedSkills`, `MissingSkills`, `Suggestions` and `Summary`. Every metric below is computed
**only from successful outcomes**. Invalid responses and unavailable calls are counted and
reported separately (see "Outcome classification").

## Normalization

Predicted and labeled skills are normalized identically (trim, lowercase, alias lookup; see
`dataset.md`) and compared as sets, so case, whitespace, aliases and duplicates do not affect
any score.

## Per-case metrics

Notation for one case and one list (matched or missing):

- `pred` = the provider's normalized list, `exp` = the labeled normalized list;
- TP = |pred intersect exp|, FP = |pred minus exp|, FN = |exp minus pred|;
- `required` = the case's normalized `requiredSkills`;
- `predicted` = normalized `MatchedSkills` union `MissingSkills`.

Every ratio goes through one helper, `Ratio.Divide(numerator, denominator, whenDenominatorIsZero)`,
so each empty-set value is explicit.

| Metric | Formula | When the denominator is 0 | Class |
|---|---|---|---|
| Matched / Missing precision | TP / (TP + FP) = TP / \|pred\| | 1 if `exp` is also empty, else 0 | `ConfusionMetric.Precision` |
| Matched / Missing recall | TP / (TP + FN) = TP / \|exp\| | 1 if `pred` is also empty, else 0 | `ConfusionMetric.Recall` |
| Matched / Missing F1 | 2PR / (P + R) | 0 when P + R = 0 | `ConfusionMetric.F1` |
| Hallucination rate | predicted skills not in `required` / \|predicted\| | 0 (nothing predicted) | `HallucinationRateMetric` |
| Contradiction | 1 if a skill is in both matched and missing, else 0 | - | `ContradictionMetric` |
| Coverage | skills of `required` present in `predicted` / \|required\| | 1 (nothing required) | `CoverageMetric` |
| Field completeness | 1 if complete (see below), else 0 | - | `FieldCompletenessMetric` |
| Suggestion coverage | reported missing skills named in at least one suggestion / reported missing skills | 1 (nothing missing, nothing to cover) | `SuggestionAssociationMetric.Coverage` |
| Suggestion mismatch | suggestions that name no reported missing skill / suggestions | 0 (no suggestions, nothing mismatched) | `SuggestionAssociationMetric.Mismatch` |

Consequences of the empty-set values:

- Both sets empty: precision 1, recall 1, F1 1 (the provider correctly reported nothing).
- Empty prediction with a non-empty label set: precision 0, recall 0, F1 0.
- Non-empty prediction with an empty label set: precision 0, recall 0, F1 0.

Each metric is a strategy implementing `ICaseMetric` (in `Metrics/Strategies/`). Precision,
recall and F1 are one class, `ConfusionMetric`, created through its factory methods
`ConfusionMetric.Precision(list)`, `ConfusionMetric.Recall(list)` and `ConfusionMetric.F1(list)`,
each registered twice (for `SkillList.Matched` and `SkillList.Missing`); the metric names stay
`"<List> precision"`, `"<List> recall"` and `"<List> F1"`. `CaseMetrics.Default` is the only
place where metrics are listed; adding a metric means adding one class and one line there.

## Metric kinds: structural and semantic

Every metric declares a `MetricKind`, and the report has one section per kind:

| Kind | Meaning | Metrics |
|---|---|---|
| Structural | Checks the shape of the output; needs no dataset labels | valid-response rate (run level), field completeness, suggestion coverage, suggestion mismatch |
| Semantic | Compares the output with the dataset labels | matched / missing precision, recall, F1; hallucination rate; contradiction; coverage |

The report writer groups by the kind each metric declares, never by a list of names.

## Suggestion association

`Suggestions` is a plain list of strings with no link to a skill, so the association is found
in the text. It is **structural**: it does not judge whether a suggestion is good, only whether
it names a skill the provider itself reported as missing.

- A suggestion names a skill when a whole term in it normalizes (trim, lowercase, dataset
  aliases) to that skill. A term is not preceded or followed by a letter, digit, `+` or `#`;
  a `.` counts as a boundary only when it is not followed by a letter or digit.
- So "Deploy to a k8s cluster" names `kubernetes`, but "Build a page in JavaScript" does not
  name `java`, and "Write a tool in C#" does not name `c`.
- Both scores use the provider's own reported missing skills; the expected labels are not used.
- If the contract ever gains an explicit skill association, it replaces the text search.

## Unsupported evidence and hallucination

A model may report a skill the CV does not support. Two different metrics catch it,
depending on whether the offer requires that skill:

- **Not required** (UnsupportedEvidence cases: the CV only wishes to learn Kubernetes, or says
  the team used AWS): reporting that technology in either list is counted by
  **`HallucinationRateMetric`**, because it is not in `requiredSkills`.
- **Required**: reporting a required skill as matched without support in the CV is a **false
  positive in matched precision** (and a false negative in missing recall), not a
  hallucination.

## Micro vs macro aggregation

`MetricAggregator` reports both:

- **Micro** (precision, recall, F1 per list): sum TP, FP and FN over all successful results,
  then apply the formulas above once.
- **Macro** (every metric): the mean of the per-case values.

Micro weights every skill equally; macro weights every case equally. They differ when cases
have different numbers of skills. Example from the unit tests (matched list):

| | Expected | Predicted | P | R | F1 |
|---|---|---|---|---|---|
| Case A | {a} | {a} | 1 | 1 | 1 |
| Case B | {b, c, d} | {b, x, y, z} | 0.25 | 0.3333 | 0.2857 |
| Macro | | | 0.625 | 0.6667 | 0.6429 |
| Micro (TP 2, FP 3, FN 2) | | | 0.4 | 0.5 | 0.4444 |

When a provider runs K times per case, every successful run is one result in the aggregate.
Aggregating zero results is an error; the report then states that no metrics were computed.

## Field completeness

A successful result is complete when:

- `MatchedSkills`, `MissingSkills` and `Suggestions` are not null;
- `Summary` is not blank;
- `Suggestions` is non-empty whenever at least one missing skill is reported.

Zero suggestions with no missing skills is valid.

## Stability

`StabilityCalculator` measures how consistent a non-deterministic provider is across K runs of
the same case:

- For each case, the mean pairwise Jaccard similarity |A intersect B| / |A union B| over all
  pairs of runs, computed separately for the matched and the missing list.
- Two empty sets count as 1 (identical).
- The reported value is the mean over cases.
- Computed only when K >= 2, and only over cases with at least two successful runs; the report
  states how many cases were used. Fewer than two results for a case is an error.
- A deterministic provider is expected to score 1.

Example: runs {a, b}, {a, b}, {a} give pairs 1, 0.5 and 0.5, so stability = 0.6667.

## Per-category breakdown

`CategoryBreakdown` groups successful (case, result) pairs by dataset category and aggregates
each group with the same `MetricAggregator`, so every metric (micro and macro) is also
reported per category. A category with no successful result is omitted.

## Outcome classification

Every provider call yields exactly one outcome:

| Outcome | When |
|---|---|
| `Success` | A result was returned with non-null lists and a non-blank summary |
| `InvalidResponse` | A result was returned with a null list or blank summary, or the call threw an exception mapped to `InvalidResponse` |
| `InfrastructureUnavailable` | The call threw an exception mapped to `InfrastructureUnavailable` |

`OutcomeClassifier` maps the exact exception type through a lookup table, based on the
exceptions the inspected providers actually throw:

| Exception type | Outcome | Source |
|---|---|---|
| `InvalidOperationException` | `InvalidResponse` | Ollama provider: invalid model output after its retry |
| `HttpRequestException` | `InfrastructureUnavailable` | Ollama provider: HTTP error status or connection failure |
| `TaskCanceledException` (not requested by the caller) | `InfrastructureUnavailable` | `HttpClient` timeout |

`StudentCvFeedbackProviderException`, which the Ollama provider throws, carries a `Kind` and is
classified through a second lookup table:

| `StudentCvFeedbackFailureKind` | Outcome |
|---|---|
| `ProviderUnavailable` | `InfrastructureUnavailable` |
| `Timeout` | `InfrastructureUnavailable` |
| `InvalidEvaluation` | `InvalidResponse` |

A kind that is not in this table is rethrown.

- Any exception type not in the table is **rethrown**, never hidden.
- A cancellation requested by the caller's own token is **rethrown**.
- An invalid result is classified before it reaches any metric.
- If the very first call is `InfrastructureUnavailable`, the run stops and the report says
  so, instead of filling the report with identical failures.

Two rates are reported separately, so an unreachable service never counts as poor model
quality:

- **Valid-response rate** = Success / (Success + InvalidResponse), over calls that reached the
  model; "n/a" when no call reached it.
- **Unavailability rate** = InfrastructureUnavailable / all calls.

## Pre-flight check

Before any provider call, `EvaluationRunner` verifies that every required configuration value
is present and not blank (for Ollama: `Ai:Ollama:Endpoint` and `Ai:Ollama:Model`). A missing
value stops the run with an `EvaluationSetupException` naming the key; it is never recorded as
a provider outcome.

This matters because the Ollama provider throws `InvalidOperationException` both for missing
configuration and for invalid model output. Only once configuration is guaranteed can that
exception be classified as an invalid response.

## Provider-internal retries

A provider may retry internally (the Ollama provider retries once after invalid model output).
These retries are **invisible to the evaluator**: every rate is measured after them, and each
report says so.

## Offline scripted run (CI)

`OfflineEvaluationRunTests` runs the whole pipeline (dataset, runner, metrics, aggregation,
report) on every `dotnet test`, with no real provider. It uses `ScriptedEvaluationProvider`,
which returns each case's own labels plus three documented perturbations:

1. `negation-03`: the negated skill (Docker) is moved from missing to matched and its
   suggestion removed;
2. `suggestion-mismatch-01`: a suggestion that names no skill is appended;
3. `minimal-01`: a blank summary, so the call is an `InvalidResponse`.

The expected values in that test are computed by hand from the labels and these
perturbations; the comments name the cases behind every number. The offline run **validates
the evaluator only and produces no provider scores**. The keyword baseline and Ollama are
measured only in the opt-in manual run (`INTERNHUB_AI_EVAL=1`). The same run also checks that
the report contains no CV text and no suggestion or summary text.

## Real runs

No real provider run is recorded: `docs/ai-evaluation/results.md` does not exist yet, for any
dataset version. In particular there are no Ollama scores for dataset 2.0.0. Results are
written there by the owner only after an actual opt-in run.

## Run context and privacy

Each report records the run context: provider, model, temperature, prompt version, K (runs per
case), date and commit. The date and context are inputs to the runner and the report writer;
neither reads the current time. Temperature and prompt version are recorded as supplied by the
person running the evaluation; they are labels and do not configure the provider.

Reports contain only case ids, categories, context values and numbers. **CV text and
provider-generated text (skills, suggestions, summaries) are never logged, printed or written.**

## Limitations

- **26 cases give indicative results only.** A single case changes a macro value by at most
  about 0.04 (1/26), and a per-category value rests on 2 to 4 cases. Differences between
  providers should be read as indications, not as statistically significant findings.
- The cases are synthetic, short and in English only; real CVs are longer and more varied.
- The labels were generated with Claude Code from the ai-evaluation skill and reviewed case by
  case by the owner; no inter-annotator agreement was measured.
- Rates are measured after provider-internal retries (see above).
- Temperature and prompt version in a report are labels, not verified provider settings.

## Thresholds

Fixed on 2026-11-08, before the first real Ollama run, and not adjusted after seeing any
results. The same thresholds apply to every provider. A provider meets the thresholds only if
all rows are satisfied.

| Metric | Threshold |
|---|---|
| Matched F1 (micro) | >= 0.70 |
| Missing F1 (micro) | >= 0.70 |
| Hallucination rate (macro) | <= 0.10 |
| Contradiction (macro) | = 0 |
| Valid-response rate | >= 0.95 |

Rationale:

- **F1 >= 0.70** for both lists: the feedback must be right about most skills the candidate has
  and most skills they lack; a lower value would mislead students more often than it helps.
- **Hallucination <= 0.10**: at most one in ten reported skills may fall outside the offer's
  requirements, so students are not sent to prepare for skills nobody asked for.
- **Contradiction = 0**: a skill reported as both present and missing is a logical error that
  makes the feedback unusable.
- **Valid-response rate >= 0.95**: at most one in twenty calls that reach the model may return
  an unusable answer.

Coverage, field completeness, suggestion coverage, suggestion mismatch and stability are
reported without a threshold; they explain the
results rather than decide them.