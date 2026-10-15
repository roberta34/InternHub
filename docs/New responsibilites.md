# InternHub - Balanced Team Work Distribution

## 1. General Working Model

The project is developed by **4 team members** using **1-week Scrum sprints** and a **vertical-slice architecture**.

Each person owns a functional area, but no one is permanently assigned only to frontend, backend, AI, database, or testing.

For the features she owns, each team member works end-to-end with:

- Frontend
- Backend / application logic
- REST APIs
- Database / persistence
- Business rules and validation
- AI-related work
- Unit tests
- Integration tests
- Integration with the rest of the system
- Documentation required by the Definition of Done

The goal is to keep the four areas similar in **workload, complexity, and technical exposure**.

---

# 2. Person 1 - Student Profile + Offer Discovery

## Main area

- **PB-01 - Create Student Profile**
- **PB-03 - Browse Active Offers**
- **PB-04 - View Offer Details**

## Main responsibilities

### Frontend
- create the Student Profile interface;
- create the page for browsing active internship offers;
- create the detailed internship offer view;
- add the student-side CV feedback action for a selected offer;
- display:
  - `matchedSkills[]`;
  - `missingSkills[]`;
  - `suggestions[]`.

### Backend / API
- implement the Student Profile flow;
- expose the APIs needed to create and retrieve student data;
- expose/consume the APIs required for active offers and offer details;
- integrate the student-facing CV feedback flow with:
  `POST /api/offers/{id}/cv-feedback`.

### Database
- persist the Student entity/profile data required by the project;
- read Offer data through the shared Offer contract;
- ensure Student data can be used by the Application slice.

### AI-related work
- integrate the CV feedback endpoint into the student flow;
- send the CV text together with the selected offer context;
- display structured AI feedback;
- ensure the AI feedback remains independent from submitting an application;
- ensure raw CV text is not stored or logged by this flow.

### Testing
- unit tests for student-side validation and application logic;
- integration tests for Student persistence;
- integration tests for Browse/View Offer flows;
- tests for the frontend/backend CV feedback integration.

## Integration with the team

Person 1 mainly integrates with:

- **Person 3**, because Browse/View Offer depends on published Offer data;
- **Person 2**, because Application flows need Student information;
- the shared AI CV Feedback contract.

## Secondary responsibility

**API contracts and shared DTO consistency**

Person 1 helps the team keep common request/response models consistent between the Student, Offer, Application, and AI slices.

---

# 3. Person 2 - Application Lifecycle

## Main area

- **PB-05 - Apply to an Offer**
- **PB-06 - Withdraw Application**
- **PB-08 - View My Applications**

## Main responsibilities

### Frontend
- implement the Apply action from an internship offer;
- create the My Applications view;
- show:
  - company name;
  - offer title;
  - current application status;
- implement the Withdraw action when the current status allows it.

### Backend / API
- implement Submit Application;
- implement View My Applications;
- implement Withdraw Application;
- return expected business outcomes using `Result<T>`;
- enforce all application-related business rules in the backend.

### Database
- persist and retrieve Application entities;
- work with StudentId and OfferId relationships;
- preserve the valid application statuses:
  - Submitted;
  - Shortlisted;
  - Accepted;
  - Rejected;
  - Waitlisted;
  - Withdrawn;
- help enforce application-related integrity constraints.

### Business rules
- reject applications after the offer deadline;
- reject new applications to manually closed offers;
- reject duplicate active applications for the same student and offer;
- enforce the maximum number of active applications;
- allow withdrawal only from the permitted states;
- trigger recruiter notification when a Shortlisted application is withdrawn.

### AI-related work
- implement/work with the deterministic **Fake AI provider** used by automated tests;
- support predictable fake responses for both student CV feedback and recruiter-assisted application review;
- provide deterministic `matchedSkills`, `missingSkills`, and `suggestions` for the student flow;
- provide deterministic `matchedSkills`, `missingOrUnclearSkills`, `evidence`, and `summary` for the recruiter flow;
- ensure automated AI tests do not depend on a live Ollama instance.

### Testing
- unit tests for all application business rules;
- tests for success and failure scenarios;
- integration tests for Application persistence;
- tests for deadline-dependent behavior;
- tests for allowed and disallowed withdrawal cases;
- deterministic tests for the Fake AI provider.

## Integration with the team

Person 2 mainly integrates with:

- **Person 1**, for Student data;
- **Person 3**, for Offer data, deadline, `IsClosed`, and requirements;
- **Person 4**, because recruiter decisions operate on the same Application entity.

## Secondary responsibility

**Database migrations and data integrity**

Person 2 helps maintain schema changes, relationships, constraints, and consistent persistence behavior.

---

# 4. Person 3 - Internship Offer Management

## Main area

- **PB-02 - Publish Internship Offer**
- **PB-09 - Close Internship Offer**

## Main responsibilities

### Frontend
- create the recruiter-side interface for publishing an internship offer;
- implement the action for manually closing an offer;
- show the relevant state of the offer to the recruiter.

