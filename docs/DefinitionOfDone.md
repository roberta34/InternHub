# Definition of Done: InternHub

A user story is **Done** when all applicable criteria below are met.

## 1. Functional Requirements

- [ ] All acceptance criteria (AC) of the user story are implemented and verified.
- [ ] Behavior follows the business rules and domain constraints.
- [ ] Input validation and error handling are implemented for the relevant scenarios.
- [ ] Operations return a success or failure `Result<T>` depending on the outcome.
- [ ] Existing functionality is not broken.

## 2. Code Quality and Architecture

- [ ] Code follows the agreed conventions and project structure (vertical slices).
- [ ] Business rules live in the domain/application layer, independent of AI and infrastructure.
- [ ] External dependencies are accessed through interfaces, injected via DI.
- [ ] Configuration-dependent behavior is controlled from `appsettings.json`.
- [ ] AI-assisted application evaluation is separated from recruiter decision logic.

## 3. Testing

- [ ] Unit tests cover business rules, validation, and edge cases.
- [ ] Integration tests cover interaction with persistence.
- [ ] Tests cover both success and failure scenarios.
- [ ] Time-dependent rules (deadline) use `TimeProvider` for deterministic tests.
- [ ] Concurrent operations (last slot) are tested under simultaneous requests.
- [ ] AI-assisted recruiter evaluation is tested independently from recruiter decision logic.
- [ ] The recruiter can still evaluate and decide applications when the AI service is unavailable.
- [ ] All automated tests pass.

## 4. Data Integrity and Business Rules

- [ ] Application statuses follow the allowed transitions: `Submitted`, `Shortlisted`, `Accepted`, `Rejected`, `Waitlisted`, `Withdrawn`.
- [ ] One active application per student per offer; at most 5 active applications per student.
- [ ] Applying after the deadline or after manual closure of the offer returns a failure `Result<T>`.
- [ ] Manually closing an offer invalidates the original deadline; existing applications can still move through the evaluation states.
- [ ] Withdrawal is allowed only from `Submitted` or `Shortlisted` (to be confirmed with the team/professor, see note below); it is not allowed after `Accepted` or `Rejected`.
- [ ] When a `Shortlisted` application is withdrawn, the recruiter is notified.
- [ ] If no slots remain, `Accept` automatically results in `Waitlisted`; on a withdrawal, the first in the queue is promoted.
- [ ] Two simultaneous `Accept` decisions on the last slot are resolved with `Version` (one `Accepted`, the other `Waitlisted`).
- [ ] Rules are enforced in the backend, not only in the UI; database constraints and optimistic concurrency are configured.
- [ ] The recruiter may optionally use AI-assisted CV analysis when evaluating an application.
- [ ] `DecideApplication` remains a human-controlled operation.
- [ ] AI analysis may support the recruiter with relevant information, but it must never automatically change the application status.
- [ ] Only an explicit recruiter action can result in `Accepted`, `Rejected`, or another recruiter-controlled status transition.

## 5. AI Module: CV Feedback and Recruiter-Assisted Application Evaluation

The AI module supports two separate use cases:

1. student-facing CV feedback;
2. recruiter-facing application evaluation assistance.

The AI remains advisory in both cases.

### 5.1 Shared AI Infrastructure

- [ ] AI functionality is accessed through an abstraction/interface with **Ollama** and **Fake** implementations.
- [ ] The active AI provider is selected through `Ai:Provider` in `appsettings.json`.
- [ ] The Ollama integration uses `HttpClient`.
- [ ] AI responses are validated with `System.Text.Json`.
- [ ] Invalid structured responses are handled safely, with a maximum of one retry when applicable.
- [ ] Prompts are stored in versioned files in the repository.
- [ ] AI service failure returns a clear failure `Result` without crashing the application.
- [ ] Standard InternHub functionality remains operational when Ollama or the selected model is unavailable.
- [ ] The Fake implementation is deterministic and automated tests never depend on a live Ollama instance.

### 5.2 Student AI CV Feedback

