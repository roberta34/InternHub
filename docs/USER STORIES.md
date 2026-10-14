USER STORIES:





**US1**: As a student, I want to receive automated feedback on my CV against an internship offer, so that I know which skills I am missing before applying.



AC1: The system takes the CV text and the offer requirements and returns a strictly formatted JSON response (matchedSkills, missingSkills, suggestions).



AC2: Each item in the missingSkills list must be directly tied to a specific, actionable recommendation in the suggestions array, guiding the student on how to acquire or demonstrate that skill.



AC3: The CV text sent to the local Ollama model is never saved in the database or logged anywhere.



AC4: The AI module acts strictly in an advisory capacity for the student; it does not automatically reject or rank the candidate for the company.





**US2**: As a recruiter, I want to publish an internship offer with a limited number of slots and a deadline, so that I can attract candidates.



AC1: The offer must include a title, company name, requirements, a deadline, and the maximum number of available slots.



AC2: The offer is assigned a Version property to handle optimistic concurrency.





**US3**: As a student, I want to browse active offers, so that I can find suitable internships.



AC1: A student can have at most 5 active applications across the entire system at any given time.



AC2: A student can have only one active application per specific offer.



AC3: The system strictly rejects any application if the offer's deadline has passed.





**US4**: As a student, I want to be able to withdraw my application, so that I can free up one of my 5 active application slots.



AC1: Withdrawing is only permitted if the application status is Submitted or Shortlisted. It is not allowed once accepted or rejected.



AC2: If a student withdraws a Shortlisted application, the system automatically notifies the recruiter, in case they would like to promote another candidate for Shortlisted.





**US5**: As a recruiter, I want to accept candidates for my offer, so that I can fill the available internship slots.



AC1: The recruiter can evaluate an application and change its status to Accepted or Rejected.



AC2: If the recruiter accepts a candidate but the available slots have already reached 0, the system automatically assigns the Waitlisted status instead of Accepted.



AC3: If exactly one slot remains and two recruiters click "Accept" for two different candidates simultaneously, the database resolves the race condition using the offer's Version property. One candidate is Accepted, and the other becomes Waitlisted.





**US6**: As a student, I want to view a dashboard of all my applications, so that I can track my current status in the recruitment processes.



AC1: The system displays a list of all applications made by the student, showing the company, offer title, and the real-time status (Submitted, Shortlisted, Accepted, Rejected, Waitlisted, Withdrawn).



AC2: The list is ordered by the application date in descending order. 





**US7**: As a recruiter, I want to manually close an internship offer before its deadline, so that I can stop receiving new CVs once I have enough strong candidates.



AC1: The recruiter can trigger a manual closure of the offer, which immediately invalidates the original deadline.



AC2: Once the offer is marked as closed, the system returns a failure Result<T> for any new application attempts.



AC3: Existing applications remain active and can still transition through the evaluation states (e.g., from Submitted to Shortlisted or Accepted).