### Backend / API
- implement Publish Internship Offer;
- implement Close Internship Offer;
- expose Offer information required by the other slices;
- ensure the Offer contract remains usable by Student, Application, Recruiter Decision, and AI flows.

### Database
- persist the Offer entity;
- manage:
  - Id;
  - Title;
  - Company Name;
  - Requirements;
  - Deadline;
  - Maximum Available Slots;
  - `IsClosed`;
  - `Version`;
- configure persistence for the `Version` property used by optimistic concurrency.

### Business rules
- require the fields defined by the backlog when publishing an offer;
- ensure manually closed offers no longer accept new applications;
- ensure existing applications remain valid and can continue through the recruitment process.

### AI-related work
- implement/work with the local **Ollama provider**;
- support both student CV feedback and recruiter-assisted application review;
- send CV text and Offer requirements to the local model;
- use separate prompt/output contracts where the two use cases require different results;
- use `HttpClient`;
- use versioned prompts stored in the repository;
- use low-temperature generation;
- validate the returned structured JSON;
- perform one retry when the returned structure is invalid;
- return a clear failure result when Ollama/model execution fails;
- ensure the rest of the application continues working when AI is unavailable;
- ensure the AI provider never performs or triggers a recruitment decision.

### Testing
- unit tests for Offer lifecycle rules;
- integration tests for Offer persistence;
- tests for closed-offer behavior;
- tests for the Ollama provider abstraction and failure handling without making the full automated test suite depend on live Ollama.

## Integration with the team

Person 3 provides one of the most important shared contracts in the project.

Offer data is used by:

- **Person 1** for Browse/View Offer and AI feedback;
- **Person 2** for Apply/Withdraw rules;
- **Person 4** for slot and concurrency logic;
- the AI component for Offer requirements.

## Secondary responsibility

**Docker / CI / local development environment**

Person 3 helps keep the project reproducible and ensures the local environment supports the application, database, automated tests, and local Ollama setup.

---

# 5. Person 4 - Application Evaluation + Slot Management

## Main area

- **PB-07 - Decide Application**
- **PB-11 - AI-Assisted Application Review**
- limited-slot handling
- optimistic concurrency requirements

## Main responsibilities

### Frontend
- create the recruiter-side application evaluation view;
- allow the recruiter to request AI analysis for a selected application;
- display recruiter-facing AI results:
  - `matchedSkills[]`;
  - `missingOrUnclearSkills[]`;
  - `evidence[]`;
  - `summary`;
- keep the original candidate information available to the recruiter;
- expose Accept and Reject actions separately from the AI analysis;
- display the resulting application status.

### Backend / API
- implement Decide Application;
- expose the recruiter decision API;
- expose the recruiter-facing AI analysis flow for a selected application;
- send the candidate CV and associated Offer requirements to the shared AI provider;
- return structured AI analysis without changing the Application status;
- implement the transition to:
  - Accepted;
  - Rejected;
  - Waitlisted;
- ensure the final decision always requires an explicit recruiter action.

### Database
- update Application status safely;
- read the Application and associated Offer data required for recruiter evaluation;
- read and update Offer slot-related state;
- use the Offer `Version` property for optimistic concurrency;
- preserve data consistency during simultaneous decisions;
- do not persist raw CV text only for the purpose of AI analysis.

### Business rules / concurrency
- AI assistance is optional for the recruiter;
- the recruiter can evaluate and decide an application without using AI;
- AI analysis may support the recruiter with information, but it cannot make the final decision;
- AI analysis cannot automatically shortlist, accept, reject, or waitlist a candidate;
- AI analysis cannot directly change an Application status;
- prevent accepted candidates from exceeding the configured maximum slots;
- if no slot remains, an explicit recruiter Accept action results in `Waitlisted`;
- handle two recruiters trying to accept different candidates for the final slot at the same time;
- ensure one candidate becomes `Accepted` and the other `Waitlisted`.

### AI / ML-related work
- integrate AI-assisted analysis into the recruiter application evaluation flow;
- compare the candidate CV with the requirements of the selected internship offer;
- present the AI result as supporting information for the recruiter;
- keep recruiter decision logic independent from AI output;
- prepare **10-15 synthetic CV-offer pairs**;
- manually label expected skills;
- evaluate at least **2 models**;
- repeat evaluation several times;
- compare against a trivial baseline;
- report:
  - valid-JSON rate;
  - overlap with manually labelled skills;
- document the results and chosen thresholds.

### Testing
- unit tests for recruiter decision rules;
- integration tests for Application status updates;
- tests for recruiter-facing AI analysis;
- tests proving that AI analysis alone never changes an Application status;
- tests proving that the recruiter can still decide when AI is unavailable;
- tests for no-slot / Waitlisted behavior;
- simultaneous-request concurrency tests;
- tests proving that Accept/Reject actions remain explicitly human-controlled.

## Integration with the team

Person 4 mainly integrates with:

