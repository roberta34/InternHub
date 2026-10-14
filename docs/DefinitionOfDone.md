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

## 3. Testing

- [ ] Unit tests cover business rules, validation, and edge cases.
- [ ] Integration tests cover interaction with persistence.
- [ ] Tests cover both success and failure scenarios.
- [ ] Time-dependent rules (deadline) use `TimeProvider` for deterministic tests.
- [ ] Concurrent operations (last slot) are tested under simultaneous requests.
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
- [ ] `DecideApplication` remains a human-only decision and does not depend on the AI module.

## 5. AI Module: CV Feedback

- [ ] The handler depends on the `ICvFeedbackProvider` interface (its own slice), with **Ollama** and **Fake** implementations, selected via `Ai:Provider` in `appsettings.json`.
- [ ] `POST /api/offers/{id}/cv-feedback` returns JSON with `matchedSkills[]`, `missingSkills[]`, and `suggestions[]`.
- [ ] The response is validated with `System.Text.Json`, with a single retry if invalid.
- [ ] Each `missingSkill` has a corresponding actionable recommendation in `suggestions`.
- [ ] The Ollama call uses `HttpClient` with a low temperature, and the prompt lives in a versioned file in the repo.
- [ ] Non-AI fallback: if Ollama or the model is unavailable, or the response stays invalid, the endpoint returns a clear failure `Result` without crashing, and the rest of the application keeps working.
- [ ] The Fake implementation is deterministic (e.g., keyword matching); tests never depend on Ollama.
- [ ] The AI result is advisory feedback for the student only; it never ranks or rejects candidates.
- [ ] CV text is not stored in the database and is not logged, including in error messages.
- [ ] An `Evaluate` tool runs on 10–15 CV–offer pairs with manually labelled skills (synthetic data, marked as such) and prints the valid-JSON rate and the overlap with the labels.
- [ ] The evaluation is run on at least 2 models, repeated several times, and compared against a trivial baseline; results and chosen thresholds are documented in the README.

## 6. Security and Privacy

- [ ] Sensitive data does not appear in logs, API responses, or error messages.
- [ ] API inputs are validated; student/recruiter role separation is respected (a student sees only their own applications, only the offer's recruiter makes decisions).

## 7. Documentation and Delivery

- [ ] Endpoints and request/response formats are documented; setup and configuration (including Ollama and models) are in the README.
- [ ] The application builds, and changes are merged into the agreed branch after code review.

