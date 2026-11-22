---
name: ai-baseline
description: Generate and maintain deterministic, explainable CV skill evaluators for InternHub without external AI services, machine learning, persistence, or automated recruitment decisions.
---

# AI Baseline Skill - InternHub

Generate and maintain production services that implement the existing recruiter-evaluation contract
using an explicit canonical technology vocabulary, explicit aliases, and deterministic boundary-aware
text matching.

## Core Rules

- Do not use Ollama, ML.NET, machine-learning models, hosted AI services, or external services.
- Do not add NuGet packages or other dependencies.
- Do not change shared interfaces (`IRecruiterEvaluationService`, `RecruiterEvaluationResult`), DI registrations, or API endpoints.
- Do not log, persist, export, or otherwise retain CV content.
- Produce reproducible results for identical inputs, with duplicate removal and stable canonical vocabulary ordering.
- Report matched skills, missing skills, advisory suggestions, and a coherent summary, but never make an automated hiring or rejection decision.
- Validate null arguments explicitly (`ArgumentNullException` for `cvContent` and `offerRequirements`).
- Handle empty or whitespace CV and requirements explicitly:
  - an empty CV never marks required skills as matched;
  - empty or unrecognized requirements return empty skill/suggestion lists with an informative summary (`"No recognized skills were found in the internship requirements."`).
- Respect the supplied `CancellationToken` (`ThrowIfCancellationRequested`) and avoid artificial asynchronous work (`Task.FromResult`).

## Matching and Boundary Rules

1. **Case-insensitive, culture-invariant matching:** match terms with `RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled` and an explicit regex timeout.
2. **Punctuation-aware boundaries:**
   - Left boundary forbids letters, digits, `_`, `#`, `+`, and `.` (`(?<![\p{L}\p{N}_#+.])`) so file extensions (e.g. `app.js`, `schema.sql`), domain names (e.g. `example.net`), and prefixed symbols (e.g. `..NET`, `#C#`, `++C++`) do not trigger false positives.
   - Right boundary forbids letters, digits, `_`, `#`, `+`, and a dot immediately followed by an identifier character (`(?![\p{L}\p{N}_#+]|\.(?=[\p{L}\p{N}_]))`) so `C#` does not match `C##` or `C#++`, `C++` does not match `C+++`, `Java` does not match `JavaScript`, and domain names/filenames (e.g. `python.org`, `git.example.com`, `sql.txt`) do not false-positive, while allowing sentence punctuation (`.`, `,`, `;`, `)`, `/`).
3. **Multiword whitespace matching:** spaces inside multiword terms (e.g. `SQL Server`, `Entity Framework Core`, `Unit Testing`, `REST API`) match `\s+`.
4. **Longest-span-first non-overlapping extraction:**
   - Matchers are ordered by term length descending, then canonical vocabulary order.
   - Once a longer/compound term claims a character span, shorter terms overlapping that span are ignored.
   - This prevents false positives between:
     - `SQL` and `SQL Server` (as well as `MS SQL`, `MS SQL Server`, `Microsoft SQL Server`, `Postgre SQL`, `No SQL`);
     - `Java` and `JavaScript` (including `Java Script`);
     - `C#` and `C++`;
     - `.NET` and `ASP.NET Core` / `ASP.NET`.
5. **No unsafe alias mappings:** do not map distinct products/concepts together (e.g. `GitHub` is not `Git`, `NoSQL` is not `SQL`, `SQL Server` is not `SQL`, `Docker Compose` is not `Docker`), and do not register standalone common English words as case-insensitive aliases (e.g. exclude standalone `REST` so `"rest"` in prose does not match `REST API`).

## Explicit Canonical Vocabulary and Aliases

Preserve the original relative order of the initial 10 canonical skills (`Python`, `Java`, `JavaScript`, `SQL`, `C#`, `C++`, `.NET`, `React`, `Docker`, `Git`) for stable output ordering:

| Canonical Skill | Explicit Aliases |
|---|---|
| `Python` | `Python`, `Python3`, `Python 3` |
| `Java` | `Java` |
| `JavaScript` | `JavaScript`, `JS`, `Java Script`, `ECMAScript` |
| `SQL` | `SQL` |
| `C#` | `C#`, `CSharp`, `C Sharp`, `C-Sharp` |
| `C++` | `C++`, `CPP` |
| `.NET` | `.NET`, `DotNet`, `Dot Net`, `.NET Core` |
| `React` | `React`, `ReactJS`, `React.js` |
| `Docker` | `Docker` |
| `Git` | `Git` |
| `SQL Server` | `SQL Server`, `SQLServer`, `MS SQL Server`, `Microsoft SQL Server`, `MS SQL`, `MSSQL` |
| `TypeScript` | `TypeScript`, `TS`, `Type Script` |
| `PostgreSQL` | `PostgreSQL`, `Postgres`, `Postgre SQL` |
| `MySQL` | `MySQL` |
| `SQLite` | `SQLite` |
| `NoSQL` | `NoSQL`, `No SQL` |
| `MongoDB` | `MongoDB`, `Mongo` |
| `ASP.NET Core` | `ASP.NET Core`, `ASP.NET`, `ASPNET Core` |
| `Entity Framework Core` | `Entity Framework Core`, `EF Core`, `Entity Framework` |
| `Node.js` | `Node.js`, `NodeJS`, `Node JS` |
| `Angular` | `Angular`, `AngularJS` |
| `Vue.js` | `Vue.js`, `VueJS`, `Vue` |
| `HTML` | `HTML`, `HTML5` |
| `CSS` | `CSS`, `CSS3` |
| `REST API` | `REST API`, `RESTful API`, `REST APIs`, `RESTful APIs`, `RESTful` |
| `GraphQL` | `GraphQL` |
| `Unit Testing` | `Unit Testing`, `Unit Tests`, `Unit Test`, `Unit-Testing` |
| `Linux` | `Linux` |
| `Kubernetes` | `Kubernetes`, `K8s`, `Kube` |
| `Azure` | `Azure`, `Microsoft Azure` |
| `AWS` | `AWS`, `Amazon Web Services` |
| `CI/CD` | `CI/CD`, `CICD` |
| `Docker Compose` | `Docker Compose`, `Docker-Compose` |
