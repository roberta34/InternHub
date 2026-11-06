---
name: ai-baseline
description: Generate deterministic, explainable CV skill evaluators for InternHub without external AI services, machine learning, persistence, or automated recruitment decisions.
---

# AI Baseline Skill - InternHub

Generate production services that implement the existing recruiter-evaluation contract
using a small explicit vocabulary and deterministic text matching.

## Rules

- Do not use Ollama, ML.NET, machine-learning models, hosted AI services, or external services.
- Do not add NuGet packages or other dependencies.
- Do not log, persist, export, or otherwise retain CV content.
- Produce reproducible results for identical inputs, with stable skill ordering and duplicate removal.
- Report matched and missing skills and simple suggestions, but never make an automated hiring or rejection decision.
- Preserve and implement the existing `IRecruiterEvaluationService` interface and `RecruiterEvaluationResult` contract exactly.
- Validate null arguments with clear argument exceptions and handle empty CV or requirements explicitly.
- Use case-insensitive matching with correct term boundaries; do not allow substrings such as `Java` in `JavaScript` or `SQL` inside another word.
- Respect the supplied `CancellationToken` and avoid unnecessary asynchronous work.