- [ ] `POST /api/offers/{id}/cv-feedback` returns structured feedback for the student.
- [ ] The response contains:

```json
{
  "matchedSkills": [],
  "missingSkills": [],
  "suggestions": []
}
```

- [ ] Each `missingSkill` has a corresponding actionable recommendation in `suggestions`.
- [ ] The student can use the AI analysis to understand how well their CV matches a selected internship offer.
- [ ] Student-facing AI feedback does not affect the application status.
- [ ] Student-facing AI feedback is independent from submitting an application.

### 5.3 Recruiter AI-Assisted Application Evaluation

- [ ] A recruiter can request AI analysis for a selected application.
- [ ] The AI compares the candidate's CV with the requirements of the internship offer associated with that application.
- [ ] Recruiter-facing AI analysis is optional and is requested on demand.
- [ ] The recruiter-facing result contains structured information such as:

```json
{
  "matchedSkills": [],
  "missingOrUnclearSkills": [],
  "evidence": [],
  "summary": ""
}
```

- [ ] `matchedSkills` identifies offer requirements that are supported by information found in the candidate's CV.
- [ ] `missingOrUnclearSkills` identifies requirements that are missing or cannot be clearly demonstrated from the CV.
- [ ] `evidence` provides relevant CV information supporting the AI analysis when applicable.
- [ ] `summary` provides a concise overview of how the candidate's CV relates to the internship requirements.
- [ ] The AI result is presented to the recruiter as supporting information only.
- [ ] The recruiter can inspect the original candidate information and is not required to follow the AI analysis.
- [ ] AI analysis does not automatically shortlist a candidate.
- [ ] AI analysis does not automatically accept a candidate.
- [ ] AI analysis does not automatically reject a candidate.
- [ ] AI analysis does not automatically assign `Waitlisted`.
- [ ] AI analysis never changes the application status.
- [ ] The final recruitment decision always requires an explicit recruiter action.
- [ ] Recruiter decisions remain possible without using AI.

### 5.4 AI Evaluation and Quality

- [ ] An `Evaluate` tool runs on 10-15 CV-offer pairs with manually labelled skills.
- [ ] Evaluation data is synthetic and clearly marked as synthetic.
- [ ] The evaluation measures the valid-JSON rate.
- [ ] The evaluation measures overlap between expected skills and AI-extracted skills.
- [ ] The evaluation is run on at least 2 models.
- [ ] Each model is evaluated several times.
- [ ] Results are compared against a trivial baseline.
- [ ] Model evaluation covers outputs relevant to both student feedback and recruiter-assisted review where applicable.
- [ ] Evaluation results, model choice, and chosen thresholds are documented in the README.

### 5.5 AI Privacy

- [ ] CV text sent to the AI service is not stored in the application database.
- [ ] Raw CV text is not written to application logs.
- [ ] Raw CV text does not appear in error messages.
- [ ] AI-related failures do not expose sensitive candidate information.

## 6. Security and Privacy

- [ ] Sensitive data does not appear in logs, API responses, or error messages.
- [ ] API inputs are validated.
- [ ] Student and recruiter role separation is respected.
- [ ] A student sees only their own applications.
- [ ] Only the authorized recruiter for an internship offer can evaluate its applications.
- [ ] Only the authorized recruiter can request recruiter-facing AI analysis for applications belonging to their offer.
- [ ] Only the recruiter can perform the final recruitment decision.
- [ ] AI output cannot directly invoke or trigger application-status transitions.

## 7. Documentation and Delivery

- [ ] Endpoints and request/response formats are documented.
- [ ] Student-facing AI CV feedback is documented.
- [ ] Recruiter-facing AI-assisted application evaluation is documented.
- [ ] The documentation clearly states that AI provides advisory analysis and does not make recruitment decisions.
- [ ] Setup and configuration, including Ollama and models, are documented in the README.
- [ ] AI failure and fallback behavior are documented.
- [ ] The application builds successfully.
- [ ] Changes are merged into the agreed branch after code review.