- **Person 2**, because both work with the Application lifecycle and candidate application data;
- **Person 3**, because concurrency depends on Offer slots and `Version`, and recruiter AI analysis uses the shared Ollama provider;
- **Person 1**, through the shared AI analysis conventions used by both student and recruiter flows;
- the shared status, Offer, Application, and AI contracts.

## Secondary responsibility

**Testing strategy, security and quality checks**

Person 4 helps ensure:

- student/recruiter role separation;
- only the authorized recruiter can evaluate applications for an offer;
- AI analysis remains advisory and cannot execute recruitment decisions;
- privacy-sensitive data does not appear in logs or errors;
- shared testing conventions are respected;
- important AI integration, application evaluation, and concurrency scenarios are covered.

---

# 6. Balanced Distribution Summary

| Person | Main functional area | AI-related responsibility | Secondary responsibility |
|---|---|---|---|
| **Person 1** | Student Profile + Browse/View Offers | Student CV feedback frontend/API integration | API contracts / DTO consistency |
| **Person 2** | Application lifecycle | Deterministic Fake AI provider for both AI flows | Database migrations / data integrity |
| **Person 3** | Offer lifecycle | Shared Ollama provider for student and recruiter AI flows | Docker / CI / local environment |
| **Person 4** | Recruiter decisions + concurrency + AI-assisted review | Recruiter AI integration + AI/ML model evaluation | Testing / security / quality |

All four members therefore work with:

- product functionality;
- frontend;
- backend;
- APIs;
- persistence;
- testing;
- integration;
- one defined part of the AI/ML work.

The exact workload should still be estimated during Sprint Planning. If one slice is significantly larger than the others, it should be split or reduced before the sprint starts.

---

# 7. Integration Between Team Members

The four members should agree on shared contracts before implementing dependent features.

## Shared Student contract

Mainly used by:

- Person 1 - Student Profile;
- Person 2 - Application lifecycle.

## Shared Offer contract

Used by:

- Person 1 - Browse/View Offers and student CV Feedback;
- Person 2 - Apply and withdrawal-related rules;
- Person 3 - Publish/Close Offer;
- Person 4 - slot/concurrency logic and recruiter AI-assisted review;
- shared AI analysis - Offer requirements.

Required Offer information includes:

- Id
- Title
- Company Name
- Requirements
- Deadline
- Maximum Available Slots
- IsClosed
- Version

## Shared Application contract

Used by:

- Person 2 - student-side lifecycle;
- Person 4 - recruiter evaluation and recruiter AI-assisted review.

Allowed statuses:

- Submitted
- Shortlisted
- Accepted
- Rejected
- Waitlisted
- Withdrawn

---

# 8. Independent Development Strategy

The goal is not to remove all dependencies, because the Product Backlog contains real dependencies.

Instead, the team should follow:

> **Contract first, implementation independently, integration afterwards.**

When a dependency exists:

1. agree on the domain/API contract;
2. use seeded data, Fake providers, or InMemory implementations temporarily;
3. implement the slice independently;
4. replace temporary dependencies with the real implementation when available;
5. run integration tests before merge.

Example:

```text
                        Shared Offer Contract
                               |
         +---------------------+---------------------+
         |                     |                     |
         v                     v                     v
     Person 1              Person 2              Person 4
  Browse / View              Apply            Decide / Slots
         ^                                           ^
         |                                           |
         +--------------- Person 3 ------------------+
                    Publish / Close Offer

PB-10 Student AI CV Feedback and PB-11 Recruiter AI-Assisted Application Review both use the same Offer requirements contract.
```

---

# 9. Definition of Done Applied to Every Slice

A feature is considered complete only when all applicable Definition of Done criteria are satisfied.

Each person must ensure that her slice includes:

- all acceptance criteria;
- backend enforcement of business rules;
- validation and error handling;
- `Result<T>` for expected success/failure outcomes;
- frontend-backend integration;
- persistence integration where applicable;
- unit tests;
- integration tests;
- success and failure cases;
- deterministic deadline handling where required;
- concurrency tests where required;
- privacy protection for CV data;
- student/recruiter role separation;
- AI remains advisory where used;
- recruiter-facing AI analysis never changes Application status by itself;
- final recruiter decisions always require an explicit human action;
- documented endpoints and request/response formats;
- successful build;
- code review before merge.

---

# 10. Open Requirements

The team should not invent behavior where the current requirements are still unclear.

The following points require clarification:

- how recruiters are represented and authenticated;
- which application statuses count as active;
- how the waiting list is ordered;
- whether an Accepted candidate may withdraw and free a slot;
- whether a closed offer can be reopened;
- the exact expected behavior when local Ollama is unavailable.

Affected features should avoid hard-coded assumptions until these points are clarified.

---

# 11. Core Rule

> **Each person owns a complete functional area, one defined AI/ML responsibility, and one secondary responsibility, while all four develop end-to-end vertical slices and integrate through shared contracts.**
