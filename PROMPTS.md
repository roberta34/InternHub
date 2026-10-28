# InternHub - PROMPTS.md

Short log of relevant Sprint 2 AI interactions. Each member adds their own entries.

## CQRS - Person 1

**Skill referenced:** `.claude/skills/cqrs/SKILL.md`  

| Prompt summary | Result |
|---|---|
| Convert `CreateStudent` to Command + Handler + thin Endpoint; reuse `Result<T>` and DI. | CQRS generated; initial namespace mismatch caused a test compilation error. Copilot corrected the references. |
| Convert `GetStudentById` to Query + Handler, returning 200/404. | Implemented; `dotnet build` passed, 60/60 tests passed, including 3 filtered GetById tests. |
| Convert `GetStudents` to read-only Query + Handler, returning 200 even for an empty list. | Implemented; build and tests reported passing. |

**CQRS skill changes:** None documented so far. The namespace error was fixed through Copilot, not by editing and rerunning the skill.

## Postman - Person 1

**Tool:** GitHub Copilot Agent (Postman task, not a Claude skill execution).

- **Prompt:** Add Student Create/GetById/GetAll requests, versioned URLs, variables and response checks.
- **Problem:** Generated collection initially lacked `/api/v1`, `apiVersion` and `GetStudents`.
- **Follow-up:** Asked Copilot to correct the JSON and add missing scenarios.
- **Result:** 7 requests across 3 folders; imported and reported passing in Postman.

## Skill improvement (required before submission)

**Pending:** Record one *real* failed generation, the exact change to a `SKILL.md` file, regeneration with Claude and the verified before/after result. Do not invent an iteration.

**Compliance note:** Assignment 2 requires Claude-generated production and test code through the team's skills. The Copilot runs above do not fulfill that requirement.
