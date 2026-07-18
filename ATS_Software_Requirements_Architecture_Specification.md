# ATS (Applicant Tracking System) — Software Requirements & Architecture Specification

**Document Type:** Full Software Requirements & Architecture Specification (SRAS)
**Intended Consumer:** AI coding agent (e.g., OpenAI Codex) and human engineering team
**Target Stack Assumption:** .NET 8, C#, Clean Architecture, CQRS + MediatR, EF Core, SQL Server, REST API
**Version:** 1.0
**Status:** Draft for implementation

> This document is a specification only. It contains no code. Every section is written so an implementing engineer or AI coding agent can build the system without needing to ask clarifying questions about scope, rules, or structure.

---

## Table of Contents

1. Project Overview
2. User Roles
3. Functional Requirements
4. Features
5. Database Design
6. Domain Model
7. CQRS Design
8. API Endpoints
9. Authentication
10. Notifications
11. Workflow
12. Business Rules
13. Non-Functional Requirements
14. Logging & Monitoring
15. Architecture
16. Folder Structure
17. Coding Standards
18. Future Improvements
19. Development Roadmap
20. Final Deliverables

---

# 1. Project Overview

## 1.1 Business Problem

Companies — particularly small-to-mid-sized businesses and agencies without an enterprise HR suite — struggle to manage recruitment in a structured, auditable, and collaborative way. Today they typically rely on a patchwork of:

- Spreadsheets shared over email to track candidates, which have no access control, no history, and break down once more than two people edit them.
- Generic inboxes (`careers@company.com`) that mix candidate applications with spam and internal discussion, making it impossible to know who is responsible for a candidate at any point in time.
- Ad-hoc interview scheduling over chat/email, causing double-booked interviewers and no centralized feedback trail.
- No single source of truth for **why** a candidate was rejected or hired, which creates compliance, fairness, and knowledge-continuity risk when a recruiter leaves the company.
- Manual, inconsistent offer and communication processes that damage candidate experience and slow down time-to-hire.

The result: recruiters lose track of candidates, hiring managers can't see pipeline health, interview feedback is inconsistent or missing, and companies cannot report on hiring metrics (time-to-hire, source effectiveness, drop-off rate) because the data doesn't exist in structured form anywhere.

## 1.2 Project Goals

1. Provide a **multi-tenant** Applicant Tracking System where multiple companies can each manage their own recruitment independently and securely, with zero data leakage between tenants.
2. Give every recruitment stakeholder (recruiter, HR, interviewer, hiring manager/company admin) a **role-appropriate view** of candidates, jobs, and pipeline status.
3. Standardize the **hiring pipeline** into explicit, trackable stages so every candidate's status is always known and auditable.
4. Centralize **interview scheduling and feedback** to eliminate double-booking and ensure structured, comparable feedback across interviewers.
5. Provide **notifications** (email + in-app) so no candidate or internal stakeholder is left waiting without communication.
6. Provide **reporting and dashboards** so company admins and HR can see hiring funnel health, bottlenecks, and team performance.
7. Build the system on a **clean, testable, maintainable architecture** (Clean Architecture + CQRS) so it can scale in complexity (new modules, new integrations) without becoming unmaintainable.
8. Produce **full audit trails** for every meaningful action taken in the system, supporting compliance and internal accountability.
9. Design the system so it is **API-first**, enabling a future web frontend, mobile app, or third-party integration to consume the same backend without modification.

## 1.3 Target Users

| User Type | Description |
|---|---|
| **Recruitment agencies** | Manage hiring pipelines for multiple client companies (multi-tenant use case). |
| **SME/mid-size companies** | Manage their own internal hiring without needing an enterprise HRIS like Workday. |
| **HR departments** | Own company-wide hiring policy, oversee recruiters, manage offer and onboarding handoff. |
| **Hiring managers / Interviewers** | Participate in the pipeline for specific jobs, submit structured feedback. |
| **Candidates (external users)** | Apply for jobs, track their own application status, upload/manage CVs, schedule interview slots when invited. |
| **Platform operators (Super Admin)** | Anthropic-internal-style operator role that manages the SaaS platform itself: companies (tenants), platform-wide settings, billing status, and system health. |

## 1.4 Key Benefits

- **Single source of truth** for every candidate, job, interview, and decision.
- **Reduced time-to-hire** through structured pipeline stages and automated notifications.
- **Fair, auditable decisions** via mandatory structured interview feedback and full audit logging.
- **No double-booked interviews** via server-enforced interview overlap validation.
- **Data-driven hiring** via dashboards and reports (funnel conversion, time-in-stage, source effectiveness).
- **Secure multi-tenancy** — each company's data is fully isolated; Super Admins can operate the platform without being able to silently read candidate PII outside of legitimate support scenarios (see §9 and §12 for constraints).
- **Extensible foundation** — Clean Architecture and CQRS mean new modules (AI CV ranking, calendar integrations) can be added without destabilizing the core.

---

# 2. User Roles

The system supports six roles. Roles are **hierarchical in scope but not strictly hierarchical in permission** — a Company Admin has more scope than a Recruiter within their company, but a Super Admin does **not** automatically get unrestricted access to tenant candidate data (see restricted actions below); Super Admin access to tenant data is deliberately limited to protect tenant privacy and is logged when it occurs.

Every role is implemented as a **claim/permission-based** role (see §9), not just a string flag, so that fine-grained permissions can be added later (e.g., a "Recruiter — Read Only" variant) without a schema migration.

## 2.1 Super Admin

**Description:** Operates the SaaS platform itself. Not tied to any single company/tenant. Typically 1–5 people on the vendor's own operations team.

**Responsibilities**
- Onboard new companies (tenants) onto the platform.
- Manage platform-wide configuration (email provider settings, feature flags, subscription/billing tier per company, global system announcements).
- Monitor platform health (via health checks, logs, metrics — see §14).
- Suspend or reinstate a company's access (e.g., non-payment, ToS violation).
- Manage global lookup/reference data shared across tenants (e.g., a global skill taxonomy, if used).
- Escalation point for support issues that require cross-tenant investigation (e.g., a bug affecting multiple companies).

**Permissions**
- `platform.companies.manage` (create, suspend, reinstate, delete company)
- `platform.settings.manage`
- `platform.users.impersonate` (time-boxed, fully audited — see §12)
- `platform.audit.read.all`
- `platform.reports.read.all` (aggregate/anonymized platform metrics only, not raw candidate PII by default)

**Allowed Actions**
- Create/suspend/delete a Company tenant.
- Create the first Company Admin user for a new tenant.
- View platform-wide audit logs.
- View aggregate reporting across all tenants (counts, usage, not candidate-level PII).
- Configure global system settings (SMTP provider, notification templates defaults, feature flags).
- Trigger a time-boxed, audited "support impersonation" session into a specific company account when explicitly requested by that company (see §12.9).

**Restricted Actions**
- Cannot silently browse a specific company's candidates, jobs, or interview feedback without either (a) using an explicit, audited impersonation flow with a logged reason, or (b) the company granting explicit support access. This is a business rule (§12), not just a UI restriction — it is enforced at the authorization-handler level.
- Cannot post interview feedback, move candidates through a pipeline, or act as a Recruiter/Interviewer inside a tenant under normal operation.
- Cannot permanently hard-delete audit logs (audit logs are append-only; see §5 and §14).

## 2.2 Company Admin

**Description:** The owner/administrator of a single company (tenant) account. Usually the person who signed the company up, or a designated HR/Ops lead.

**Responsibilities**
- Manage the company's profile, departments, and branding (e.g., careers page name, logo reference).
- Invite and manage all internal users within the company (Recruiters, HR, Interviewers, other Company Admins).
- Assign roles and department scoping to internal users.
- Own company-wide settings (notification templates, hiring pipeline stage configuration, working hours for interview scheduling).
- View all jobs, applications, and reports across the entire company (not limited to one department).
- Deactivate departing employees' accounts.

**Permissions**
- `company.settings.manage`
- `company.users.manage` (invite, edit role, deactivate — scoped to own company)
- `company.departments.manage`
- `company.jobs.manage.all` (create/edit/publish/close any job in the company)
- `company.applications.read.all`
- `company.pipeline.manage.all`
- `company.reports.read.all` (company-scoped)
- `company.auditlogs.read` (company-scoped)

**Allowed Actions**
- Create/edit/delete Departments.
- Invite users and assign them roles (Recruiter, HR, Interviewer, Company Admin).
- Create, edit, publish, unpublish, and close any Job Post in the company.
- View and manage any Application/Candidate pipeline across all departments.
- Configure the company's hiring pipeline stages (e.g., add a custom stage like "Technical Assessment").
- View company-level reports and dashboards.
- View company-scoped audit logs.
- Override a pipeline stage transition in exceptional cases (with mandatory reason, logged).

**Restricted Actions**
- Cannot access another company's data under any circumstance (enforced by tenant isolation at the data-access layer, not just UI).
- Cannot modify platform-wide settings (SMTP config, global feature flags) — Super Admin only.
- Cannot alter or delete audit log entries (read-only, even for Company Admin).
- Cannot impersonate a Candidate.

## 2.3 Recruiter

**Description:** The primary day-to-day operator of the hiring pipeline. Owns a portfolio of job posts and candidates.

**Responsibilities**
- Create and manage job posts (subject to Company Admin/HR approval workflow if configured — see §4).
- Review incoming applications and screen candidates.
- Move candidates through pipeline stages.
- Schedule interviews and coordinate interviewers.
- Communicate with candidates (via system notifications/email templates).
- Prepare and send offers (with HR/Company Admin approval depending on company settings).
- Maintain CV/document records for candidates they own.

**Permissions**
- `jobs.manage.own` (create/edit own job posts; edit others only if explicitly shared)
- `applications.read.assigned`
- `applications.manage.assigned` (move stage, reject, shortlist)
- `interviews.schedule`
- `interviews.feedback.read.assigned`
- `candidates.cv.read.assigned`
- `notifications.send.candidate`

**Allowed Actions**
- Create a new Job Post as a draft; submit for publish (or publish directly, depending on company workflow settings).
- View applications for jobs they own or are assigned to.
- Move a candidate forward/backward in the pipeline, with reason capture on rejection.
- Schedule an interview, select interviewers, propose time slots.
- View interview feedback submitted by interviewers for their candidates.
- Send templated or custom notifications/emails to candidates in their pipeline.
- Download/view candidate CVs and attached documents for their own candidates.
- Generate an offer letter draft (approval may be required — see §4.11).

**Restricted Actions**
- Cannot view applications/candidates for jobs they do not own or are not assigned to (unless Company Admin grants company-wide visibility).
- Cannot edit company-wide settings, departments, or manage other users' roles.
- Cannot access another company's data.
- Cannot permanently delete a candidate or application record (soft delete only, and only with sufficient permission — typically Company Admin approval for hard-delete requests such as GDPR erasure, see §12).
- Cannot alter interview feedback submitted by an Interviewer (feedback is immutable once submitted, only appendable via a separate "addendum" if the business decides to allow it — default: immutable).

## 2.4 HR

**Description:** Oversees hiring policy, compliance, and the offer/hiring finalization step. Often works alongside or above Recruiters but is not necessarily doing day-to-day sourcing.

**Responsibilities**
- Approve or reject job post publication (if the company enables an approval workflow).
- Approve offers before they are sent to candidates.
- Ensure compliance in the hiring process (e.g., mandatory structured feedback exists before an offer).
- View company-wide hiring reports and pipeline health.
- Manage final "Hired" transition and (at a conceptual level) prepare handoff data for onboarding (actual onboarding module is out of scope, see §18).

**Permissions**
- `jobs.approve`
- `applications.read.all` (company-scoped)
- `offers.approve`
- `offers.manage`
- `pipeline.manage.all` (company-scoped, similar breadth to Company Admin but without user/department management rights)
- `reports.read.all` (company-scoped)

**Allowed Actions**
- Approve/reject a job post pending publication.
- View any application/candidate pipeline within the company.
- Approve or reject an offer prepared by a Recruiter before it is sent.
- Mark a candidate as formally Hired (final pipeline stage), which is a protected transition (see §12).
- View company-wide reports.

**Restricted Actions**
- Cannot manage users, departments, or company settings (Company Admin only).
- Cannot directly edit interview feedback content.
- Cannot access another company's data.

## 2.5 Interviewer

**Description:** An internal employee (could be a hiring manager, a technical lead, or any staff member) invited to interview a specific candidate for a specific job. Has the narrowest scope of all internal roles.

**Responsibilities**
- Conduct assigned interviews.
- Submit structured feedback (rating + notes + recommendation) after each interview.
- View only the candidates/interviews they are assigned to.

**Permissions**
- `interviews.read.assigned`
- `interviews.feedback.create.assigned`
- `candidates.cv.read.assigned` (read-only, limited to candidates they are interviewing)

**Allowed Actions**
- View their own upcoming/past interview schedule.
- View the CV and basic profile of a candidate they are scheduled to interview (visibility begins once the interview is scheduled, not before).
- Submit a structured feedback form after the interview (rating scale + free text + recommendation: Advance / Reject / Hold).
- View feedback they themselves submitted.

**Restricted Actions**
- Cannot view feedback submitted by other interviewers for the same candidate until they have submitted their own (configurable "blind feedback" business rule, default: **on** — see §12) — prevents anchoring bias.
- Cannot move a candidate through the pipeline (no stage-transition permission).
- Cannot view candidates/applications they are not assigned to.
- Cannot edit or delete their feedback once submitted (immutable audit trail); may submit an addendum if company policy allows.
- Cannot access company settings, reports, or other modules.

## 2.6 Candidate

**Description:** An external user — the job applicant. Has an account scoped only to their own data.

**Responsibilities**
- Maintain their own profile and CV/documents.
- Apply to job posts.
- Track their own application status.
- Respond to interview scheduling requests (select from proposed slots, if self-scheduling is enabled).
- Accept or decline offers.

**Permissions**
- `profile.manage.own`
- `applications.create.own`
- `applications.read.own`
- `applications.withdraw.own`
- `cv.manage.own`
- `interviews.read.own`
- `interviews.selfschedule.own` (if enabled by company)
- `offers.respond.own`

**Allowed Actions**
- Register and manage their own account/profile.
- Upload/update/delete their own CV and supporting documents (before an application is submitted; after submission, historical versions are retained — see §4.5).
- Browse public job posts and apply.
- View the status of their own applications (current pipeline stage, using a simplified/candidate-facing status label — not necessarily the raw internal stage name).
- Withdraw an application at any time before a final decision.
- Select an interview time slot from options proposed by the recruiter (if self-scheduling is enabled for that interview).
- Accept or decline an offer, with optional comments.
- View their own notification history.

**Restricted Actions**
- Cannot view any other candidate's data.
- Cannot view internal-only fields (interview feedback content, internal notes, recruiter comments, salary negotiation notes) — these are always filtered out of any candidate-facing API response at the DTO/mapping layer (see §7), not just hidden in the UI.
- Cannot see which company employees are interviewing them beyond first name/role, if the company chooses to reveal interviewer identity at all (configurable, default: show first name + title only).
- Cannot re-apply to the same job post if an active (non-withdrawn, non-rejected) application already exists (see §12).
- Cannot access any company management, reporting, or configuration features.

## 2.7 Role Summary Matrix

| Capability | Super Admin | Company Admin | Recruiter | HR | Interviewer | Candidate |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| Manage platform/tenants | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Manage company settings/users | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage departments | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Create/publish jobs | ❌ | ✅ | ✅ (own) | Approve only | ❌ | ❌ |
| View all company applications | ❌ (audited exception) | ✅ | Own/assigned only | ✅ | Assigned only | Own only |
| Move pipeline stage | ❌ | ✅ | ✅ (own) | ✅ | ❌ | ❌ (withdraw only) |
| Schedule interviews | ❌ | ✅ | ✅ | ✅ | ❌ | Self-schedule slot only |
| Submit interview feedback | ❌ | ❌ | ❌ | ❌ | ✅ | ❌ |
| Approve/send offers | ❌ | ✅ | Draft only | ✅ approve | ❌ | Respond only |
| View company reports | ❌ (aggregate only) | ✅ | Own scope | ✅ | ❌ | ❌ |
| View audit logs | ✅ (platform) | ✅ (company) | ❌ | ❌ | ❌ | ❌ |

---

# 3. Functional Requirements — Module Breakdown

The system is decomposed into the following modules. Each module maps to a bounded context in the domain model (§6) and a CQRS slice (§7).

1. **Authentication & Identity** — registration, login, JWT/refresh token issuance, password reset, email verification, logout/token revocation.
2. **Users** — internal user management (invite, role assignment, deactivation) scoped per company; candidate account management.
3. **Companies (Tenants)** — company registration/onboarding, profile, settings, subscription/status.
4. **Departments** — organizational units within a company used to scope jobs and reporting.
5. **Job Posts** — creation, approval workflow, publishing, editing, closing, and public listing of job openings.
6. **Applications** — a candidate's application to a specific job post; the central entity linking Candidate ↔ Job ↔ Pipeline.
7. **CV Management** — candidate document upload/storage/versioning (CVs, cover letters, portfolios).
8. **Hiring Pipeline** — configurable stage definitions and the state machine governing an Application's progress.
9. **Interview Scheduling** — proposing, confirming, rescheduling, and cancelling interviews; conflict/overlap prevention.
10. **Interview Feedback** — structured feedback capture tied to a specific interview + interviewer + candidate.
11. **Offers** — offer creation, approval, sending, candidate response, and revocation.
12. **Notifications (In-App)** — persisted, per-user notification feed with read/unread state.
13. **Email** — outbound transactional email via templated messages, triggered by domain events.
14. **Dashboard** — role-specific aggregated views (recruiter's active pipeline, company admin's funnel health, interviewer's upcoming interviews).
15. **Reports** — exportable/queryable analytics (time-to-hire, source effectiveness, stage conversion, interviewer load).
16. **Settings** — company-level configuration (pipeline stage customization, working hours, approval workflow toggles, notification templates).
17. **Audit Logs** — append-only record of significant actions across all modules, queryable by company/user/entity.

## 3.1 Module Interaction Overview

- **Authentication** issues identity for all other modules; every module's authorization checks depend on it.
- **Companies** is the tenant root; **Departments**, **Users**, **Job Posts**, and **Settings** all belong to exactly one Company.
- **Job Posts** belong to a Department and a Company; **Applications** belong to a Job Post and a Candidate.
- **Hiring Pipeline** governs the valid state transitions an **Application** can take; **Interview Scheduling** and **Offers** are triggered from specific pipeline stages.
- **Interview Feedback** is always tied to exactly one Interview, which is tied to exactly one Application.
- **Notifications** and **Email** are both triggered by **Domain Events** raised from any module (see §6.4) — they do not contain business logic themselves, they are subscribers.
- **Dashboard** and **Reports** are read-only aggregations across other modules' data (Query side of CQRS only — see §7).
- **Audit Logs** subscribes to a cross-cutting logging concern triggered by command handlers across all modules (see §14).

---
# 4. Features

Each feature below follows a fixed structure: **Purpose, Business Rules, Validation Rules, Edge Cases, Error Scenarios, Success Flow.**

---

## 4.1 User Registration & Login (Internal Users)

**Purpose:** Allow internal users (invited by a Company Admin) to activate their account and authenticate into the system.

**Business Rules**
- Internal users cannot self-register; they are created via an **invitation** (see §4.2). Registration here means "accepting an invitation and setting a password."
- Each internal user belongs to exactly one Company (no cross-company internal accounts in v1 — see §18 for multi-company staffing as a future improvement).
- Email address is the unique login identifier, unique **within** the platform (not just within a company), because a person's email is globally unique in practice and this simplifies login-without-tenant-selection.
- An invitation link expires after 72 hours.

**Validation Rules**
- Password minimum 8 characters, must include at least one uppercase letter, one lowercase letter, one digit, and one special character.
- Email must be a syntactically valid email and must match the invited email exactly (case-insensitive).
- Invitation token must be valid, unexpired, and unused.

**Edge Cases**
- User tries to accept an already-used invitation → reject with a clear "already activated, please log in" message.
- User tries to accept an expired invitation → reject; Company Admin/HR can resend a new invitation.
- User changes their mind and never activates → account remains in `PendingActivation` state indefinitely; Company Admin can revoke or resend.

**Error Scenarios**
- Invalid/expired token → `400 Bad Request` with `INVITATION_EXPIRED` or `INVITATION_INVALID` error code.
- Password fails complexity rules → `400 Bad Request` with field-level validation errors.
- Email already active on platform → `409 Conflict`.

**Success Flow**
1. Company Admin invites a user (email + role + department).
2. System creates a `User` record in `PendingActivation` status and sends an invitation email with a secure, single-use token link.
3. User clicks link, sets password, and account transitions to `Active`.
4. User is redirected to login and can authenticate normally thereafter.

---

## 4.2 User Invitation (by Company Admin)

**Purpose:** Allow a Company Admin to add new internal team members with a specific role and department scope.

**Business Rules**
- Only Company Admin can invite users with role `Company Admin`, `Recruiter`, `HR`, or `Interviewer`.
- A company's active user count may be limited by subscription tier (checked against `Company.Subscription.MaxUsers`); exceeding it blocks new invitations until upgrade or deactivation of another user.
- Re-inviting a `PendingActivation` user resends the email and issues a new token (invalidating the old one).
- Re-inviting a `Deactivated` user reactivates them into `PendingActivation` if their email/role need to change, or directly to `Active` if reactivating without changes (configurable, default: goes through `PendingActivation` again for security).

**Validation Rules**
- Email required, valid format, not already an active user in **this** company.
- Role required, must be one of the four internal roles (Super Admin is never invitable via this flow).
- Department optional for Company Admin/HR (company-wide scope), required for Recruiter/Interviewer if the company has department-scoping enabled in Settings.

**Edge Cases**
- Inviting an email that is already `Active` in a **different** company → allowed (a person can work at multiple companies with separate accounts in future, but in v1 this is blocked — see §12 business rule on globally unique email — so this returns a conflict; documented as a known v1 limitation, see §18).
- Inviting the same email twice while still `PendingActivation` → treated as a resend, not a duplicate error.
- Subscription seat limit reached → block with a clear upgrade-prompt error, not a silent failure.

**Error Scenarios**
- `403 Forbidden` if caller is not Company Admin.
- `409 Conflict` if email is active elsewhere on the platform.
- `422 Unprocessable Entity` if seat limit exceeded.

**Success Flow**
1. Company Admin submits invite (email, role, department).
2. System validates seat limit and email uniqueness.
3. User record created (`PendingActivation`), invitation email queued.
4. Audit log entry created: `UserInvited`.

---

## 4.3 Candidate Registration & Profile

**Purpose:** Allow external candidates to self-register and manage their own profile so they can apply to jobs.

**Business Rules**
- Candidates self-register directly (no invitation needed).
- A candidate account is **platform-wide**, not company-scoped — one candidate account can apply to jobs at many different companies.
- Email verification is required before a candidate can submit an application (they may browse jobs unverified, but `Apply` is blocked — see §4.4).

**Validation Rules**
- Email valid and unique across all candidate accounts.
- Password same complexity rule as §4.1.
- Full name required; phone number optional but validated for format if provided.

**Edge Cases**
- Candidate registers with an email that already exists as an internal user account → allowed; these are separate identity spaces (a person could be a Recruiter at Company A and a Candidate applying to Company B) but the system must clearly disambiguate login context (see §9.6).
- Candidate attempts to update email to one already in use → `409 Conflict`.

**Error Scenarios**
- `400 Bad Request` for validation failures.
- `409 Conflict` for duplicate email.

**Success Flow**
1. Candidate submits registration form.
2. Account created in `Unverified` status; verification email sent.
3. Candidate can browse/search jobs immediately.
4. Candidate verifies email (see §9.5) → status becomes `Verified`; `Apply` action unlocked.

---

## 4.4 Job Application Submission

**Purpose:** Allow a verified candidate to apply to a published job post.

**Business Rules**
- A candidate cannot apply to the same job post more than once while an active application exists. "Active" means any status except `Rejected` or `Withdrawn`. If a prior application was `Rejected` or `Withdrawn`, a company-level setting determines whether re-application is allowed and, if so, after what cooldown period (default: allowed after 90 days).
- Job post must be in `Published` status and within its application window (`OpenDate` ≤ now ≤ `CloseDate`, if a close date is set).
- An application requires at least one CV/document attached (either newly uploaded or selected from the candidate's existing document library).
- Upon submission, the application is created at the pipeline's initial stage (typically `Applied` / `New`).

**Validation Rules**
- Candidate must be `Verified`.
- Job must exist, be `Published`, and not `Closed`/`Archived`.
- At least one CV reference required.
- Optional cover letter text limited to a max length (e.g., 5,000 characters).

**Edge Cases**
- Job closes (reaches `CloseDate`) between candidate viewing it and submitting → reject at submission time with `JOB_CLOSED`, not just at listing time.
- Candidate has a `Rejected` application from 45 days ago on a job with a 90-day cooldown → reapplication blocked until day 90, with a clear message stating the eligible date.
- Candidate applies via a direct link to a job that was unpublished/archived → `404` or `410 Gone` semantics, not a generic 500 error.

**Error Scenarios**
- `403 Forbidden` — candidate not verified.
- `409 Conflict` — duplicate active application, or cooldown not yet elapsed.
- `422 Unprocessable Entity` — no CV attached.
- `404 Not Found` — job doesn't exist or isn't visible.

**Success Flow**
1. Candidate selects a published job and clicks Apply.
2. Candidate attaches CV (existing or new upload) and optional cover letter.
3. System validates eligibility (verified, not duplicate, job open).
4. `Application` record created at initial pipeline stage; `ApplicationSubmitted` domain event raised.
5. Candidate receives confirmation notification/email; assigned Recruiter (or job's default recruiter queue) receives a "New Application" notification.
6. Audit log entry created.

---

## 4.5 CV / Document Management

**Purpose:** Allow candidates to upload, store, and manage CVs and supporting documents, and allow authorized internal users to view them.

**Business Rules**
- Supported file types: PDF, DOC, DOCX. Max file size: 10 MB (configurable platform-wide).
- A candidate may maintain multiple CVs (e.g., "General CV," "Frontend-focused CV") and choose which one to attach per application.
- Once a CV is attached to a submitted Application, it becomes an **immutable snapshot** referenced by that application — if the candidate later edits/replaces their "live" CV document, previously submitted applications keep the version that was attached at submission time. This is critical for interview feedback integrity and audit purposes.
- Internal users can only view CVs for candidates within applications they have permission to view (see §2 role permissions).

**Validation Rules**
- File type must be in the allowed list; reject others with a clear message.
- File size must not exceed the configured max.
- Virus/malware scan (conceptual requirement — integration point, not implemented in v1 code but the field/status must exist: `ScanStatus: Pending|Clean|Infected`). Files with `Infected` status are quarantined and not retrievable.

**Edge Cases**
- Candidate uploads a corrupted/unreadable file → validation should at minimum check file signature/magic bytes, not just extension.
- Candidate deletes a CV that is referenced by a past submitted application → deletion is blocked from removing the **snapshot reference**; only the "live" document entry is removed from their active library. The immutable snapshot persists (soft-delete / copy-on-submit model — see §5).
- Storage provider failure during upload → application submission must not proceed with a broken CV reference; use a transactional upload-then-confirm pattern.

**Error Scenarios**
- `400 Bad Request` — invalid file type or corrupt file.
- `413 Payload Too Large` — file exceeds size limit.
- `422 Unprocessable Entity` — scan status `Infected`.

**Success Flow**
1. Candidate uploads a document via the CV management screen.
2. System validates type/size, stores file in blob storage, creates a `Document` record with `ScanStatus: Pending`.
3. Async scan completes → status updated to `Clean` (or `Infected`, triggering quarantine + candidate notification to re-upload).
4. Document is now selectable when submitting applications.

---

## 4.6 Job Post Creation & Publishing Workflow

**Purpose:** Allow Recruiters/Company Admins to create job posts and, if the company enables it, route them through HR approval before they go live.

**Business Rules**
- A job post has a lifecycle: `Draft → PendingApproval → Published → Closed → Archived` (with `Rejected` as a branch from `PendingApproval` back to `Draft`).
- If `Company.Settings.RequireJobApproval = true`, a job cannot move from `Draft` to `Published` without an HR (or Company Admin) approval action; it must pass through `PendingApproval`.
- If `RequireJobApproval = false`, a Recruiter (or Company Admin) can publish directly from `Draft`.
- A job **cannot be published** without all required fields populated (see Validation Rules) — this is enforced regardless of the approval workflow setting.
- A published job can be edited; certain "material" field changes (title, salary range, employment type) may optionally trigger a "re-approval" flag if the company enables strict governance (default: off — edits to a published job do not require re-approval, but are captured in audit log with a diff).
- Closing a job (`Closed`) stops accepting new applications but keeps existing applications' pipelines active. `Archived` is a terminal state used for historical/reporting purposes and hides the job from all active lists.

**Validation Rules — required to publish**
- Title (required, 5–150 chars)
- Description (required, min 50 chars)
- Department (required, must belong to the same company)
- Employment type (required: Full-Time, Part-Time, Contract, Internship)
- Location (required: On-site/Remote/Hybrid + city/region if applicable)
- At least one required qualification/requirement line item
- Number of open positions (required, integer ≥ 1)
- Application close date, if set, must be in the future at time of publish

**Edge Cases**
- Recruiter tries to publish a job missing required fields → blocked with a field-by-field error list, not a generic failure.
- HR rejects a `PendingApproval` job → must require a rejection reason; job returns to `Draft` with the reason visible to the Recruiter, and a notification is sent.
- A job with zero open positions remaining (all positions filled via `Hired` outcomes) — system should prompt to auto-close, but not force it automatically without confirmation (business decision: auto-suggest, don't auto-act, to avoid surprising the recruiter).
- Concurrent edit: two admins editing the same Draft job — last-write-wins is acceptable in v1, but the system must use optimistic concurrency (a `RowVersion`/`ConcurrencyToken`) to detect and reject a stale save with a clear conflict error rather than silently overwriting.

**Error Scenarios**
- `422 Unprocessable Entity` — missing required fields on publish attempt.
- `403 Forbidden` — non-owner Recruiter attempting to edit a job they don't own and isn't company-wide-visible.
- `409 Conflict` — optimistic concurrency violation on edit.
- `400 Bad Request` — invalid state transition (e.g., trying to publish an already-`Archived` job).

**Success Flow (approval-required company)**
1. Recruiter creates a job in `Draft`, fills required fields, saves progress freely (drafts can be incomplete).
2. Recruiter submits for approval → validation runs; if valid, status becomes `PendingApproval`; HR notified.
3. HR reviews, approves → status becomes `Published`; job appears on public listing; candidates notified if they had a saved search/alert matching it (future improvement, see §18, but the event should still be raised now for extensibility).
4. Recruiter can later close or archive the job.

---

## 4.7 Application Review & Screening

**Purpose:** Allow Recruiters/HR to review incoming applications and make initial screening decisions.

**Business Rules**
- Applications are listed per job, filterable/sortable by stage, application date, and (if implemented) a manual "flag/star" marker.
- A Recruiter can view the candidate's CV, cover letter, and profile, and move the application forward (`Screening`) or reject it directly from `Applied`.
- Rejecting an application **requires a reason** selected from a configurable reason list (e.g., "Underqualified," "Position filled," "Not a culture fit") plus optional free text — mandatory reason capture supports both candidate communication and internal reporting.
- Bulk actions (e.g., reject 10 applications at once) are supported but each individual application still gets its own audit log entry and state transition event — bulk is a UI/API convenience, not a different domain operation.

**Validation Rules**
- Reason code required on rejection.
- Cannot move an application to a stage that isn't a valid forward transition from its current stage (see §11 pipeline state machine) except for the explicit "Reject" transition, which is valid from almost any non-terminal stage.

**Edge Cases**
- Recruiter attempts to reject an application that is already in `Offer` or `Hired` stage → blocked; must first be moved back or handled via an explicit "Retract Offer" flow (see §4.11), not a generic rejection, because different notification/legal handling applies.
- Two recruiters simultaneously acting on the same application (rare but possible if company-wide visibility is on) → optimistic concurrency check on the Application's state.

**Error Scenarios**
- `400 Bad Request` — invalid stage transition.
- `422 Unprocessable Entity` — missing rejection reason.
- `409 Conflict` — concurrent modification.

**Success Flow**
1. Recruiter opens the applications list for a job.
2. Reviews candidate CV/profile.
3. Chooses "Advance" (moves to `Screening` or next configured stage) or "Reject" (reason required).
4. System updates Application state, raises a domain event (`ApplicationAdvanced` or `ApplicationRejected`), triggers candidate notification per company's notification settings, writes audit log.

---

## 4.8 Hiring Pipeline Stage Management

**Purpose:** Allow Company Admins to define and customize the ordered set of stages an application moves through.

**Business Rules**
- Every company gets a **default pipeline** on creation: `Applied → Screening → Interview → Offer → Hired`, with `Rejected` and `Withdrawn` as terminal side-states reachable from any non-terminal stage.
- Company Admin can add custom intermediate stages (e.g., "Technical Assessment," "Final Round") and reorder them, but cannot remove or reorder the two fixed anchor stages: the initial stage (always first) and `Hired`/`Rejected`/`Withdrawn` (always terminal).
- Changing the pipeline definition does **not** retroactively move existing in-flight applications to different stages; it only affects the stage list available for **future** transitions. Existing applications keep referencing their current stage by ID even if the stage is later removed from the active template (soft-delete the stage definition, don't hard-delete, to preserve historical integrity — see §5).
- Each stage has a `Type` classification (`Initial`, `Standard`, `Interview`, `Offer`, `Terminal-Positive` [Hired], `Terminal-Negative` [Rejected/Withdrawn]) which drives system behavior (e.g., only `Interview`-type stages allow scheduling an interview from that context; only `Offer`-type stages allow creating an offer).

**Validation Rules**
- Stage name required, unique within the company's pipeline template.
- Cannot delete a stage that has at least one Application currently sitting in it (must reassign or wait until empty).
- Must always have exactly one `Initial` stage and at least one `Terminal-Positive` stage.

**Edge Cases**
- Company Admin tries to delete the only `Interview`-type stage while jobs have interviews scheduled referencing applications in that stage → block deletion with a clear dependency error.
- Reordering stages while applications are actively transitioning (race condition) → stage order changes should be versioned; in-flight transition validations use the stage graph version active at the time the transition command is processed.

**Error Scenarios**
- `409 Conflict` — attempting to delete a stage in use.
- `422 Unprocessable Entity` — invalid graph (no initial stage, no terminal-positive stage).

**Success Flow**
1. Company Admin opens Pipeline Settings.
2. Adds/reorders/renames stages, assigns each a `Type`.
3. Saves — validation runs on the full graph, not just the changed node.
4. New template version becomes active for all future transitions.

---

## 4.9 Interview Scheduling

**Purpose:** Allow Recruiters/HR to schedule interviews with one or more interviewers for a candidate, without double-booking anyone.

**Business Rules**
- An interview is linked to exactly one Application (and therefore one Candidate and one Job).
- An interview has one or more assigned Interviewers, a scheduled start/end time, a mode (`OnSite`, `Video`, `Phone`), and an optional location/link.
- **Interviews cannot overlap** for the same Interviewer. The system must check the interviewer's existing confirmed interviews (across all jobs/candidates/companies they're involved in, since an interviewer belongs to one company but could theoretically be double-booked across concurrent hiring processes within that company) before confirming a new one.
- Two modes of scheduling are supported: (a) **Direct scheduling** — Recruiter picks an exact time and interviewers confirm/decline; (b) **Self-scheduling** — Recruiter proposes multiple candidate-facing time slots (derived from interviewer availability) and the Candidate picks one, which then locks the slot.
- Rescheduling an interview creates a new time proposal and marks the old one `Rescheduled` (not deleted, to preserve history); cancelling marks it `Cancelled` with a required reason.
- Only Recruiter, HR, or Company Admin can schedule/reschedule/cancel interviews.

**Validation Rules**
- Start time must be before end time; both must be in the future at creation time (cannot schedule an interview in the past).
- Interviewer(s) must belong to the same company as the job/application.
- Overlap check: no confirmed interview for any selected interviewer may overlap `[StartTime, EndTime)` with the proposed slot.
- Application must currently be in a pipeline stage of `Type = Interview` (or the immediately preceding stage, depending on company workflow config) to allow scheduling.

**Edge Cases**
- Two recruiters simultaneously try to book the same interviewer for overlapping slots → the second request must fail the overlap check at commit time (server-side, transactional check — not just a client-side pre-check), returning a clear conflict error with the conflicting interview's time range (but not necessarily the other candidate's identity, to preserve privacy — show "Interviewer is unavailable 2:00–3:00 PM" without naming the other candidate).
- Candidate fails to select a self-scheduling slot before all proposed slots expire → system flags the interview scheduling request as `Expired`, notifies the Recruiter to re-propose.
- Interviewer assigned to an interview is deactivated (leaves the company) before the interview occurs → system flags the interview as `NeedsAttention` and notifies the Recruiter to reassign.
- Time zone handling: all times stored in UTC; display conversion happens at the API/presentation boundary using the user's profile timezone (candidate) or company default timezone (internal users), never stored pre-converted.

**Error Scenarios**
- `409 Conflict` — interviewer overlap detected.
- `400 Bad Request` — start/end time invalid, or start time in the past.
- `422 Unprocessable Entity` — application not in an interview-eligible stage.
- `410 Gone` — attempting to select an expired self-scheduling proposal.

**Success Flow (direct scheduling)**
1. Recruiter selects an Application in an Interview-type stage.
2. Chooses interviewer(s), proposes a date/time, mode, and location/link.
3. System validates overlap for each interviewer.
4. Interview created in `Scheduled` status; notifications (email + in-app) sent to interviewer(s) and candidate.
5. Audit log entry created.

---

## 4.10 Interview Feedback

**Purpose:** Capture structured, comparable feedback from each interviewer after an interview.

**Business Rules**
- Feedback can only be submitted by an interviewer who was assigned to that specific interview.
- Feedback is submitted **once per interviewer per interview** and is **immutable** after submission (supports audit integrity and prevents post-hoc rationalization). Addenda are allowed as a separate linked record if the company enables it, but the original is never overwritten.
- Feedback structure: an overall recommendation (`StrongAdvance`, `Advance`, `Hold`, `Reject`), a numeric rating (e.g., 1–5) per configurable competency dimension (e.g., "Technical Skills," "Communication," "Culture Fit" — company-configurable list), and free-text notes.
- **Blind feedback** (default ON): an interviewer cannot see other interviewers' feedback for the same candidate until they have submitted their own. This prevents anchoring/groupthink. Company Admin can disable this per-company if they prefer collaborative real-time feedback.
- Feedback becomes visible to the assigned Recruiter/HR/Company Admin immediately upon submission, subject to normal role visibility rules — it is never visible to the Candidate.
- An interview cannot be considered "complete" for pipeline-advancement purposes until all assigned interviewers have submitted feedback (configurable: company may allow advancing with partial feedback plus a warning).

**Validation Rules**
- Recommendation is required (enum, not free text).
- At least one competency rating required if the company has configured competency dimensions; free text notes optional but recommended (soft warning, not hard block, if empty).
- Cannot submit feedback for an interview that hasn't occurred yet unless the company explicitly enables "early feedback" (default: blocked until `EndTime` has passed).

**Edge Cases**
- Interviewer tries to submit feedback twice → second attempt blocked with `409 Conflict`, directed to the addendum flow if enabled.
- Interview is cancelled after feedback was already submitted (e.g., a technical issue caused a redo) → original feedback remains but is flagged `OrphanedByCancellation`; a new interview + new feedback cycle is required.
- Interviewer leaves the company before submitting feedback → system flags as `FeedbackOverdue` after a configurable grace period and notifies the Recruiter/HR to follow up or manually note it as unavailable.

**Error Scenarios**
- `403 Forbidden` — not an assigned interviewer for this interview.
- `409 Conflict` — feedback already submitted.
- `422 Unprocessable Entity` — missing required recommendation field, or interview hasn't concluded yet.

**Success Flow**
1. Interview's scheduled end time passes.
2. System (or interviewer manually) surfaces a "Submit Feedback" prompt to each assigned interviewer.
3. Interviewer fills recommendation, ratings, notes; submits.
4. Feedback record created (immutable); `InterviewFeedbackSubmitted` domain event raised.
5. If all interviewers have now submitted, `AllFeedbackComplete` event raised → Recruiter/HR notified that the application is ready for a pipeline decision.

---

## 4.11 Offer Management

**Purpose:** Allow Recruiters to prepare an offer, route it through HR approval, send it to the candidate, and capture the candidate's response.

**Business Rules**
- An offer can only be created for an Application currently in an `Offer`-type pipeline stage.
- Offer lifecycle: `Draft → PendingApproval → Approved → Sent → (Accepted | Declined | Expired | Retracted)`.
- If `Company.Settings.RequireOfferApproval = true` (default: true — offers involve compensation, higher governance by default than jobs), HR or Company Admin must approve before it can be sent.
- An offer includes: proposed title, compensation (base + optional bonus/equity notes as free text or structured fields), start date, expiration date for the candidate's response, and any additional terms as free text/attached document.
- Sending an offer transitions the Application's pipeline stage to reflect "Offer Sent" if not already there, and notifies the candidate.
- Candidate response (`Accept`/`Decline`) is captured with a timestamp; accepting an offer **automatically transitions the Application to `Hired`** (terminal-positive stage) and closes the job's open position count by one (does not auto-close the whole job unless positions reach zero and company setting allows auto-close — see §4.6 edge case).
- A `Sent` offer that passes its expiration date without a response automatically transitions to `Expired`, notifying the Recruiter.
- A Recruiter/HR can `Retract` a `Sent` (not-yet-responded) offer with a mandatory reason before the candidate responds; retracting after acceptance is not a simple retraction — it must go through a separate, explicitly logged "Rescind Accepted Offer" action, which is intentionally friction-heavy given its legal/reputational sensitivity (still supported, but flagged in audit logs with higher severity).

**Validation Rules**
- Compensation fields must be non-negative numbers if structured fields are used.
- Expiration date must be in the future at creation.
- Cannot send an offer that hasn't been approved when approval is required.
- Cannot create a second active (`Draft` through `Sent`) offer for the same Application while one already exists — must retract/decline/expire the existing one first.

**Edge Cases**
- HR rejects an offer at `PendingApproval` → requires reason, returns to `Draft` for Recruiter revision, notification sent.
- Candidate accepts an offer for a job that has since been closed for other reasons → acceptance still succeeds (candidate commitment honored); system flags for manual review if the position count logic seems inconsistent, but never silently blocks a candidate's acceptance.
- Concurrent: Recruiter retracts an offer at the exact moment the candidate accepts it → use optimistic concurrency; whichever transaction commits first wins, the other gets a conflict error explaining the offer's current state.

**Error Scenarios**
- `422 Unprocessable Entity` — missing required offer fields, or application not in Offer-type stage.
- `403 Forbidden` — insufficient role for approval/send action.
- `409 Conflict` — duplicate active offer, or concurrency conflict.
- `410 Gone` — attempting to respond to an already-expired offer.

**Success Flow**
1. Recruiter drafts an offer on an Application in an Offer-type stage.
2. Submits for approval (if required) → HR approves.
3. Recruiter sends the approved offer → candidate notified (email + in-app), Application stage updated to reflect "Offer Sent."
4. Candidate accepts → Application moves to `Hired`; job position count decremented; audit log entry created; internal team notified.
   — or Candidate declines → Application moves to `Rejected`-equivalent terminal state specific to decline (`DeclinedByCandidate`, distinct from recruiter-initiated `Rejected` for reporting accuracy); reason optionally captured.

---

## 4.12 In-App Notifications

**Purpose:** Give every user a persistent, role-appropriate feed of relevant system events.

**Business Rules**
- Notifications are generated as **subscribers to domain events** (see §6.4), never written directly by command handlers — this keeps notification logic decoupled and extensible.
- Each notification has a `Type` (enum, e.g., `ApplicationReceived`, `InterviewScheduled`, `FeedbackRequested`, `OfferReceived`), a target `UserId` (or `CandidateId`), a `Read/Unread` state, a payload (structured, for building the message + deep link), and a `CreatedAt` timestamp.
- Notifications are company-scoped for internal users (never leak cross-tenant) and candidate-scoped for candidates.
- Users can mark individual notifications as read, or mark-all-as-read.
- Notifications older than a configurable retention period (default 180 days) may be purged by a background job, but this must not affect the underlying audit log (notifications are a UX convenience, not the system of record).

**Validation Rules**
- A notification always requires a valid target user/candidate that exists and belongs to the correct tenant context.

**Edge Cases**
- Target user is deactivated before the notification is read → notification remains queryable in history (for any Company Admin audit purposes) but not delivered via any active real-time channel.
- High-frequency events (e.g., bulk rejection of 50 applications by a Recruiter, notifying the same Company Admin 50 times) → system should support notification batching/summarization (e.g., "50 applications were updated") as a v1.1 consideration; v1 minimum requirement is that it does not crash or rate-limit-fail under bulk operations — batching itself can be listed under Future Improvements if not done initially, but the requirement to not fail is v1 scope.

**Error Scenarios**
- Failure to persist a notification must not roll back the originating business transaction (notifications are best-effort/eventually-consistent relative to the core action) — but failures must be logged for monitoring (§14).

**Success Flow**
1. A domain event is raised (e.g., `InterviewScheduled`).
2. Notification handler(s) subscribed to that event construct and persist one or more `Notification` records for relevant users.
3. User's notification feed/badge count updates on next fetch (polling or real-time channel, implementation detail left to frontend — API just needs to expose unread count + list + mark-read endpoints).

---

## 4.13 Email Notifications

**Purpose:** Send transactional emails for key lifecycle events to both candidates and internal users.

**Business Rules**
- Email sending is template-based; each `EmailTemplate` is keyed by `TemplateType` + `CompanyId` (companies can customize wording within a fixed set of merge fields, falling back to a platform default template if the company hasn't customized one).
- Triggered by the same domain events as in-app notifications, via a separate subscriber — the two channels are independent so one failing doesn't block the other.
- Emails must never leak internal-only information to candidate recipients (e.g., an "Application Rejected" email to a candidate must not include internal interviewer feedback notes).
- All outbound email sends are logged (recipient, template type, timestamp, delivery status) for troubleshooting and compliance, separate from the general audit log (or as a specialized audit entry type).

**Validation Rules**
- Recipient email must be present and valid before attempting a send.
- Template must resolve (company-specific or platform default) — a missing template is a configuration error that must be logged and alerted, not silently dropped.

**Edge Cases**
- Email provider is temporarily down → sends must be queued with retry (exponential backoff, max attempts configurable, e.g., 5 attempts over 24 hours) rather than immediately failing and losing the notification.
- Candidate's email bounces (hard bounce) → mark candidate's email as `Undeliverable`, surface a warning to the Recruiter on that candidate's profile, do not keep silently retrying indefinitely.

**Error Scenarios**
- Provider API failure → queued for retry; after max attempts, marked `Failed` and logged for manual follow-up/alerting.
- Missing template → `500`-class internal error logged with high severity (this is a system misconfiguration, not a user error).

**Success Flow**
1. Domain event raised (e.g., `OfferSent`).
2. Email subscriber resolves the appropriate template (company override or default), merges in event payload data.
3. Email queued to the outbound email provider.
4. Delivery status recorded (Sent/Delivered/Bounced/Failed, as reported by provider webhooks where available).

---

## 4.14 Dashboards

**Purpose:** Give each role a relevant, at-a-glance operational view.

**Business Rules**
- Dashboard content is role-specific (query-side only, no write operations):
  - **Recruiter:** their active jobs, applications needing action (new applications, feedback complete & awaiting decision, offers pending response), upcoming interviews they scheduled.
  - **HR:** company-wide pipeline health, jobs pending approval, offers pending approval, overdue feedback.
  - **Company Admin:** all of HR's view plus user/team activity summary and subscription/usage status.
  - **Interviewer:** their upcoming interviews, any feedback they still owe.
  - **Candidate:** their own application statuses, upcoming interview invitations/confirmations, any pending offers.
- Dashboards are read-optimized (see §7 — likely backed by dedicated query projections/views rather than ad hoc joins at request time, for performance at scale).

**Validation Rules**
- N/A (read-only); standard authorization scoping applies (a user only ever sees data within their permission scope, enforced the same way as any other query).

**Edge Cases**
- New company with zero data → dashboard must render meaningful empty states, not errors.
- Very large companies (thousands of applications) → dashboard queries must be paginated/limited (e.g., "top 10 items needing attention," with a link to the full filtered list) rather than attempting to load everything.

**Error Scenarios**
- Standard `403`/`401` for unauthorized access attempts to scopes outside the user's role.

**Success Flow**
1. User logs in / navigates to dashboard.
2. Frontend calls role-appropriate dashboard query endpoint(s).
3. API returns aggregated, pre-scoped data.

---

## 4.15 Reports

**Purpose:** Provide analytical, exportable views of hiring performance.

**Business Rules**
- Minimum v1 report set:
  - **Time-to-Hire** — average/median days from `Applied` to `Hired`, filterable by job/department/date range.
  - **Pipeline Conversion Funnel** — count and percentage of applications at each stage, and drop-off rate between stages.
  - **Source Effectiveness** — if application source is tracked (e.g., job board vs. referral vs. direct), conversion rate by source.
  - **Interviewer Load & Turnaround** — number of interviews conducted, average feedback turnaround time per interviewer.
  - **Rejection Reason Breakdown** — count of rejections grouped by reason code.
- All reports are company-scoped (except Super Admin's platform-aggregate views, which must be anonymized/aggregated — no candidate-level PII in cross-tenant aggregate reports).
- Reports support a date range filter and, where relevant, department/job filters.
- Reports must be exportable (CSV minimum for v1; PDF as a future improvement, see §18).

**Validation Rules**
- Date range required for time-bound reports (default to "last 90 days" if unspecified).
- End date must not be before start date.

**Edge Cases**
- Report requested for a date range with zero data → return an empty, well-formed result, not an error.
- Very large export requests → must be handled asynchronously (background job + downloadable link) beyond a configurable row-count threshold, to avoid request timeouts.

**Error Scenarios**
- `400 Bad Request` — invalid date range.
- `202 Accepted` (not an error) — large export queued asynchronously, with a status-check endpoint.

**Success Flow**
1. User (with reporting permission) selects a report type and filters.
2. API computes/queries the aggregated data (from optimized read models where applicable).
3. Result returned inline (small) or as an async export job (large).

---

## 4.16 Settings

**Purpose:** Allow Company Admins to configure company-specific behavior without code changes.

**Business Rules — configurable settings include**
- `RequireJobApproval` (bool)
- `RequireOfferApproval` (bool, default true)
- `ReapplicationCooldownDays` (int, default 90)
- `BlindFeedbackEnabled` (bool, default true)
- `SelfSchedulingEnabled` (bool, default true)
- `InterviewerIdentityVisibleToCandidate` (enum: None, FirstNameOnly, FirstNameAndTitle; default FirstNameOnly)
- `WorkingHours` (per-day start/end, used to bound self-scheduling proposed slots)
- `PipelineTemplate` (see §4.8)
- `CompetencyDimensions` (list of named rating categories used in interview feedback)
- `NotificationTemplateOverrides` (per template type)
- `DepartmentScopingEnabled` (bool — whether Recruiters/Interviewers must be scoped to a department)

**Validation Rules**
- Numeric settings (cooldown days, etc.) must be non-negative.
- Working hours: start time must be before end time per day.
- Cannot disable a setting in a way that would invalidate existing in-flight data (e.g., disabling `BlindFeedbackEnabled` mid-process is allowed since it's a forward-looking visibility rule, not a data integrity issue; but deleting a `CompetencyDimension` that has existing feedback tied to it must be a soft-delete/deprecate, not a hard delete).

**Edge Cases**
- Company Admin changes `PipelineTemplate` stage types while interviews/offers are in flight referencing the old types → see §4.8 edge cases; system must not silently break in-flight entities' displayed state.

**Error Scenarios**
- `422 Unprocessable Entity` for invalid setting values (e.g., negative cooldown days).

**Success Flow**
1. Company Admin opens Settings.
2. Adjusts one or more values.
3. System validates and persists as a versioned settings snapshot (so audit logs can show "Setting X changed from A to B by User Y at time T").

---

## 4.17 Audit Logs

**Purpose:** Provide an append-only, queryable record of significant actions for compliance and internal accountability.

**Business Rules**
- Every command handler that mutates state (see §7) writes at least one audit log entry as part of the same logical operation (not best-effort/fire-and-forget like notifications — audit logging failure should fail the operation in a production-grade implementation, or at minimum be treated as a critical alerting condition if eventual consistency is chosen for performance reasons; the specification default is **synchronous, same-transaction audit writes** for the highest-sensitivity actions — role changes, deletions, offer actions, impersonation — and **asynchronous but guaranteed-delivery** (outbox pattern) for high-volume lower-sensitivity actions like notification-read-state).
- Audit entries are immutable and append-only; no update or delete API exists for them, at any role level, including Super Admin.
- Each entry captures: `ActorUserId` (or `System` for automated actions), `ActorRole`, `CompanyId` (tenant context, nullable for platform-level actions), `Action` (enum/string, e.g., `ApplicationRejected`), `EntityType`, `EntityId`, `Timestamp`, `Details` (structured diff/payload where relevant), and `IpAddress`/`UserAgent` metadata where available.
- Super Admin impersonation sessions (§2.1) generate a dedicated high-severity audit entry at session start and end, including the stated reason.

**Validation Rules**
- N/A for reads; writes are system-internal, not user-submitted forms.

**Edge Cases**
- Extremely high-volume actions (bulk operations) → each individual entity change still gets its own audit row (never collapse into a summary row that loses per-entity traceability), but the write path must be efficient (batched inserts) to avoid performance degradation.

**Error Scenarios**
- If a synchronous audit write fails for a high-sensitivity action, the triggering command must fail/roll back rather than complete silently un-audited.

**Success Flow**
1. Any state-mutating command executes successfully.
2. As part of the same handler (via a cross-cutting behavior — see §15 MediatR pipeline behaviors), an audit entry is constructed and persisted.
3. Company Admins/Super Admins can later query audit logs filtered by date range, actor, entity type, or action type.

---
# 5. Database Design

## 5.1 Conventions

- **Primary keys:** `Guid` (`uniqueidentifier`) for all entities, generated application-side (not database identity), to support offline-friendly ID generation and safe merge scenarios.
- **Soft delete:** Every entity that represents user-facing business data has an `IsDeleted` (bit, default 0) and `DeletedAtUtc` (datetime2, nullable) column. Hard deletes are reserved for narrow, explicitly-approved compliance erasure flows (see §12) and are never exposed as a generic API capability.
- **Audit fields (every entity):** `CreatedAtUtc` (datetime2, not null), `CreatedBy` (Guid, nullable — nullable for system-generated rows), `ModifiedAtUtc` (datetime2, nullable), `ModifiedBy` (Guid, nullable).
- **Concurrency:** Every mutable entity has a `RowVersion` (`rowversion`/`timestamp`) column for optimistic concurrency control.
- **Tenant isolation:** Every entity that is not platform-global carries a `CompanyId` (Guid, not null, FK to `Companies`) column, and **all** EF Core queries apply a global query filter on `CompanyId` matching the current authenticated tenant context, in addition to `IsDeleted = 0`. This is defense-in-depth on top of authorization-layer checks.
- **Naming:** PascalCase table and column names (consistent with typical .NET/SQL Server conventions used elsewhere in this spec).

## 5.2 Entity List

1. Company
2. Subscription
3. Department
4. User (internal)
5. Candidate
6. RoleAssignment (or embedded role on User, see note below)
7. JobPost
8. JobRequirement
9. Application
10. Document (CV/attachments)
11. ApplicationDocument (join/snapshot table)
12. PipelineStageTemplate
13. PipelineStage
14. ApplicationStageHistory
15. Interview
16. InterviewParticipant
17. InterviewFeedback
18. InterviewFeedbackRating
19. CompetencyDimension
20. Offer
21. Notification
22. EmailTemplate
23. EmailLog
24. AuditLog
25. RejectionReason (lookup)
26. CompanySettings

---

### 5.2.1 Company

Represents a tenant.

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| Name | nvarchar(200) | Not null |
| LegalName | nvarchar(250) | Nullable |
| Slug | nvarchar(100) | Unique, used in public careers URL |
| LogoUrl | nvarchar(500) | Nullable |
| Status | tinyint/enum | `Active`, `Suspended`, `Deleted` |
| SubscriptionId | Guid (FK → Subscription) | Nullable until subscription assigned |
| PrimaryContactEmail | nvarchar(320) | Not null |
| TimeZone | nvarchar(100) | IANA tz id, default company timezone for scheduling display |
| CreatedAtUtc / CreatedBy / ModifiedAtUtc / ModifiedBy | — | Standard audit fields |
| IsDeleted / DeletedAtUtc | — | Soft delete |
| RowVersion | rowversion | Concurrency |

**Relationships:** 1 Company → many Departments, Users, JobPosts, Candidates-do-NOT-belong-here (Candidates are platform-global, see 5.2.5). 1 Company → 1 CompanySettings. 1 Company → 0..1 Subscription.
**Indexes:** Unique index on `Slug`. Index on `Status`.
**Constraints:** `Slug` unique, not null.

---

### 5.2.2 Subscription

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| Tier | tinyint/enum | `Free`, `Starter`, `Professional`, `Enterprise` |
| MaxUsers | int | Seat limit |
| MaxActiveJobs | int | Nullable = unlimited |
| StartedAtUtc | datetime2 | |
| RenewsAtUtc | datetime2 | Nullable |
| Status | tinyint/enum | `Active`, `PastDue`, `Cancelled` |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Subscription → 1 Company.
**Indexes:** Index on `CompanyId`, `Status`.

---

### 5.2.3 Department

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| Name | nvarchar(150) | Not null |
| Description | nvarchar(1000) | Nullable |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Company → many Departments. 1 Department → many JobPosts, many Users (scoped).
**Indexes:** Composite unique index on (`CompanyId`, `Name`) where `IsDeleted = 0`.

---

### 5.2.4 User (Internal)

Represents Super Admin, Company Admin, Recruiter, HR, Interviewer.

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Nullable **only** for Super Admin (platform-level user) |
| DepartmentId | Guid (FK) | Nullable |
| Email | nvarchar(320) | Unique across platform |
| PasswordHash | nvarchar(500) | Not null (after activation) |
| FirstName | nvarchar(100) | |
| LastName | nvarchar(100) | |
| Role | tinyint/enum | `SuperAdmin`, `CompanyAdmin`, `Recruiter`, `HR`, `Interviewer` |
| Status | tinyint/enum | `PendingActivation`, `Active`, `Deactivated` |
| InvitationToken | nvarchar(200) | Nullable, hashed |
| InvitationExpiresAtUtc | datetime2 | Nullable |
| EmailVerified | bit | |
| LastLoginAtUtc | datetime2 | Nullable |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Company → many Users. 1 Department → many Users. 1 User → many JobPosts (as owner), many Interviews (as participant), many InterviewFeedback (as author), many AuditLog entries (as actor).
**Indexes:** Unique index on `Email`. Composite index on (`CompanyId`, `Role`, `Status`).
**Constraint:** `CompanyId` NOT NULL unless `Role = SuperAdmin` (enforced at application layer + DB check constraint).

> **Note on RoleAssignment:** v1 uses a single `Role` enum column on `User` for simplicity, since each internal user has exactly one role per company and belongs to only one company. This is documented as a deliberate simplification; a future multi-role-per-user or multi-company-per-user model (§18) would introduce a proper `RoleAssignment` join entity. Permission checks (§9) should still be implemented against a **permission-claim abstraction**, not raw role string comparisons, so this future migration doesn't require rewriting every authorization check.

---

### 5.2.5 Candidate

Platform-global, not tenant-scoped.

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| Email | nvarchar(320) | Unique across all candidates |
| PasswordHash | nvarchar(500) | |
| FirstName | nvarchar(100) | |
| LastName | nvarchar(100) | |
| Phone | nvarchar(30) | Nullable |
| TimeZone | nvarchar(100) | Nullable, IANA id |
| Status | tinyint/enum | `Unverified`, `Verified`, `Deactivated` |
| EmailVerificationToken | nvarchar(200) | Nullable, hashed |
| EmailVerificationExpiresAtUtc | datetime2 | Nullable |
| Audit + soft delete + RowVersion | — | Standard (`CreatedBy`/`ModifiedBy` nullable/self-referential as needed) |

**Relationships:** 1 Candidate → many Documents, many Applications.
**Indexes:** Unique index on `Email`.
**Note:** No `CompanyId` — global query filter for tenant isolation does **not** apply to this table; access control is enforced purely at the application/authorization layer (a candidate's own JWT scopes their queries to their own `CandidateId`).

---

### 5.2.6 JobPost

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| DepartmentId | Guid (FK) | |
| OwnerUserId | Guid (FK → User) | Recruiter/Company Admin who owns it |
| Title | nvarchar(150) | |
| Description | nvarchar(max) | |
| EmploymentType | tinyint/enum | `FullTime`, `PartTime`, `Contract`, `Internship` |
| LocationType | tinyint/enum | `OnSite`, `Remote`, `Hybrid` |
| LocationText | nvarchar(200) | Nullable (city/region) |
| OpenPositions | int | ≥ 1 |
| PositionsFilled | int | Default 0 |
| SalaryMin | decimal(12,2) | Nullable |
| SalaryMax | decimal(12,2) | Nullable |
| Status | tinyint/enum | `Draft`, `PendingApproval`, `Published`, `Rejected`, `Closed`, `Archived` |
| OpenDateUtc | datetime2 | Nullable |
| CloseDateUtc | datetime2 | Nullable |
| RejectionReason | nvarchar(1000) | Nullable, set when Status = Rejected |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Company → many JobPosts. 1 Department → many JobPosts. 1 User (Owner) → many JobPosts. 1 JobPost → many JobRequirements, many Applications.
**Indexes:** Composite index on (`CompanyId`, `Status`). Index on `DepartmentId`. Full-text or standard index on `Title` for search.
**Constraints:** Check constraint `OpenPositions >= 1`. Check constraint `SalaryMax >= SalaryMin` when both present.

---

### 5.2.7 JobRequirement

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| JobPostId | Guid (FK) | |
| Text | nvarchar(500) | |
| IsRequired | bit | Distinguishes "required" vs "nice-to-have" |
| SortOrder | int | |
| Audit + soft delete | — | Standard (RowVersion optional here — low-conflict child entity) |

**Relationships:** 1 JobPost → many JobRequirements.
**Indexes:** Index on `JobPostId`.

---

### 5.2.8 Application

The central entity of the system.

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Denormalized from JobPost for query/filter performance and tenant-filter simplicity |
| JobPostId | Guid (FK) | |
| CandidateId | Guid (FK) | |
| CurrentStageId | Guid (FK → PipelineStage) | |
| Status | tinyint/enum | `Active`, `Rejected`, `Withdrawn`, `Hired`, `DeclinedByCandidate` |
| Source | tinyint/enum | `Direct`, `JobBoard`, `Referral`, `Other` (nullable/unknown allowed) |
| CoverLetterText | nvarchar(max) | Nullable |
| SubmittedAtUtc | datetime2 | |
| DecidedAtUtc | datetime2 | Nullable — set when reaching a terminal state |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 JobPost → many Applications. 1 Candidate → many Applications. 1 Application → many ApplicationDocuments, many ApplicationStageHistory rows, many Interviews, 0..1 active Offer (historically many Offers over time, only one "active" at a time per business rule).
**Indexes:** Composite index on (`CandidateId`, `JobPostId`) — used to enforce/check the "no duplicate active application" rule efficiently. Composite index on (`CompanyId`, `CurrentStageId`). Index on `Status`.
**Constraints:** Application-level uniqueness of "one active application per candidate per job" is enforced at the application layer (since "active" is a computed condition, not a simple unique constraint) — implemented via a filtered unique index where feasible: `CREATE UNIQUE INDEX ... WHERE Status = 'Active'` (SQL Server supports filtered indexes), which gives DB-level protection for the common case while the command handler additionally checks cooldown-period re-application logic.

---

### 5.2.9 Document

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CandidateId | Guid (FK) | |
| FileName | nvarchar(300) | |
| ContentType | nvarchar(100) | |
| SizeBytes | bigint | |
| StorageUrl | nvarchar(1000) | Blob storage reference, not the file itself |
| Label | nvarchar(150) | e.g., "General CV," "Frontend CV" |
| DocumentType | tinyint/enum | `Cv`, `CoverLetter`, `Portfolio`, `Other` |
| ScanStatus | tinyint/enum | `Pending`, `Clean`, `Infected` |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Candidate → many Documents. Referenced (snapshotted) by ApplicationDocument.
**Indexes:** Index on `CandidateId`.

---

### 5.2.10 ApplicationDocument (snapshot join)

Captures the **immutable** document reference at the time of application submission (see §4.5 business rule).

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| ApplicationId | Guid (FK) | |
| DocumentId | Guid (FK) | Points to the Document row as it existed at submission; Document rows referenced here are never hard-deleted even if the candidate deletes them from their "live" library (soft-delete only) |
| SnapshotFileName | nvarchar(300) | Denormalized copy, belt-and-suspenders against any future Document mutation |
| SnapshotStorageUrl | nvarchar(1000) | Denormalized copy of the storage pointer at time of attach |
| Audit fields (Created only) | — | Immutable, no Modified fields needed |

**Relationships:** Many-to-many resolver between Application and Document, but semantically a one-way immutable snapshot.
**Indexes:** Index on `ApplicationId`.

---

### 5.2.11 PipelineStageTemplate

Represents a versioned pipeline definition per company (see §4.8).

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| Version | int | Incremented on each structural change |
| IsActive | bit | Only one active version per company |
| Audit + soft delete | — | Standard |

**Relationships:** 1 Company → many PipelineStageTemplate versions (historical). 1 PipelineStageTemplate → many PipelineStage.
**Indexes:** Composite unique index on (`CompanyId`, `Version`).

---

### 5.2.12 PipelineStage

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| PipelineStageTemplateId | Guid (FK) | |
| Name | nvarchar(100) | |
| Type | tinyint/enum | `Initial`, `Standard`, `Interview`, `Offer`, `TerminalPositive`, `TerminalNegative` |
| SortOrder | int | |
| Audit + soft delete | — | Standard |

**Relationships:** 1 PipelineStageTemplate → many PipelineStage. 1 PipelineStage → many Applications (via `CurrentStageId`), many ApplicationStageHistory rows.
**Indexes:** Index on `PipelineStageTemplateId`.
**Constraints:** Exactly one `Initial` stage and at least one `TerminalPositive` stage enforced at application layer on template save (see §4.8).

---

### 5.2.13 ApplicationStageHistory

Append-only log of every stage transition for an Application (distinct from the general AuditLog — this is domain-specific and used directly for pipeline/funnel reporting).

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| ApplicationId | Guid (FK) | |
| FromStageId | Guid (FK) | Nullable (null for the very first "entered initial stage" row) |
| ToStageId | Guid (FK) | |
| ChangedByUserId | Guid (FK → User) | Nullable if system-driven (e.g., auto-expire) |
| Reason | nvarchar(1000) | Nullable (required at command-validation level for rejections, see §4.7) |
| ChangedAtUtc | datetime2 | |

**Relationships:** 1 Application → many ApplicationStageHistory.
**Indexes:** Index on `ApplicationId`, `ChangedAtUtc` — used heavily for time-in-stage reporting.
**Constraint:** Append-only; no update/delete at the application layer.

---

### 5.2.14 Interview

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| ApplicationId | Guid (FK) | |
| ScheduledByUserId | Guid (FK → User) | |
| StartTimeUtc | datetime2 | |
| EndTimeUtc | datetime2 | |
| Mode | tinyint/enum | `OnSite`, `Video`, `Phone` |
| LocationOrLink | nvarchar(500) | Nullable |
| Status | tinyint/enum | `ProposedSelfSchedule`, `Scheduled`, `Rescheduled`, `Cancelled`, `Completed`, `Expired`, `NeedsAttention` |
| CancellationReason | nvarchar(1000) | Nullable |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Application → many Interviews (across rounds/history). 1 Interview → many InterviewParticipant, 0..1 InterviewFeedback per participant.
**Indexes:** Composite index on (`ApplicationId`, `Status`). Index on (`StartTimeUtc`, `EndTimeUtc`) to support overlap queries efficiently.

---

### 5.2.15 InterviewParticipant

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| InterviewId | Guid (FK) | |
| InterviewerUserId | Guid (FK → User) | |
| ResponseStatus | tinyint/enum | `Pending`, `Confirmed`, `Declined` |
| Audit + soft delete | — | Standard |

**Relationships:** 1 Interview → many InterviewParticipant. 1 User → many InterviewParticipant (as interviewer).
**Indexes:** Composite index on (`InterviewerUserId`, `InterviewId`) — critical for the overlap-check query (must efficiently find all of a given interviewer's confirmed interviews in a time range).
**Constraint:** Composite unique index on (`InterviewId`, `InterviewerUserId`) — an interviewer isn't added twice to the same interview.

---

### 5.2.16 InterviewFeedback

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| InterviewId | Guid (FK) | |
| InterviewerUserId | Guid (FK → User) | |
| Recommendation | tinyint/enum | `StrongAdvance`, `Advance`, `Hold`, `Reject` |
| Notes | nvarchar(max) | Nullable |
| SubmittedAtUtc | datetime2 | |
| IsAddendum | bit | Default false |
| ParentFeedbackId | Guid (FK, self) | Nullable, set when `IsAddendum = true` |
| Audit fields (Created only) | — | Immutable — no Modified fields; updates are disallowed at the application layer |

**Relationships:** 1 Interview → many InterviewFeedback (one per participant, plus optional addenda). 1 InterviewFeedback → many InterviewFeedbackRating.
**Indexes:** Composite unique index on (`InterviewId`, `InterviewerUserId`) where `IsAddendum = 0` — enforces "one primary feedback per interviewer per interview" at the DB level.

---

### 5.2.17 InterviewFeedbackRating

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| InterviewFeedbackId | Guid (FK) | |
| CompetencyDimensionId | Guid (FK) | |
| Score | tinyint | 1–5 (or company-configured scale, validated at application layer) |

**Relationships:** 1 InterviewFeedback → many InterviewFeedbackRating (one per competency dimension).
**Indexes:** Index on `InterviewFeedbackId`.

---

### 5.2.18 CompetencyDimension

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| Name | nvarchar(100) | e.g., "Technical Skills" |
| SortOrder | int | |
| Audit + soft delete | — | Standard (deprecate via soft-delete, never hard-delete if referenced by existing feedback — see §4.16) |

**Relationships:** 1 Company → many CompetencyDimension. Referenced by InterviewFeedbackRating.
**Indexes:** Composite index on (`CompanyId`, `IsDeleted`).

---

### 5.2.19 Offer

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | |
| ApplicationId | Guid (FK) | |
| CreatedByUserId | Guid (FK → User) | |
| ApprovedByUserId | Guid (FK → User) | Nullable |
| ProposedTitle | nvarchar(150) | |
| BaseCompensation | decimal(14,2) | |
| CompensationCurrency | nvarchar(3) | ISO 4217 code |
| AdditionalTermsText | nvarchar(max) | Nullable |
| ProposedStartDateUtc | datetime2 | |
| ResponseExpiresAtUtc | datetime2 | |
| Status | tinyint/enum | `Draft`, `PendingApproval`, `Approved`, `Rejected` (by approver), `Sent`, `Accepted`, `Declined`, `Expired`, `Retracted` |
| ApprovalRejectionReason | nvarchar(1000) | Nullable |
| RetractionReason | nvarchar(1000) | Nullable |
| CandidateResponseNote | nvarchar(1000) | Nullable |
| RespondedAtUtc | datetime2 | Nullable |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Application → many Offer (historically; business rule restricts to one **active** at a time — see §5.2.8 pattern, enforced via filtered unique index on `ApplicationId` where `Status` is in the active set).
**Indexes:** Index on `ApplicationId`. Filtered unique index on `ApplicationId` where `Status IN ('Draft','PendingApproval','Approved','Sent')`.

---

### 5.2.20 Notification

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Nullable (candidate notifications have no company context) |
| RecipientUserId | Guid (FK → User) | Nullable |
| RecipientCandidateId | Guid (FK → Candidate) | Nullable — exactly one of RecipientUserId/RecipientCandidateId is set |
| Type | tinyint/enum | See §4.12 |
| PayloadJson | nvarchar(max) | Structured data for message building/deep-linking |
| IsRead | bit | Default false |
| ReadAtUtc | datetime2 | Nullable |
| CreatedAtUtc | datetime2 | |

**Relationships:** Many Notification → 1 User or 1 Candidate.
**Indexes:** Composite index on (`RecipientUserId`, `IsRead`, `CreatedAtUtc`). Composite index on (`RecipientCandidateId`, `IsRead`, `CreatedAtUtc`).
**Constraint:** Check constraint — exactly one of `RecipientUserId`/`RecipientCandidateId` is non-null.

---

### 5.2.21 EmailTemplate

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Nullable — null row = platform default template |
| TemplateType | tinyint/enum | Mirrors notification `Type` set |
| Subject | nvarchar(300) | |
| BodyHtml | nvarchar(max) | |
| Audit + soft delete + RowVersion | — | Standard |

**Relationships:** 1 Company → many EmailTemplate (overrides). Platform defaults have `CompanyId = null`.
**Indexes:** Composite unique index on (`CompanyId`, `TemplateType`) — including a special-cased unique index for `CompanyId IS NULL` rows (one default per type).

---

### 5.2.22 EmailLog

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Nullable |
| RecipientEmail | nvarchar(320) | |
| TemplateType | tinyint/enum | |
| Status | tinyint/enum | `Queued`, `Sent`, `Delivered`, `Bounced`, `Failed` |
| AttemptCount | int | |
| LastAttemptAtUtc | datetime2 | Nullable |
| ProviderMessageId | nvarchar(200) | Nullable |
| CreatedAtUtc | datetime2 | |

**Relationships:** Standalone log table, loosely linked via `RecipientEmail`/`TemplateType` rather than hard FK to keep the email subsystem decoupled.
**Indexes:** Index on `Status`. Index on `RecipientEmail`.

---

### 5.2.23 AuditLog

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Nullable — null for platform-level actions |
| ActorUserId | Guid (FK → User) | Nullable — null if `System` |
| ActorRole | tinyint/enum | Nullable |
| Action | nvarchar(150) | e.g., `ApplicationRejected` |
| EntityType | nvarchar(100) | |
| EntityId | Guid | |
| DetailsJson | nvarchar(max) | Nullable — structured diff/context |
| IpAddress | nvarchar(45) | Nullable |
| UserAgent | nvarchar(500) | Nullable |
| Severity | tinyint/enum | `Info`, `Warning`, `Critical` (e.g., impersonation = Critical) |
| CreatedAtUtc | datetime2 | |

**Relationships:** Loosely linked to any entity via `EntityType` + `EntityId` (polymorphic reference, not a hard FK, by design — audit logs must survive even if the referenced entity is later hard-deleted in a rare compliance-erasure scenario).
**Indexes:** Composite index on (`CompanyId`, `CreatedAtUtc`). Composite index on (`EntityType`, `EntityId`). Index on `ActorUserId`.
**Constraint:** Append-only — no update or delete operation exists anywhere in the application for this table.

---

### 5.2.24 RejectionReason (lookup)

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK) | Nullable — null = platform default set, seedable/customizable per company |
| Text | nvarchar(200) | |
| AppliesTo | tinyint/enum | `Application`, `Offer` (a reason list can be scoped to context) |
| Audit + soft delete | — | Standard |

**Relationships:** Referenced by `ApplicationStageHistory.Reason` conceptually (stored as free text there for flexibility, with this table powering a suggested dropdown) — implementers may alternatively FK it directly; either is acceptable, but if stored as free text, a `RejectionReasonId` nullable FK alongside is recommended for reporting cleanliness.
**Indexes:** Composite index on (`CompanyId`, `AppliesTo`).

---

### 5.2.25 CompanySettings

| Field | Type | Notes |
|---|---|---|
| Id | Guid (PK) | |
| CompanyId | Guid (FK, unique) | 1:1 with Company |
| RequireJobApproval | bit | Default false |
| RequireOfferApproval | bit | Default true |
| ReapplicationCooldownDays | int | Default 90 |
| BlindFeedbackEnabled | bit | Default true |
| SelfSchedulingEnabled | bit | Default true |
| InterviewerIdentityVisibility | tinyint/enum | Default `FirstNameOnly` |
| DepartmentScopingEnabled | bit | Default false |
| WorkingHoursJson | nvarchar(max) | Structured per-day start/end |
| Audit + RowVersion | — | Standard (no soft delete — 1:1 lifecycle with Company) |

**Relationships:** 1:1 with Company.
**Indexes:** Unique index on `CompanyId`.

---

## 5.3 Entity Relationship Summary (textual ERD)

```
Company (1) ──< Department (many)
Company (1) ──< User (many)                [nullable for SuperAdmin]
Company (1) ──< JobPost (many)
Company (1) ──1 CompanySettings
Company (1) ──1 Subscription
Company (1) ──< PipelineStageTemplate (many, versioned)
Company (1) ──< CompetencyDimension (many)
Company (1) ──< RejectionReason (many)
Company (1) ──< EmailTemplate (many, overrides)

Department (1) ──< JobPost (many)
Department (1) ──< User (many)              [Recruiter/Interviewer scoping]

JobPost (1) ──< JobRequirement (many)
JobPost (1) ──< Application (many)

PipelineStageTemplate (1) ──< PipelineStage (many)
PipelineStage (1) ──< Application (many, via CurrentStageId)
PipelineStage (1) ──< ApplicationStageHistory (many, via ToStageId/FromStageId)

Candidate (1) ──< Document (many)
Candidate (1) ──< Application (many)

Application (1) ──< ApplicationDocument (many) ──> Document
Application (1) ──< ApplicationStageHistory (many)
Application (1) ──< Interview (many)
Application (1) ──< Offer (many, one active)

Interview (1) ──< InterviewParticipant (many) ──> User
Interview (1) ──< InterviewFeedback (many, one per participant + addenda)
InterviewFeedback (1) ──< InterviewFeedbackRating (many) ──> CompetencyDimension

User / Candidate ──< Notification (many)
AuditLog: polymorphic reference to any entity, loosely coupled
```

---

# 6. Domain Model

This section describes the system using Domain-Driven Design (DDD) tactical patterns: Aggregates, Entities, Value Objects, and Domain Events. This model informs the Clean Architecture `Domain` layer (§15–16).

## 6.1 Aggregates

An **Aggregate** is a cluster of entities/value objects treated as a single unit for consistency purposes, with one **Aggregate Root** as the only externally-referenceable entry point. Command handlers load and persist whole aggregates.

| Aggregate Root | Contains | Consistency Boundary Rationale |
|---|---|---|
| **Company** | Departments (as child entities within the aggregate for structural changes), CompanySettings (1:1) | Company-level structural changes (adding a department) should be transactionally consistent with the company itself; Users, JobPosts, etc. are **separate aggregates** referencing `CompanyId`, not nested inside — keeping the Company aggregate small avoids lock contention and huge load graphs. |
| **User** | — (flat aggregate) | Simple entity-as-aggregate; role/status changes are self-contained. |
| **Candidate** | Documents (child entities) | A candidate's document library changes together with candidate identity concerns like email verification in some flows, but is modeled as a child collection for the "manage my documents" consistency boundary. |
| **JobPost** | JobRequirements (child entities) | Requirements only make sense in the context of their parent job; publishing validation spans the whole aggregate (title + requirements + all required fields) so they must be loaded/validated together. |
| **Application** | ApplicationDocuments (snapshot children), ApplicationStageHistory (append-only children) | The application's current stage, its document snapshot, and its history must move together transactionally — a stage transition **is** the creation of a new ApplicationStageHistory row plus updating `CurrentStageId`, which must be atomic. |
| **PipelineStageTemplate** | PipelineStages (child entities) | The whole graph of stages must be validated as a unit (exactly one Initial, at least one TerminalPositive) — see §4.8 — so it is one aggregate, not independent stage entities. |
| **Interview** | InterviewParticipants (child entities) | Adding/removing participants and checking overlap must be consistent with the interview's own time window within one transaction. |
| **InterviewFeedback** | InterviewFeedbackRatings (child entities) | A feedback submission and its per-competency ratings are created atomically and are immutable together. |
| **Offer** | — (flat aggregate, but state-machine heavy) | Self-contained state machine; references Application by ID only (not nested), since an Offer doesn't own the Application. |
| **Notification** | — (flat aggregate) | Simple, read/unread state only. |
| **AuditLog** | — (flat, append-only) | Not really a DDD aggregate with behavior — more of an event-sourced side-effect record; included here for completeness of the persistence model. |

**Cross-aggregate references** are always by ID (e.g., `Application.JobPostId`, `Interview.ApplicationId`) — never by object navigation property that would blur the transaction boundary. Command handlers that need to touch two aggregates issue two separate save operations (or use domain events / a saga-like coordination) rather than modifying two aggregate roots in a single `SaveChanges` when strict aggregate boundaries are being followed. In pragmatic EF Core implementation, minor exceptions are acceptable (e.g., updating `Application.CurrentStageId` and inserting `ApplicationStageHistory` in the same `SaveChanges` is fine because they're within the **same** aggregate).

## 6.2 Key Entities (beyond aggregate roots already listed)

- **JobRequirement** — entity, child of JobPost aggregate.
- **ApplicationStageHistory** — entity, child of Application aggregate; append-only.
- **InterviewParticipant** — entity, child of Interview aggregate.
- **InterviewFeedbackRating** — entity, child of InterviewFeedback aggregate.
- **CompetencyDimension** — standalone reference entity (its own tiny aggregate), referenced by ID from InterviewFeedbackRating.
- **Department** — child entity of Company aggregate for structural purposes, but also independently referenced by ID from JobPost/User (a common pragmatic DDD compromise: modeled as "aggregate-owned but externally ID-referenceable").

## 6.3 Value Objects

Value Objects are immutable, defined by their attributes (not identity), and should be implemented as C# `record` types or equivalent immutable types embedded within entities.

| Value Object | Fields | Used By |
|---|---|---|
| **EmailAddress** | `Value` (string), with validation in constructor | User, Candidate |
| **Money** | `Amount` (decimal), `Currency` (ISO 4217 code) | Offer.BaseCompensation |
| **DateRange** | `StartUtc`, `EndUtc`, with invariant `Start < End` | Interview scheduling, JobPost open/close window |
| **PersonName** | `FirstName`, `LastName` | User, Candidate |
| **PhoneNumber** | `Value` (string), with format validation | Candidate |
| **Address/LocationText** | `LocationType` (enum), `Text` | JobPost |
| **StageTransition** | `FromStageId`, `ToStageId`, `Reason` (used as a parameter object passed into the Application aggregate's transition method, not necessarily persisted as its own VO beyond becoming an ApplicationStageHistory row) | Application |

Value Objects enforce their own invariants at construction time (e.g., `Money` rejects negative amounts; `DateRange` rejects `End <= Start`), so invalid states are unrepresentable rather than merely validated after the fact.

## 6.4 Domain Events

Domain Events are raised by aggregate roots when a meaningful state change occurs, and are dispatched (via MediatR `INotification` or equivalent) after the originating command's transaction commits successfully. Notifications, emails, and read-model/dashboard projections are all **subscribers**, never the source of business logic.

| Event | Raised By | Typical Subscribers |
|---|---|---|
| `CompanyOnboarded` | Company aggregate | Welcome email, Super Admin dashboard metric |
| `UserInvited` | User aggregate | Invitation email |
| `UserActivated` | User aggregate | Audit log, Company Admin notification |
| `CandidateRegistered` | Candidate aggregate | Verification email |
| `CandidateEmailVerified` | Candidate aggregate | Audit log |
| `JobPostSubmittedForApproval` | JobPost aggregate | HR notification |
| `JobPostPublished` | JobPost aggregate | Public listing refresh, saved-search alert hook (future) |
| `JobPostRejectedByApprover` | JobPost aggregate | Recruiter notification |
| `JobPostClosed` | JobPost aggregate | Internal notification, reporting snapshot |
| `ApplicationSubmitted` | Application aggregate | Candidate confirmation email, Recruiter "new application" notification, audit log |
| `ApplicationAdvancedStage` | Application aggregate | Candidate status-update notification (mapped to candidate-facing label), audit log, dashboard/report projection update |
| `ApplicationRejected` | Application aggregate | Candidate rejection email, audit log, reporting |
| `ApplicationWithdrawn` | Application aggregate | Recruiter notification, audit log |
| `InterviewScheduled` | Interview aggregate | Interviewer + candidate notification/email, audit log |
| `InterviewRescheduled` | Interview aggregate | All participants notified |
| `InterviewCancelled` | Interview aggregate | All participants notified, audit log |
| `InterviewFeedbackSubmitted` | InterviewFeedback aggregate | Recruiter notification (if all feedback now complete), audit log |
| `AllInterviewFeedbackComplete` | Derived/process-manager event (raised by a saga/process manager listening to `InterviewFeedbackSubmitted` and checking completeness) | Recruiter/HR "ready for decision" notification |
| `OfferSubmittedForApproval` | Offer aggregate | HR notification |
| `OfferApproved` | Offer aggregate | Recruiter notification (ready to send) |
| `OfferSent` | Offer aggregate | Candidate notification/email |
| `OfferAccepted` | Offer aggregate | Triggers `ApplicationAdvancedStage` (to Hired) via process manager, internal team notification |
| `OfferDeclined` | Offer aggregate | Recruiter/HR notification, triggers Application terminal-state update |
| `OfferExpired` | Offer aggregate (raised by a background job) | Recruiter notification |
| `OfferRetracted` | Offer aggregate | Candidate notification (careful/tactful template), audit log with elevated severity |
| `SuperAdminImpersonationStarted` | Impersonation process | Critical-severity audit log |
| `SuperAdminImpersonationEnded` | Impersonation process | Critical-severity audit log |

**Event/Command boundary note:** Commands express intent ("RejectApplication"); when the handler successfully applies the change to the aggregate, the aggregate raises the past-tense Domain Event ("ApplicationRejected") which is what everything else in the system reacts to. This decoupling means, for example, that adding a new notification channel (SMS, §18) requires only a new event subscriber, not changes to any command handler.

---
# 7. CQRS Design

The system uses CQRS (Command Query Responsibility Segregation) with MediatR as the in-process mediator. **Commands** mutate state and return minimal data (typically just an ID or a success confirmation DTO); **Queries** never mutate state and return read-optimized DTOs. Each module gets its own `Commands/` and `Queries/` folder (see §16).

Every Command has: a Command object (record), a Handler, a FluentValidation Validator, and (where relevant) a resulting DTO/mapping. Every Query has: a Query object (record), a Handler, and a DTO/mapping. Cross-cutting concerns (validation execution, audit logging, authorization checks, transaction wrapping) are implemented as **MediatR Pipeline Behaviors**, not duplicated per-handler (see §15).

Below, each module lists its Commands, Queries, and notes on Handlers/Validators/DTOs. Full parameter-level detail is intentionally summarized here (field lists already given in §5) — behavior is the focus.

## 7.1 Authentication Module

**Commands**
- `RegisterCandidateCommand` → Handler creates Candidate, raises `CandidateRegistered`. Validator: email format/uniqueness, password complexity.
- `LoginCommand` (email, password, `LoginContext`: Internal|Candidate) → Handler verifies credentials, issues JWT + refresh token. Validator: required fields only (business validation, e.g., "user not found," is a domain/application-layer check, not FluentValidation).
- `RefreshTokenCommand` → Handler validates refresh token, rotates it, issues new JWT.
- `LogoutCommand` → Handler revokes the refresh token.
- `RequestPasswordResetCommand` (email) → Handler issues reset token, raises event for email.
- `ResetPasswordCommand` (token, newPassword) → Handler validates token, updates password hash.
- `VerifyEmailCommand` (token) → Handler validates token, sets `EmailVerified`/`Status = Verified`.
- `AcceptInvitationCommand` (token, password) → Handler activates a `PendingActivation` User.

**Queries**
- `GetCurrentUserQuery` → returns the authenticated principal's profile DTO (internal user or candidate, discriminated).

**DTOs:** `AuthTokenResponseDto` (accessToken, refreshToken, expiresAtUtc), `CurrentUserDto`.

## 7.2 Users Module

**Commands**
- `InviteUserCommand` (email, role, departmentId) → Company Admin only. Validator: seat limit, email uniqueness.
- `ResendInvitationCommand`
- `RevokeInvitationCommand`
- `UpdateUserRoleCommand` (userId, newRole, departmentId)
- `DeactivateUserCommand` (userId)
- `ReactivateUserCommand` (userId)

**Queries**
- `GetUsersByCompanyQuery` (filters: role, department, status; paginated)
- `GetUserByIdQuery`

**DTOs:** `UserSummaryDto`, `UserDetailDto`.

## 7.3 Companies Module

**Commands**
- `OnboardCompanyCommand` (Super Admin only) — creates Company + first Company Admin User + default CompanySettings + default PipelineStageTemplate.
- `UpdateCompanyProfileCommand`
- `SuspendCompanyCommand` (Super Admin only, reason required)
- `ReinstateCompanyCommand` (Super Admin only)

**Queries**
- `GetCompanyProfileQuery`
- `GetCompaniesQuery` (Super Admin only, paginated, filterable by status)

**DTOs:** `CompanyProfileDto`, `CompanySummaryDto`.

## 7.4 Departments Module

**Commands**
- `CreateDepartmentCommand`
- `UpdateDepartmentCommand`
- `DeleteDepartmentCommand` (soft delete; validator blocks if active JobPosts/Users reference it)

**Queries**
- `GetDepartmentsQuery` (company-scoped)
- `GetDepartmentByIdQuery`

**DTOs:** `DepartmentDto`.

## 7.5 Job Posts Module

**Commands**
- `CreateJobPostCommand` (Draft)
- `UpdateJobPostCommand`
- `SubmitJobPostForApprovalCommand`
- `ApproveJobPostCommand` (HR/Company Admin)
- `RejectJobPostCommand` (reason required)
- `PublishJobPostCommand` (direct publish when approval not required)
- `CloseJobPostCommand`
- `ArchiveJobPostCommand`
- `AddJobRequirementCommand` / `RemoveJobRequirementCommand`

**Queries**
- `GetJobPostsQuery` (internal, filterable: status, department, owner; paginated)
- `GetPublicJobPostsQuery` (candidate-facing, only `Published` + within window; filterable: keyword, location, employment type)
- `GetJobPostByIdQuery` (internal detail view)
- `GetPublicJobPostByIdQuery` (candidate-facing detail view — filtered DTO, no internal fields)

**DTOs:** `JobPostDetailDto`, `JobPostSummaryDto`, `PublicJobPostDto` (deliberately distinct shape — excludes `OwnerUserId`, internal notes, etc.).

## 7.6 Applications Module

**Commands**
- `SubmitApplicationCommand` (candidate)
- `AdvanceApplicationStageCommand` (recruiter/HR/company admin; targetStageId)
- `RejectApplicationCommand` (reason required)
- `WithdrawApplicationCommand` (candidate)
- `ReassignApplicationOwnerCommand` (if per-application ownership beyond job ownership is modeled — optional refinement)

**Queries**
- `GetApplicationsForJobQuery` (internal, paginated, filterable by stage)
- `GetApplicationByIdQuery` (internal detail)
- `GetMyApplicationsQuery` (candidate — own applications only)
- `GetApplicationStageHistoryQuery`

**DTOs:** `ApplicationDetailDto` (internal — includes internal notes, feedback summary links), `ApplicationSummaryDto`, `CandidateApplicationStatusDto` (candidate-facing — simplified status label, no internal fields).

## 7.7 CV Management Module

**Commands**
- `UploadDocumentCommand` (candidate)
- `DeleteDocumentCommand` (candidate; blocked from removing snapshot references, see §5.2.10)
- `UpdateDocumentLabelCommand`

**Queries**
- `GetMyDocumentsQuery` (candidate)
- `GetDocumentDownloadUrlQuery` (internal — authorization-checked against application assignment; candidate — own documents only)

**DTOs:** `DocumentDto`.

## 7.8 Hiring Pipeline Module

**Commands**
- `CreatePipelineStageTemplateVersionCommand` (Company Admin — full graph replace-and-version)
- `ActivatePipelineStageTemplateCommand`

**Queries**
- `GetActivePipelineTemplateQuery`
- `GetPipelineTemplateHistoryQuery`

**DTOs:** `PipelineStageTemplateDto`, `PipelineStageDto`.

## 7.9 Interview Scheduling Module

**Commands**
- `ScheduleInterviewCommand` (direct) — validator includes the overlap-check business rule (implemented as a domain service injected into the validator or checked in the handler pre-commit — see note below).
- `ProposeSelfScheduleSlotsCommand`
- `SelectSelfScheduleSlotCommand` (candidate)
- `RescheduleInterviewCommand`
- `CancelInterviewCommand` (reason required)
- `ConfirmParticipantCommand` / `DeclineParticipantCommand` (interviewer response to invite, if that workflow step is enabled)

**Queries**
- `GetInterviewsForApplicationQuery`
- `GetMyUpcomingInterviewsQuery` (interviewer)
- `GetInterviewerAvailabilityQuery` (used to build self-schedule proposed slots)

**DTOs:** `InterviewDto`, `InterviewParticipantDto`, `SelfScheduleSlotDto`.

> **Note on overlap validation placement:** The interviewer-overlap check is a business rule with a database dependency (must query existing interviews), so it cannot live purely in a synchronous FluentValidation validator without a DB call — it is implemented as an **async FluentValidation rule** (`MustAsync`) calling a scoped `IInterviewOverlapChecker` domain/application service, and is **also** re-verified inside the command handler immediately before commit within the same transaction, to close the race-condition window between validation and persistence (defense in depth against concurrent scheduling — see §4.9 edge cases).

## 7.10 Interview Feedback Module

**Commands**
- `SubmitInterviewFeedbackCommand`
- `SubmitFeedbackAddendumCommand` (if company allows addenda)

**Queries**
- `GetFeedbackForInterviewQuery` (respects blind-feedback visibility rule at the query/handler level — see §12)
- `GetMySubmittedFeedbackQuery` (interviewer)
- `GetFeedbackSummaryForApplicationQuery` (recruiter/HR — aggregated view across all interviews for that application)

**DTOs:** `InterviewFeedbackDto`, `FeedbackSummaryDto`.

## 7.11 Offers Module

**Commands**
- `CreateOfferCommand` (Draft)
- `SubmitOfferForApprovalCommand`
- `ApproveOfferCommand`
- `RejectOfferApprovalCommand` (reason required)
- `SendOfferCommand`
- `RespondToOfferCommand` (candidate — accept/decline)
- `RetractOfferCommand` (reason required)
- `RescindAcceptedOfferCommand` (elevated-severity, reason required, restricted to Company Admin/HR)

**Queries**
- `GetOfferForApplicationQuery`
- `GetOffersPendingApprovalQuery` (HR/Company Admin)
- `GetMyOffersQuery` (candidate)

**DTOs:** `OfferDto`, `CandidateOfferDto` (candidate-facing subset).

## 7.12 Notifications Module

**Commands**
- `MarkNotificationReadCommand`
- `MarkAllNotificationsReadCommand`

**Queries**
- `GetMyNotificationsQuery` (paginated, filterable by read/unread)
- `GetUnreadNotificationCountQuery`

**DTOs:** `NotificationDto`.

> Notifications are primarily **created** by domain event handlers (subscribers), not by direct user-initiated commands — so there is deliberately no `CreateNotificationCommand` exposed as a public application command; creation is an internal side-effect.

## 7.13 Email Module

No user-facing Commands/Queries in v1 beyond what Settings exposes for template overrides (see §7.16). Internal: `SendTemplatedEmailCommand` is an **internal-only** command invoked by event handlers, not exposed via API.

**Queries**
- `GetEmailLogsQuery` (Company Admin — troubleshooting/deliverability visibility, company-scoped)

## 7.14 Dashboard Module

**Queries only** (no commands — dashboards are pure read projections):
- `GetRecruiterDashboardQuery`
- `GetHrDashboardQuery`
- `GetCompanyAdminDashboardQuery`
- `GetInterviewerDashboardQuery`
- `GetCandidateDashboardQuery`

**DTOs:** One dedicated DTO per dashboard, each intentionally denormalized/pre-shaped for its UI rather than reusing entity DTOs directly.

## 7.15 Reports Module

**Queries only**
- `GetTimeToHireReportQuery`
- `GetPipelineConversionReportQuery`
- `GetSourceEffectivenessReportQuery`
- `GetInterviewerLoadReportQuery`
- `GetRejectionReasonBreakdownReportQuery`

**Commands**
- `RequestReportExportCommand` (for large async exports — returns a job ID)

**Queries**
- `GetReportExportStatusQuery`

**DTOs:** One DTO per report type; `ReportExportJobDto`.

## 7.16 Settings Module

**Commands**
- `UpdateCompanySettingsCommand`
- `AddCompetencyDimensionCommand` / `DeprecateCompetencyDimensionCommand`
- `AddRejectionReasonCommand` / `DeprecateRejectionReasonCommand`
- `UpsertEmailTemplateOverrideCommand`

**Queries**
- `GetCompanySettingsQuery`
- `GetCompetencyDimensionsQuery`
- `GetRejectionReasonsQuery`
- `GetEmailTemplatesQuery`

**DTOs:** `CompanySettingsDto`, `CompetencyDimensionDto`, `RejectionReasonDto`, `EmailTemplateDto`.

## 7.17 Audit Logs Module

**Queries only**
- `GetAuditLogsQuery` (company-scoped, filterable by actor/entity/action/date range) — Company Admin.
- `GetPlatformAuditLogsQuery` (Super Admin, cross-tenant).

**DTOs:** `AuditLogEntryDto`.

## 7.18 CQRS Cross-Cutting Notes

- **Validators:** FluentValidation validators are auto-discovered and run via a `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior before the handler executes. Validation failures short-circuit the pipeline and are translated to `422 Unprocessable Entity` (field-level) at the API boundary.
- **Authorization:** An `AuthorizationBehavior` pipeline behavior inspects a `[RequirePermission("...")]`-style attribute (or an `IAuthorizationRequirement` implemented per command/query) and checks the current user's claims before the handler runs, short-circuiting to `403 Forbidden` on failure. This keeps authorization out of individual handler bodies.
- **Transactions:** A `TransactionBehavior` wraps Command handlers (not Queries) in a database transaction, ensuring aggregate + audit-log writes commit atomically where required (see §4.17).
- **Mappings:** DTO mapping uses a lightweight mapping approach (either hand-written extension methods or a mapping library) — kept out of handlers themselves where reasonable, typically as a static `ToDto()` extension on the entity or a dedicated `IMapper` call at the end of the query handler.

---

# 8. API Endpoints

All endpoints are prefixed `/api/v1`. Authentication via `Authorization: Bearer {jwt}` header unless marked **Public**. Response bodies are JSON. Standard status codes are used consistently: `200` (success/read), `201` (created), `204` (success/no content), `400` (validation/bad input), `401` (unauthenticated), `403` (unauthorized), `404` (not found), `409` (conflict), `410` (gone/expired), `422` (semantic validation failure), `500` (unexpected server error).

## 8.1 Authentication

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/auth/register/candidate` | Public | `{email, password, firstName, lastName}` | `AuthTokenResponseDto` | 201, 400, 409 |
| POST | `/auth/login` | Public | `{email, password, context: "Internal"\|"Candidate"}` | `AuthTokenResponseDto` | 200, 400, 401 |
| POST | `/auth/refresh` | Public (requires valid refresh token in body/cookie) | `{refreshToken}` | `AuthTokenResponseDto` | 200, 401 |
| POST | `/auth/logout` | Authenticated | `{refreshToken}` | 204 | 204, 401 |
| POST | `/auth/password-reset/request` | Public | `{email}` | 204 (always, to avoid email enumeration) | 204 |
| POST | `/auth/password-reset/confirm` | Public | `{token, newPassword}` | 204 | 204, 400, 410 |
| POST | `/auth/verify-email` | Public | `{token}` | 204 | 204, 400, 410 |
| POST | `/auth/invitations/accept` | Public | `{token, password}` | `AuthTokenResponseDto` | 200, 400, 409, 410 |
| GET | `/auth/me` | Authenticated | — | `CurrentUserDto` | 200, 401 |

## 8.2 Users

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/users/invitations` | CompanyAdmin | `{email, role, departmentId?}` | `UserSummaryDto` | 201, 403, 409, 422 |
| POST | `/users/invitations/{id}/resend` | CompanyAdmin | — | 204 | 204, 403, 404 |
| DELETE | `/users/invitations/{id}` | CompanyAdmin | — | 204 | 204, 403, 404 |
| GET | `/users` | CompanyAdmin, HR (read-only) | Query: `role, departmentId, status, page, pageSize` | `PagedResult<UserSummaryDto>` | 200, 403 |
| GET | `/users/{id}` | CompanyAdmin, HR, Self | — | `UserDetailDto` | 200, 403, 404 |
| PUT | `/users/{id}/role` | CompanyAdmin | `{role, departmentId?}` | `UserDetailDto` | 200, 403, 404, 422 |
| POST | `/users/{id}/deactivate` | CompanyAdmin | — | 204 | 204, 403, 404 |
| POST | `/users/{id}/reactivate` | CompanyAdmin | — | 204 | 204, 403, 404 |

## 8.3 Companies

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/companies` | SuperAdmin | `{name, slug, primaryContactEmail, firstAdminEmail}` | `CompanyProfileDto` | 201, 403, 409, 422 |
| GET | `/companies` | SuperAdmin | Query: `status, page, pageSize` | `PagedResult<CompanySummaryDto>` | 200, 403 |
| GET | `/companies/{id}` | SuperAdmin, CompanyAdmin (self) | — | `CompanyProfileDto` | 200, 403, 404 |
| PUT | `/companies/{id}` | CompanyAdmin (self), SuperAdmin | `{name, logoUrl, timeZone, ...}` | `CompanyProfileDto` | 200, 403, 404, 409 |
| POST | `/companies/{id}/suspend` | SuperAdmin | `{reason}` | 204 | 204, 403, 404, 422 |
| POST | `/companies/{id}/reinstate` | SuperAdmin | — | 204 | 204, 403, 404 |

## 8.4 Departments

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/departments` | CompanyAdmin | `{name, description?}` | `DepartmentDto` | 201, 403, 409, 422 |
| GET | `/departments` | Any internal role (company-scoped) | — | `List<DepartmentDto>` | 200, 403 |
| GET | `/departments/{id}` | Any internal role | — | `DepartmentDto` | 200, 403, 404 |
| PUT | `/departments/{id}` | CompanyAdmin | `{name, description?}` | `DepartmentDto` | 200, 403, 404, 409 |
| DELETE | `/departments/{id}` | CompanyAdmin | — | 204 | 204, 403, 404, 409 |

## 8.5 Job Posts

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/jobs` | Recruiter, CompanyAdmin | `{title, description, departmentId, employmentType, locationType, locationText?, openPositions, salaryMin?, salaryMax?, requirements[]}` | `JobPostDetailDto` | 201, 403, 422 |
| PUT | `/jobs/{id}` | Owner Recruiter, CompanyAdmin | Same shape as create | `JobPostDetailDto` | 200, 403, 404, 409, 422 |
| POST | `/jobs/{id}/submit-for-approval` | Owner Recruiter, CompanyAdmin | — | `JobPostDetailDto` | 200, 403, 404, 422 |
| POST | `/jobs/{id}/approve` | HR, CompanyAdmin | — | `JobPostDetailDto` | 200, 403, 404, 400 |
| POST | `/jobs/{id}/reject` | HR, CompanyAdmin | `{reason}` | `JobPostDetailDto` | 200, 403, 404, 422 |
| POST | `/jobs/{id}/publish` | Owner Recruiter, CompanyAdmin | — | `JobPostDetailDto` | 200, 403, 404, 422 |
| POST | `/jobs/{id}/close` | Owner Recruiter, CompanyAdmin | — | `JobPostDetailDto` | 200, 403, 404, 400 |
| POST | `/jobs/{id}/archive` | CompanyAdmin | — | `JobPostDetailDto` | 200, 403, 404, 400 |
| GET | `/jobs` | Any internal role (company-scoped) | Query: `status, departmentId, ownerId, page, pageSize` | `PagedResult<JobPostSummaryDto>` | 200, 403 |
| GET | `/jobs/{id}` | Any internal role (company-scoped) | — | `JobPostDetailDto` | 200, 403, 404 |
| GET | `/public/jobs` | **Public** | Query: `keyword, location, employmentType, companySlug, page, pageSize` | `PagedResult<PublicJobPostDto>` | 200 |
| GET | `/public/jobs/{id}` | **Public** | — | `PublicJobPostDto` | 200, 404 |

## 8.6 Applications

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/applications` | Candidate | `{jobPostId, documentId, coverLetterText?, source?}` | `ApplicationDetailDto` | 201, 403, 404, 409, 422 |
| GET | `/applications` | Recruiter, HR, CompanyAdmin (company-scoped, filtered by assignment for Recruiter) | Query: `jobPostId, stageId, status, page, pageSize` | `PagedResult<ApplicationSummaryDto>` | 200, 403 |
| GET | `/applications/{id}` | Recruiter (assigned), HR, CompanyAdmin, Owning Candidate (filtered DTO) | — | `ApplicationDetailDto` / `CandidateApplicationStatusDto` | 200, 403, 404 |
| POST | `/applications/{id}/advance` | Recruiter (assigned), HR, CompanyAdmin | `{targetStageId, note?}` | `ApplicationDetailDto` | 200, 403, 404, 400, 409 |
| POST | `/applications/{id}/reject` | Recruiter (assigned), HR, CompanyAdmin | `{reasonId, note?}` | `ApplicationDetailDto` | 200, 403, 404, 400, 422 |
| POST | `/applications/{id}/withdraw` | Owning Candidate | `{reason?}` | `CandidateApplicationStatusDto` | 200, 403, 404, 400 |
| GET | `/applications/{id}/history` | Recruiter (assigned), HR, CompanyAdmin | — | `List<ApplicationStageHistoryDto>` | 200, 403, 404 |
| GET | `/candidates/me/applications` | Candidate | Query: `page, pageSize` | `PagedResult<CandidateApplicationStatusDto>` | 200, 401 |

## 8.7 CV / Documents

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/candidates/me/documents` | Candidate | multipart/form-data (`file`, `label`, `documentType`) | `DocumentDto` | 201, 400, 413, 422 |
| GET | `/candidates/me/documents` | Candidate | — | `List<DocumentDto>` | 200 |
| PUT | `/candidates/me/documents/{id}` | Candidate | `{label}` | `DocumentDto` | 200, 403, 404 |
| DELETE | `/candidates/me/documents/{id}` | Candidate | — | 204 | 204, 403, 404, 409 |
| GET | `/documents/{id}/download-url` | Candidate (own), Recruiter/HR/CompanyAdmin/Interviewer (assigned, via application context) | — | `{url, expiresAtUtc}` | 200, 403, 404 |

## 8.8 Hiring Pipeline

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| GET | `/pipeline/template` | Any internal role (company-scoped) | — | `PipelineStageTemplateDto` | 200, 403 |
| GET | `/pipeline/template/history` | CompanyAdmin | — | `List<PipelineStageTemplateDto>` | 200, 403 |
| POST | `/pipeline/template` | CompanyAdmin | `{stages: [{name, type, sortOrder}]}` (full graph) | `PipelineStageTemplateDto` | 201, 403, 422 |
| POST | `/pipeline/template/{version}/activate` | CompanyAdmin | — | `PipelineStageTemplateDto` | 200, 403, 404, 409 |

## 8.9 Interview Scheduling

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/interviews` | Recruiter, HR, CompanyAdmin | `{applicationId, startTimeUtc, endTimeUtc, mode, locationOrLink?, interviewerUserIds[]}` | `InterviewDto` | 201, 403, 404, 409, 422 |
| POST | `/interviews/self-schedule-proposals` | Recruiter, HR, CompanyAdmin | `{applicationId, interviewerUserIds[], candidateFacingSlots: [{startTimeUtc, endTimeUtc}]}` | `InterviewDto` (status `ProposedSelfSchedule`) | 201, 403, 404, 409, 422 |
| POST | `/interviews/{id}/select-slot` | Owning Candidate | `{slotIndex or startTimeUtc}` | `InterviewDto` | 200, 403, 404, 409, 410 |
| PUT | `/interviews/{id}/reschedule` | Recruiter, HR, CompanyAdmin | `{startTimeUtc, endTimeUtc}` | `InterviewDto` | 200, 403, 404, 409, 422 |
| POST | `/interviews/{id}/cancel` | Recruiter, HR, CompanyAdmin | `{reason}` | `InterviewDto` | 200, 403, 404, 422 |
| GET | `/interviews/{id}` | Recruiter, HR, CompanyAdmin, Assigned Interviewer, Owning Candidate | — | `InterviewDto` | 200, 403, 404 |
| GET | `/applications/{id}/interviews` | Recruiter (assigned), HR, CompanyAdmin | — | `List<InterviewDto>` | 200, 403, 404 |
| GET | `/interviewers/me/interviews` | Interviewer | Query: `from, to, status` | `List<InterviewDto>` | 200, 401 |
| GET | `/interviewers/{id}/availability` | Recruiter, HR, CompanyAdmin | Query: `from, to` | `List<DateRangeDto>` (busy slots) | 200, 403, 404 |

## 8.10 Interview Feedback

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/interviews/{id}/feedback` | Assigned Interviewer | `{recommendation, ratings: [{competencyDimensionId, score}], notes?}` | `InterviewFeedbackDto` | 201, 403, 404, 409, 422 |
| POST | `/interviews/{id}/feedback/{feedbackId}/addendum` | Original Interviewer (author) | `{notes}` | `InterviewFeedbackDto` | 201, 403, 404, 422 |
| GET | `/interviews/{id}/feedback` | Assigned Interviewer (own, or all if already submitted / blind mode off), Recruiter (assigned), HR, CompanyAdmin | — | `List<InterviewFeedbackDto>` | 200, 403, 404 |
| GET | `/applications/{id}/feedback-summary` | Recruiter (assigned), HR, CompanyAdmin | — | `FeedbackSummaryDto` | 200, 403, 404 |

## 8.11 Offers

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/applications/{id}/offers` | Recruiter (assigned), HR, CompanyAdmin | `{proposedTitle, baseCompensation, currency, additionalTermsText?, proposedStartDateUtc, responseExpiresAtUtc}` | `OfferDto` | 201, 403, 404, 409, 422 |
| POST | `/offers/{id}/submit-for-approval` | Recruiter (assigned), CompanyAdmin | — | `OfferDto` | 200, 403, 404, 422 |
| POST | `/offers/{id}/approve` | HR, CompanyAdmin | — | `OfferDto` | 200, 403, 404, 400 |
| POST | `/offers/{id}/reject-approval` | HR, CompanyAdmin | `{reason}` | `OfferDto` | 200, 403, 404, 422 |
| POST | `/offers/{id}/send` | Recruiter (assigned), HR, CompanyAdmin | — | `OfferDto` | 200, 403, 404, 400, 422 |
| POST | `/offers/{id}/retract` | Recruiter (assigned), HR, CompanyAdmin | `{reason}` | `OfferDto` | 200, 403, 404, 400, 422 |
| POST | `/offers/{id}/rescind-accepted` | HR, CompanyAdmin | `{reason}` | `OfferDto` | 200, 403, 404, 400, 422 |
| POST | `/offers/{id}/respond` | Owning Candidate | `{decision: "Accept"\|"Decline", note?}` | `CandidateOfferDto` | 200, 403, 404, 409, 410, 422 |
| GET | `/offers/{id}` | Recruiter (assigned), HR, CompanyAdmin, Owning Candidate | — | `OfferDto` / `CandidateOfferDto` | 200, 403, 404 |
| GET | `/offers/pending-approval` | HR, CompanyAdmin | — | `List<OfferDto>` | 200, 403 |
| GET | `/candidates/me/offers` | Candidate | — | `List<CandidateOfferDto>` | 200, 401 |

## 8.12 Notifications

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| GET | `/notifications` | Authenticated (self only) | Query: `isRead, page, pageSize` | `PagedResult<NotificationDto>` | 200, 401 |
| GET | `/notifications/unread-count` | Authenticated (self only) | — | `{count}` | 200, 401 |
| POST | `/notifications/{id}/read` | Authenticated (self only) | — | 204 | 204, 401, 403, 404 |
| POST | `/notifications/read-all` | Authenticated (self only) | — | 204 | 204, 401 |

## 8.13 Dashboard

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| GET | `/dashboard/recruiter` | Recruiter | — | `RecruiterDashboardDto` | 200, 403 |
| GET | `/dashboard/hr` | HR | — | `HrDashboardDto` | 200, 403 |
| GET | `/dashboard/company-admin` | CompanyAdmin | — | `CompanyAdminDashboardDto` | 200, 403 |
| GET | `/dashboard/interviewer` | Interviewer | — | `InterviewerDashboardDto` | 200, 403 |
| GET | `/dashboard/candidate` | Candidate | — | `CandidateDashboardDto` | 200, 401 |

## 8.14 Reports

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| GET | `/reports/time-to-hire` | HR, CompanyAdmin | Query: `from, to, departmentId?, jobId?` | `TimeToHireReportDto` | 200, 403, 400 |
| GET | `/reports/pipeline-conversion` | HR, CompanyAdmin | Query: `from, to, jobId?` | `PipelineConversionReportDto` | 200, 403, 400 |
| GET | `/reports/source-effectiveness` | HR, CompanyAdmin | Query: `from, to` | `SourceEffectivenessReportDto` | 200, 403, 400 |
| GET | `/reports/interviewer-load` | HR, CompanyAdmin | Query: `from, to` | `InterviewerLoadReportDto` | 200, 403, 400 |
| GET | `/reports/rejection-reasons` | HR, CompanyAdmin | Query: `from, to` | `RejectionReasonReportDto` | 200, 403, 400 |
| POST | `/reports/export` | HR, CompanyAdmin | `{reportType, filters, format: "Csv"}` | `{jobId}` | 202, 403, 400 |
| GET | `/reports/export/{jobId}` | HR, CompanyAdmin | — | `{status, downloadUrl?}` | 200, 403, 404 |

## 8.15 Settings

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| GET | `/settings` | Any internal role (read), CompanyAdmin (implied write access) | — | `CompanySettingsDto` | 200, 403 |
| PUT | `/settings` | CompanyAdmin | Full settings payload | `CompanySettingsDto` | 200, 403, 422 |
| GET | `/settings/competency-dimensions` | Any internal role | — | `List<CompetencyDimensionDto>` | 200, 403 |
| POST | `/settings/competency-dimensions` | CompanyAdmin | `{name, sortOrder}` | `CompetencyDimensionDto` | 201, 403, 422 |
| DELETE | `/settings/competency-dimensions/{id}` | CompanyAdmin | — | 204 (soft delete/deprecate) | 204, 403, 404 |
| GET | `/settings/rejection-reasons` | Any internal role | — | `List<RejectionReasonDto>` | 200, 403 |
| POST | `/settings/rejection-reasons` | CompanyAdmin | `{text, appliesTo}` | `RejectionReasonDto` | 201, 403, 422 |
| DELETE | `/settings/rejection-reasons/{id}` | CompanyAdmin | — | 204 | 204, 403, 404 |
| GET | `/settings/email-templates` | CompanyAdmin | — | `List<EmailTemplateDto>` | 200, 403 |
| PUT | `/settings/email-templates/{templateType}` | CompanyAdmin | `{subject, bodyHtml}` | `EmailTemplateDto` | 200, 403, 422 |

## 8.16 Audit Logs

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| GET | `/audit-logs` | CompanyAdmin | Query: `actorUserId, entityType, action, from, to, page, pageSize` | `PagedResult<AuditLogEntryDto>` | 200, 403 |
| GET | `/platform/audit-logs` | SuperAdmin | Query: `companyId?, actorUserId, entityType, action, from, to, page, pageSize` | `PagedResult<AuditLogEntryDto>` | 200, 403 |

## 8.17 Platform / Super Admin Support

| Method | URL | Authorization | Request | Response | Status Codes |
|---|---|---|---|---|---|
| POST | `/platform/impersonation-sessions` | SuperAdmin | `{companyId, reason}` | `{impersonationToken, expiresAtUtc}` | 201, 403, 404, 422 |
| POST | `/platform/impersonation-sessions/{id}/end` | SuperAdmin | — | 204 | 204, 403, 404 |

---
# 9. Authentication

## 9.1 JWT Access Tokens

- Signed with an asymmetric algorithm (RS256 recommended) so token validation can happen without sharing the signing key across services.
- Short-lived: default **15 minutes** expiry.
- Claims included: `sub` (user/candidate ID), `email`, `principal_type` (`InternalUser` | `Candidate`), `company_id` (null for Candidate and SuperAdmin), `role`, a `permissions` claim array (or the API derives permissions server-side from `role` at request time via a permission-mapping table — either is acceptable, but **permissions must be checked via a claim/permission abstraction, not raw role string comparisons in handler code**, per §5.2.4 note), `jti` (token ID, used for revocation-list checks on logout if a denylist approach is used alongside refresh rotation).

## 9.2 Refresh Tokens

- Opaque, cryptographically random string (not a JWT), stored hashed in the database (`RefreshToken` entity: `Id`, `UserId`/`CandidateId`, `TokenHash`, `ExpiresAtUtc`, `RevokedAtUtc` nullable, `ReplacedByTokenId` nullable, `CreatedByIp`).
- Default expiry: **14 days**, sliding (each successful refresh extends by issuing a new token and revoking the old one — **rotation**, not reuse).
- Refresh token reuse detection: if a refresh token that has already been rotated (has a `ReplacedByTokenId`) is presented again, this indicates possible token theft — the entire token family is revoked and the user is forced to re-authenticate. This event is logged at `Warning`/`Critical` severity.
- Refresh tokens are `Candidate`-scoped or `InternalUser`-scoped consistently with the access token they support; a refresh token cannot be used to mint an access token for a different `principal_type`.

## 9.3 Role-Based Authorization

- Every internal API endpoint declares the minimum role(s) required (see §8 tables).
- Implemented via a custom `[RequireRole(...)]` attribute or ASP.NET Core policy-based authorization (`AddAuthorization` with named policies per role/role-set), evaluated in the `AuthorizationBehavior` MediatR pipeline behavior (§7.18) so the rule is enforced at the application layer, not only at the controller/attribute layer — this matters because some MediatR handlers may be invoked from more than one entry point (e.g., a background job) where the ASP.NET Core attribute wouldn't apply.

## 9.4 Permission-Based Authorization

- Beyond coarse role checks, fine-grained actions use **permission claims** (see the permission list under each role in §2) evaluated against the specific resource being acted on — e.g., a Recruiter's `applications.manage.assigned` permission is not just "has Recruiter role" but "has Recruiter role AND is the owning Recruiter (or has been granted company-wide visibility) for this specific Application's Job."
- This resource-level check is implemented via an `IAuthorizationRequirement`-style handler that loads minimal ownership/scoping data (e.g., `JobPost.OwnerUserId`, `Department` scoping) and compares it against the current principal — not via broad role checks alone.
- Tenant isolation (a user from Company A can never act on Company B's data) is enforced at **two layers**: (1) the permission/authorization behavior compares `CompanyId` claim to the target entity's `CompanyId`, and (2) the EF Core global query filter (§5.1) prevents the entity from even being loaded cross-tenant, so a bug in layer 1 cannot leak data past layer 2.

## 9.5 Password Reset

- `RequestPasswordResetCommand` always returns `204 No Content` regardless of whether the email exists, to prevent user enumeration.
- Reset token: single-use, expires after **1 hour**, stored hashed.
- On successful reset, all existing refresh tokens for that user/candidate are revoked (force re-login everywhere) as a security measure.

## 9.6 Email Verification

- Applies to Candidates (mandatory before applying, per §4.3) and optionally to internal Users (internal users are pre-verified implicitly by completing the invitation-acceptance flow, since receiving and clicking the invitation email is itself a form of verification — no separate step needed).
- Verification token: single-use, expires after **24 hours**; candidate can request a new verification email if expired.

## 9.7 Identity Disambiguation (Internal User vs. Candidate with same email)

- Because `User.Email` and `Candidate.Email` are uniqueness-enforced in **separate** tables (not a shared identity table in v1 — see §5.2.4/§5.2.5), the same email address could theoretically exist as both an internal User at Company A and a Candidate account.
- The `LoginCommand` requires an explicit `context` parameter (`Internal` | `Candidate`) so login always resolves unambiguously against the correct table; the client (web app) determines context based on which portal the user is logging into (company/internal portal vs. candidate portal).
- This is a deliberate simplification for v1; unifying identity across contexts is listed as a future improvement (§18) if the business later wants a single login experience.

## 9.8 Super Admin Impersonation (Support Access)

- A Super Admin can start a time-boxed impersonation session against a specific Company, **only** with a logged reason string.
- The resulting `impersonationToken` is a distinct token type carrying the target company's context but flagged internally as `IsImpersonation = true` and `ImpersonatedByUserId = {superAdminId}` — every audit log entry written during an impersonation session includes both the acting Super Admin's ID and the impersonation flag, at `Critical` severity.
- Impersonation sessions expire after **30 minutes** or on explicit `end` call, whichever is first.
- Impersonation still respects normal role permissions of whatever internal role context is granted (typically Company Admin-equivalent for support purposes) — it does not grant a special "God mode" bypassing business rules; it grants **visibility and action capability equivalent to a Company Admin of that tenant**, fully audited.

---

# 10. Notifications

## 10.1 In-App Notifications — Recap and Trigger Matrix

See §4.12 for structural detail. The table below maps domain events (§6.4) to in-app notification recipients.

| Domain Event | In-App Recipient(s) |
|---|---|
| `UserInvited` | (no in-app — user doesn't have an account yet; email only) |
| `UserActivated` | Company Admin(s) who invited them |
| `JobPostSubmittedForApproval` | HR, Company Admin |
| `JobPostRejectedByApprover` | Job's Owner Recruiter |
| `JobPostPublished` | Job's Owner Recruiter (confirmation) |
| `ApplicationSubmitted` | Job's Owner Recruiter (or company-wide Recruiter queue if unassigned) |
| `ApplicationAdvancedStage` | Owning Candidate (candidate-facing label) |
| `ApplicationRejected` | Owning Candidate |
| `ApplicationWithdrawn` | Job's Owner Recruiter |
| `InterviewScheduled` | Assigned Interviewer(s), Owning Candidate |
| `InterviewRescheduled` | Assigned Interviewer(s), Owning Candidate |
| `InterviewCancelled` | Assigned Interviewer(s), Owning Candidate |
| `InterviewFeedbackSubmitted` | (no immediate recipient unless it completes the set) |
| `AllInterviewFeedbackComplete` | Job's Owner Recruiter, HR |
| `OfferSubmittedForApproval` | HR, Company Admin |
| `OfferApproved` | Offer's Creator (Recruiter) |
| `OfferSent` | Owning Candidate |
| `OfferAccepted` | Job's Owner Recruiter, HR, Company Admin |
| `OfferDeclined` | Job's Owner Recruiter, HR |
| `OfferExpired` | Job's Owner Recruiter |
| `OfferRetracted` | Owning Candidate |

## 10.2 Email Notifications — Trigger Matrix

Mirrors §10.1 but for candidate-facing events (and select internal events where email is more appropriate than in-app, e.g., invitations which precede account existence):

- `UserInvited` → Email to invitee with activation link.
- `CandidateRegistered` → Email verification link.
- `ApplicationSubmitted` → Confirmation email to candidate.
- `ApplicationAdvancedStage` → Status update email to candidate (candidate-facing label only).
- `ApplicationRejected` → Rejection email to candidate (templated, respectful, no internal detail).
- `InterviewScheduled` / `Rescheduled` / `Cancelled` → Email to candidate and to each interviewer, with calendar-friendly details (date/time/mode/location).
- `OfferSent` → Email to candidate with offer summary and a link to respond.
- `OfferAccepted` / `Declined` → Internal confirmation email to Recruiter/HR.
- `PasswordResetRequested` → Reset link email.

## 10.3 Notification Content Rules

- Candidate-facing notifications/emails **never** include: internal notes, interviewer identity beyond the configured visibility level, interview feedback content, or other candidates' information.
- Internal notifications may include richer detail (candidate name, job title, direct deep-link into the internal app).

---

# 11. Workflow — The Hiring Pipeline (End-to-End)

This section describes the canonical, default hiring workflow. Company Admins can customize stage names/count (§4.8), but the **state machine shape** described here (an Initial stage, N standard/interview/offer stages, and terminal Hired/Rejected/Withdrawn states) is the structural contract the rest of the system relies on.

## 11.1 Stage-by-Stage Narrative

1. **Candidate Applies** — Candidate submits an `Application` against a `Published` JobPost. Application enters the pipeline's `Initial` stage (default label: "Applied"). `ApplicationSubmitted` event fires.

2. **Recruiter Reviews** — Recruiter (or HR) reviews the application, either advancing it to a `Standard`-type screening stage (default: "Screening") or rejecting it outright with a reason. This is the first decision point.

3. **Interview Scheduled** — Once an application reaches an `Interview`-type stage, a Recruiter/HR schedules one or more interview rounds. Multiple interviews can occur across the same or multiple `Interview`-type stages (e.g., "Phone Screen" then "Technical Interview" then "Final Round" — all `Interview`-type stages, each with its own Interview records).

4. **Feedback Collected** — After each interview, assigned Interviewer(s) submit structured feedback (§4.10). Blind feedback rules apply until the interviewer's own submission is complete.

5. **Decision Point** — Once all feedback for the current interview round is complete, the Recruiter/HR reviews the aggregated `FeedbackSummaryDto` and decides to: advance to the next stage (another interview round, or to the `Offer` stage), or reject (reason required).

6. **Offer** — Once the application reaches an `Offer`-type stage, a Recruiter drafts an `Offer`. It goes through approval (if `RequireOfferApproval`), is sent to the candidate, and awaits response.

7. **Accepted** — Candidate accepts → Application transitions to `Hired` (terminal-positive). Job's `PositionsFilled` increments. Internal stakeholders notified.

8. **Rejected** — At any non-terminal stage, a Recruiter/HR/Company Admin can reject the application (reason required) → Application transitions to `Rejected` (terminal-negative). Candidate notified respectfully.

9. **Withdrawn** — At any non-terminal stage (including after an offer is sent but before acceptance), the Candidate can withdraw their own application → Application transitions to `Withdrawn` (terminal-negative, but distinct from `Rejected` for reporting — this was the candidate's choice, not the company's). If a `Sent` offer exists when withdrawal occurs, that offer is automatically transitioned to `Retracted` (system-initiated, reason auto-set to "Application withdrawn by candidate").

## 11.2 State Diagram (textual)

```
[Initial: Applied]
   │
   ├──(Recruiter rejects)──────────────────────────────► [Rejected]
   ├──(Candidate withdraws)────────────────────────────► [Withdrawn]
   ▼
[Standard: Screening]
   │
   ├──(reject)──────────────────────────────────────────► [Rejected]
   ├──(withdraw)────────────────────────────────────────► [Withdrawn]
   ▼
[Interview: Round 1] ──(schedule/feedback loop, §11.1 steps 3-5)──┐
   │                                                              │
   ├──(reject)──────────────────────────────────────────► [Rejected]
   ├──(withdraw)────────────────────────────────────────► [Withdrawn]
   ▼                                                              │
[Interview: Round 2 / Final] (optional, company-configured) ◄─────┘
   │
   ├──(reject)──────────────────────────────────────────► [Rejected]
   ├──(withdraw)────────────────────────────────────────► [Withdrawn]
   ▼
[Offer]
   │
   ├──(offer declined by candidate)──────────────────────► [DeclinedByCandidate] (terminal-negative variant)
   ├──(offer retracted by company)───────────────────────► [Rejected] (or a dedicated terminal state, company-configurable)
   ├──(candidate withdraws)──────────────────────────────► [Withdrawn]
   ▼
[Offer Accepted]
   │
   ▼
[Hired] (terminal-positive)
```

## 11.3 Candidate-Facing Status Labels

Because internal stage names can be customized and may contain internal jargon, the Candidate-facing API (`CandidateApplicationStatusDto`) maps each internal `PipelineStage.Type` to a fixed, friendly candidate-facing label, **not** the raw internal stage name:

| Internal Stage Type | Candidate-Facing Label |
|---|---|
| `Initial` | "Application Received" |
| `Standard` | "Under Review" |
| `Interview` | "Interview Stage" |
| `Offer` | "Offer Stage" |
| `TerminalPositive` (Hired) | "Hired" |
| `TerminalNegative` (Rejected) | "Not Selected" |
| `TerminalNegative` (Withdrawn) | "Withdrawn by You" |

This mapping is a deliberate design decision to prevent internal stage-naming customization from leaking confusing or overly candid internal terminology (e.g., a stage literally named "Manager Pushback") to candidates.

---

# 12. Business Rules

Consolidated list of cross-cutting business rules (many detailed already per-feature in §4; this section is the canonical, quick-reference list an implementer/QA engineer should validate against).

1. A Job Post **cannot be published** without: title, description (≥50 chars), department, employment type, location, ≥1 requirement, open positions ≥1. (§4.6)
2. A Candidate **cannot have two simultaneous active Applications** for the same Job Post. "Active" excludes `Rejected`/`Withdrawn`/`DeclinedByCandidate`. (§4.4, §5.2.8)
3. Re-application after rejection/withdrawal is blocked until `ReapplicationCooldownDays` (default 90) has elapsed, company-configurable. (§4.4)
4. **Interviews cannot overlap** for the same interviewer — validated both at request-validation time and again at commit time inside the same transaction. (§4.9)
5. **Only Recruiter (assigned/owning), HR, or Company Admin can move a candidate through the pipeline.** Interviewers cannot; Candidates can only withdraw (a special-cased self-service terminal transition, not a general stage move). (§2, §4.7)
6. Rejecting an Application **requires a reason** from a configurable reason list, optionally with free text. (§4.7)
7. Interview Feedback is **immutable** once submitted; corrections happen via an explicit, separately-flagged addendum, never an edit of the original. (§4.10)
8. **Blind feedback** (default on): an interviewer cannot view others' feedback for the same interview/candidate until their own is submitted. (§4.10)
9. An Application cannot have more than **one active Offer** at a time (`Draft` through `Sent` inclusive count as active). (§4.11, §5.2.19)
10. Accepting an Offer **automatically** transitions the Application to `Hired` and increments the Job's `PositionsFilled` — this is not a manual, separate step. (§4.11)
11. Offers require HR/Company Admin approval before sending **by default** (`RequireOfferApproval = true`), company-configurable to off.
12. A CV attached to a **submitted** Application becomes an immutable snapshot; later edits/deletions to the candidate's live document library never retroactively alter a submitted application's attached document. (§4.5)
13. **Super Admin cannot silently access tenant candidate/application/feedback data** without an explicit, reason-logged, time-boxed impersonation session (§9.8). This is enforced at the authorization-handler level, not merely a UI convention.
14. Audit log entries are **immutable and append-only** — no update or delete capability exists at any role level, including Super Admin, anywhere in the system. (§4.17)
15. Tenant data isolation is absolute: no cross-`CompanyId` data access is possible for any Company-scoped role, enforced redundantly at both the authorization layer and the EF Core global query filter layer. (§9.4)
16. A Pipeline Stage that currently has one or more Applications sitting in it **cannot be deleted**; it must be emptied (applications moved elsewhere) or the deletion is rejected with a dependency error. (§4.8)
17. Deleting a `CompetencyDimension` or `RejectionReason` that has historical references is a **soft-delete/deprecation**, never a hard delete, to preserve historical reporting integrity. (§4.16, §5.2.18)
18. A candidate withdrawing their Application while a `Sent` Offer exists **automatically retracts** that Offer (system-initiated, reason auto-populated). (§11.1)
19. All monetary values (Offer compensation) must be non-negative; currency is always an explicit ISO 4217 code, never assumed. (§5.2.19)
20. All timestamps are stored in UTC; timezone conversion happens only at the presentation/API-response boundary, using the relevant user's or company's configured timezone. (§4.9)
21. Optimistic concurrency (`RowVersion`) is required on all frequently-concurrently-edited entities (JobPost, Application, Offer, Interview) — a stale write must fail with `409 Conflict`, never silently overwrite. (§5.1, §4.6, §4.11)
22. Global search/list queries default to excluding soft-deleted (`IsDeleted = 1`) records; no query anywhere in the system returns soft-deleted records unless explicitly requested via an admin-only "include deleted" flag (Company Admin/Super Admin audit contexts only).
23. Rate-of-change validation: a Job Post's material fields (title, salary, employment type) can be edited post-publish without re-approval by default, but every such edit is captured in the audit log as a diff for governance visibility. (§4.6)
24. Hard-delete (true data erasure) is reserved exclusively for a narrow, explicitly-designed compliance/GDPR-erasure flow (out of full detail in this document but the schema must support it — see soft-delete design in §5.1) and is never a capability exposed via a generic "delete" API endpoint for business entities other than that dedicated flow.

---

# 13. Non-Functional Requirements

## 13.1 Performance

- API p95 response time target: **< 300ms** for standard read endpoints, **< 800ms** for write endpoints involving validation + audit logging, under expected load (defined per deployment sizing, but baseline target: 50 concurrent users per mid-size tenant, 200 tenants).
- List/search endpoints must be paginated by default (max page size capped, e.g., 100) — no unbounded result sets.
- Dashboard and report queries should be backed by indexed columns (see §5 index list) and, for heavier aggregate reports, consider dedicated read-model/materialized-view projections updated asynchronously from domain events rather than computing aggregates from raw transactional tables on every request, once data volume warrants it (explicitly deferred optimization — acceptable to start with direct queries against well-indexed tables, revisit if p95 targets are missed).

## 13.2 Security

- All traffic over TLS 1.2+.
- Passwords hashed with a modern adaptive algorithm (e.g., Argon2id or bcrypt with adequate work factor) — never reversible encryption, never plain text.
- JWT signing keys rotated on a defined schedule (e.g., every 90 days) with overlap support for graceful rotation (multiple valid signing keys during transition).
- All file uploads (CVs) scanned for malware before being made retrievable (§4.5).
- Input validation at every layer: FluentValidation for command/query shape, EF Core parameterized queries (no raw SQL string concatenation, preventing SQL injection by construction), output encoding at the API boundary.
- Tenant isolation enforced redundantly (§9.4, §12.15).
- Sensitive fields (password hashes, refresh token hashes) are never included in any API response DTO, ever, under any role — verified via DTO-level whitelisting (explicit property mapping), not blacklisting.
- Rate limiting on authentication endpoints (login, password reset, email verification) to mitigate brute-force and enumeration attacks.

## 13.3 Logging

See §14 for full detail. Summary requirement: structured logging (not plain string concatenation) across all layers, with correlation IDs propagated through a request's full lifecycle (API → MediatR pipeline → EF Core → any background job triggered).

## 13.4 Caching

- Read-heavy, low-volatility data (e.g., active `PipelineStageTemplate`, `CompanySettings`, `CompetencyDimensions`, `RejectionReasons`) are strong candidates for a short-TTL (e.g., 5-minute) in-memory or distributed cache (Redis, if horizontally scaled) to reduce DB load, invalidated on the relevant `UpdateXCommand` completing.
- Public job listing (`/public/jobs`) is a strong candidate for caching given its high read volume and public/anonymous access pattern — cache invalidation triggered by `JobPostPublished`/`JobPostClosed`/`JobPostArchived` events.
- Caching must never be applied to per-user authorization-sensitive data (e.g., never cache an `ApplicationDetailDto` response keyed only by ID without factoring in the requesting user's scope) — the risk of cross-tenant/cross-role data leakage via a shared cache key is unacceptable; if caching authorization-sensitive data, the cache key must include the requesting principal's scope-defining attributes (e.g., `CompanyId`, `Role`).

## 13.5 Scalability

- Stateless API layer (JWT-based auth, no server-side session state) to support horizontal scaling behind a load balancer.
- Database: SQL Server with tenant data logically (not physically) partitioned via `CompanyId` in v1; a future improvement (§18) could consider physical sharding by tenant if a single very large enterprise customer's data volume warrants it.
- Background/async work (email sending, report exports, notification fan-out for bulk operations) offloaded to a queue-backed worker process rather than executed inline in the API request thread.

## 13.6 Availability

- Target: 99.5% uptime for v1 (typical SaaS SLA for a growth-stage product; can be revisited once enterprise customers with contractual SLA requirements are onboarded).
- Health check endpoints (§14.3) integrated with the deployment platform's readiness/liveness probes to support safe rolling deployments and automatic recovery from unhealthy instances.
- Database backups: automated daily full backup + transaction log backups (e.g., every 15 minutes) to support point-in-time recovery, given the append-only audit log and hiring-decision data's compliance sensitivity.

## 13.7 Maintainability

- Clean Architecture layering (§15) with strict dependency direction (Domain has zero dependencies on Application/Infrastructure/API) is itself a maintainability requirement — enforced via architecture tests (e.g., NetArchTest or equivalent) run in CI to catch layering violations automatically, not just via code review discipline.
- Consistent CQRS structure (§7) across all modules means a new engineer (or AI coding agent) can predict where any given piece of logic lives without needing tribal knowledge.
- Comprehensive automated test coverage (unit tests for domain logic and validators, integration tests for command/query handlers against a real or in-memory-equivalent database, and a smaller set of end-to-end API tests for critical flows like application submission and offer acceptance) is a maintainability requirement, not just a QA nicety — the goal is that the AI coding agent implementing this spec can safely refactor with a regression safety net.

---

# 14. Logging & Monitoring

## 14.1 Audit Logs

Already fully specified functionally in §4.17 and structurally in §5.2.23. This subsection adds the operational/monitoring angle: audit log write failures for `Critical`-severity actions (role changes, impersonation, offer rescission, deletions) must trigger an alert to the operations team (via whatever alerting integration the deployment uses — e.g., a webhook to an incident tool), not just a log line, because a failed audit write on a sensitive action is itself a compliance incident.

## 14.2 Application Logging (Serilog)

- Serilog (or an equivalent structured logging library) configured with:
  - Console sink (for container log aggregation in most cloud deployments) and a durable sink (e.g., a log aggregation service, file, or Application Insights/Seq/ELK — deployment-specific, left as a configuration choice, not hardcoded).
  - Structured properties on every log event: `CorrelationId` (propagated from an incoming `X-Correlation-Id` header or generated per-request), `CompanyId` (when in a tenant context), `UserId`/`CandidateId` (when authenticated), `RequestPath`, `RequestMethod`.
  - Log levels used deliberately: `Debug` (verbose, dev-only), `Information` (normal operational events — request completed, command handled), `Warning` (recoverable anomalies — validation failures, retried email sends, refresh-token reuse detection), `Error` (unhandled exceptions, failed external calls after retries exhausted), `Fatal`/`Critical` (application cannot continue, e.g., cannot connect to database at startup).
  - **PII discipline in logs:** log messages must never include full candidate CV content, full passwords/tokens (even hashed forms should be truncated/redacted in log output), or other sensitive free-text fields in plain form — log entity IDs and event types, not full payloads, except where a structured `DetailsJson` audit field is the deliberate, access-controlled exception (audit logs are a separate, access-controlled store from general application logs).

## 14.3 Health Checks

- `/health/live` — liveness probe: process is running and can respond (no external dependency checks).
- `/health/ready` — readiness probe: checks database connectivity, and (if used) cache/queue connectivity, before reporting healthy — used by the deployment platform to decide whether to route traffic to this instance.
- `/health/detailed` — internal/ops-only endpoint (not public) returning per-dependency status (DB, email provider, blob storage, cache) for troubleshooting.

## 14.4 Metrics

- Request-level metrics: request count, latency (p50/p95/p99), error rate, broken down by endpoint — exposed via a metrics endpoint/exporter compatible with common observability stacks (e.g., Prometheus-style `/metrics` or an APM agent, deployment-specific).
- Business metrics worth exposing operationally (distinct from the candidate-facing Reports module, §4.15, though they may share underlying data): applications submitted per day, average time-to-hire trend, active company count, email delivery failure rate — useful for the platform operations team (Super Admin) to monitor overall system health versus just infrastructure health.
- Background job metrics: queue depth, job failure rate, retry counts (particularly for email sending and report export jobs).

---
# 15. Architecture

## 15.1 Architectural Style

The system uses **Clean Architecture** with **CQRS** implemented via **MediatR**, layered as follows (dependency direction always points inward — outer layers depend on inner layers, never the reverse):

```
┌─────────────────────────────────────────────────────────┐
│  API (Presentation)                                       │
│  Controllers, Middleware, DTOs (request/response),        │
│  Swagger/OpenAPI config, API-level filters                │
└───────────────────────┬───────────────────────────────────┘
                         │ depends on
┌───────────────────────▼───────────────────────────────────┐
│  Application                                               │
│  Commands, Queries, Handlers, Validators (FluentValidation)│
│  DTOs, Mapping profiles, Pipeline Behaviors,                │
│  Interfaces for Infrastructure concerns                     │
│  (IEmailSender, IDocumentStorage, IUnitOfWork, IRepository) │
└───────────────────────┬───────────────────────────────────┘
                         │ depends on
┌───────────────────────▼───────────────────────────────────┐
│  Domain                                                     │
│  Aggregates, Entities, Value Objects, Domain Events,         │
│  Domain Services (e.g., IInterviewOverlapPolicy interface    │
│  defined here, implemented in Infrastructure/Application),   │
│  Enums, Domain Exceptions — ZERO external dependencies        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  Infrastructure                                            │
│  EF Core DbContext, Migrations, Repository implementations,│
│  Unit of Work implementation, Email provider integration,  │
│  Blob storage integration, external service clients         │
│  (implements interfaces defined in Application)             │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  Persistence  (may be merged into Infrastructure, or kept  │
│  as its own project if the team wants a cleaner separation  │
│  between "EF Core/DB-specific" and "other infra" — this      │
│  spec keeps them as separate projects for clarity, see §16) │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│  Shared / Common                                            │
│  Cross-cutting utility types used by multiple layers          │
│  without creating illegal dependency directions               │
│  (e.g., Result<T> wrapper, PagedResult<T>, generic exceptions)│
└─────────────────────────────────────────────────────────────┘
```

**Infrastructure and Persistence depend on Application and Domain (to implement their interfaces), never the other way around.** The API layer composes everything at startup (Dependency Injection registration) but contains no business logic itself.

## 15.2 CQRS + MediatR

- Every use case is a Command or Query, dispatched through `IMediator.Send()`.
- Handlers are thin: validate (delegated to the pipeline), execute, persist, return a DTO.
- MediatR `INotification` + `INotificationHandler<T>` is used for Domain Events (§6.4) dispatched **after** the originating transaction commits (using a `IDomainEventDispatcher` invoked from a `SaveChangesInterceptor` or an explicit post-commit dispatch step in the pipeline behavior — not raised mid-transaction, to avoid partial-commit visibility issues for subscribers like email/notifications that shouldn't fire if the transaction ultimately rolls back).

## 15.3 Repository Pattern & Unit of Work

- Generic `IRepository<TAggregateRoot>` interface (defined in Application, implemented in Infrastructure/Persistence) providing `GetByIdAsync`, `Add`, `Remove` (soft-delete semantics), and aggregate-specific repositories (e.g., `IApplicationRepository`) adding specialized query methods needed by handlers that don't belong in a generic interface (e.g., `GetActiveApplicationForCandidateAndJobAsync`).
- `IUnitOfWork` interface exposes `SaveChangesAsync()`, wrapping the EF Core `DbContext`'s change tracking; the `TransactionBehavior` pipeline behavior (§7.18) calls this once per Command after the handler completes its aggregate mutations, ensuring one logical transaction per command by default (with explicit exceptions clearly documented in code where a command legitimately needs to touch two aggregates non-atomically, e.g., triggering an async email queue entry that is intentionally eventually-consistent).

## 15.4 FluentValidation

- One validator class per Command/Query (where input validation is meaningful — simple parameterless queries may skip this).
- Validators focus on **input shape/format validation** (required fields, string lengths, format, enum membership); **business rule validation** that requires domain knowledge or database state (e.g., "this stage transition is not valid from the current state," "this interviewer is already booked") lives in the domain/handler layer, not in FluentValidation rules, to keep the responsibility boundary clean — except where an async DB-dependent check is pragmatically implemented as a `MustAsync` validator rule for convenience (see §7.9 interview overlap note), which is an accepted exception given MediatR pipeline ordering benefits.

## 15.5 Specification Pattern

- Used for composable, reusable, testable query criteria — e.g., an `ActiveApplicationsForJobSpecification`, a `PendingApprovalOffersSpecification` — implemented as classes exposing an `Expression<Func<TEntity, bool>>` (or equivalent) that can be combined (`And`/`Or`) and passed into repository query methods, rather than scattering raw LINQ predicates across handlers. Particularly valuable for the Reports module (§4.15, §7.15) where filter criteria are numerous and combinable.

## 15.6 Dependency Injection

- All services registered in the API layer's composition root (`Program.cs` or equivalent), organized into extension methods per layer (`AddApplicationServices()`, `AddInfrastructureServices()`, `AddPersistenceServices()`) to keep startup configuration readable and to let each layer "own" its own registration logic without the API layer needing intimate knowledge of every implementation type.
- Scoped lifetime for `DbContext`, repositories, and `IUnitOfWork` (per-request); Singleton for stateless services like `IPasswordHasher`, configuration-bound options; Transient for lightweight, stateless helper services.

## 15.7 Global Exception Middleware

- A single exception-handling middleware at the top of the API pipeline catches all unhandled exceptions and maps them to a consistent `ProblemDetails`-style JSON error response (RFC 7807), including: `type`, `title`, `status`, `detail`, `traceId` (correlation ID for support/log correlation).
- Domain-specific exceptions (e.g., a custom `DomainRuleViolationException`, `ConcurrencyConflictException`, `NotFoundException`, `ForbiddenException`) are mapped to their appropriate HTTP status codes by this middleware, keeping controllers free of repetitive try/catch blocks.
- Unhandled/unexpected exceptions are logged at `Error` severity with full stack trace (server-side only — never exposed in the API response body beyond a generic message + trace ID, to avoid leaking implementation details to clients).

---

# 16. Folder Structure

```
ATS.sln
│
├── src/
│   ├── ATS.API/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── UsersController.cs
│   │   │   ├── CompaniesController.cs
│   │   │   ├── DepartmentsController.cs
│   │   │   ├── JobPostsController.cs
│   │   │   ├── PublicJobsController.cs
│   │   │   ├── ApplicationsController.cs
│   │   │   ├── DocumentsController.cs
│   │   │   ├── PipelineController.cs
│   │   │   ├── InterviewsController.cs
│   │   │   ├── OffersController.cs
│   │   │   ├── NotificationsController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── ReportsController.cs
│   │   │   ├── SettingsController.cs
│   │   │   ├── AuditLogsController.cs
│   │   │   └── PlatformController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── CorrelationIdMiddleware.cs
│   │   ├── Filters/
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── ATS.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── AuthorizationBehavior.cs
│   │   │   │   ├── TransactionBehavior.cs
│   │   │   │   └── AuditLoggingBehavior.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   ├── IEmailSender.cs
│   │   │   │   ├── IDocumentStorage.cs
│   │   │   │   └── IDateTimeProvider.cs
│   │   │   ├── Models/
│   │   │   │   ├── PagedResult.cs
│   │   │   │   └── Result.cs
│   │   │   └── Exceptions/
│   │   │       ├── NotFoundException.cs
│   │   │       ├── ForbiddenException.cs
│   │   │       ├── ConcurrencyConflictException.cs
│   │   │       └── DomainRuleViolationException.cs
│   │   ├── Auth/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Users/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   ├── Companies/
│   │   ├── Departments/
│   │   ├── JobPosts/
│   │   ├── Applications/
│   │   ├── Documents/
│   │   ├── Pipeline/
│   │   ├── Interviews/
│   │   ├── InterviewFeedback/
│   │   ├── Offers/
│   │   ├── Notifications/
│   │   ├── Dashboard/
│   │   ├── Reports/
│   │   ├── Settings/
│   │   └── AuditLogs/
│   │       (each of the above modules follows the same internal shape:)
│   │       ├── Commands/
│   │       │   └── {CommandName}/
│   │       │       ├── {CommandName}Command.cs
│   │       │       ├── {CommandName}CommandHandler.cs
│   │       │       └── {CommandName}CommandValidator.cs
│   │       ├── Queries/
│   │       │   └── {QueryName}/
│   │       │       ├── {QueryName}Query.cs
│   │       │       └── {QueryName}QueryHandler.cs
│   │       ├── Dtos/
│   │       └── Mappings/
│   │
│   ├── ATS.Domain/
│   │   ├── Aggregates/
│   │   │   ├── Companies/
│   │   │   │   ├── Company.cs
│   │   │   │   ├── Department.cs
│   │   │   │   └── CompanySettings.cs
│   │   │   ├── Users/
│   │   │   │   └── User.cs
│   │   │   ├── Candidates/
│   │   │   │   ├── Candidate.cs
│   │   │   │   └── Document.cs
│   │   │   ├── JobPosts/
│   │   │   │   ├── JobPost.cs
│   │   │   │   └── JobRequirement.cs
│   │   │   ├── Applications/
│   │   │   │   ├── Application.cs
│   │   │   │   ├── ApplicationDocument.cs
│   │   │   │   └── ApplicationStageHistory.cs
│   │   │   ├── Pipeline/
│   │   │   │   ├── PipelineStageTemplate.cs
│   │   │   │   └── PipelineStage.cs
│   │   │   ├── Interviews/
│   │   │   │   ├── Interview.cs
│   │   │   │   ├── InterviewParticipant.cs
│   │   │   │   ├── InterviewFeedback.cs
│   │   │   │   └── InterviewFeedbackRating.cs
│   │   │   └── Offers/
│   │   │       └── Offer.cs
│   │   ├── ValueObjects/
│   │   │   ├── EmailAddress.cs
│   │   │   ├── Money.cs
│   │   │   ├── DateRange.cs
│   │   │   ├── PersonName.cs
│   │   │   └── PhoneNumber.cs
│   │   ├── Events/
│   │   │   └── (one file per domain event, or grouped per aggregate folder)
│   │   ├── Enums/
│   │   │   ├── UserRole.cs
│   │   │   ├── ApplicationStatus.cs
│   │   │   ├── JobPostStatus.cs
│   │   │   ├── InterviewStatus.cs
│   │   │   ├── OfferStatus.cs
│   │   │   └── ...
│   │   ├── Services/
│   │   │   └── IInterviewOverlapPolicy.cs
│   │   ├── Exceptions/
│   │   └── Common/
│   │       ├── AggregateRoot.cs
│   │       ├── Entity.cs
│   │       └── ValueObject.cs
│   │
│   ├── ATS.Infrastructure/
│   │   ├── Email/
│   │   │   ├── SmtpEmailSender.cs
│   │   │   └── EmailTemplateRenderer.cs
│   │   ├── Storage/
│   │   │   └── BlobDocumentStorage.cs
│   │   ├── Security/
│   │   │   ├── PasswordHasher.cs
│   │   │   └── JwtTokenGenerator.cs
│   │   ├── Identity/
│   │   │   └── CurrentUserService.cs
│   │   └── BackgroundJobs/
│   │       ├── OfferExpiryJob.cs
│   │       ├── SelfScheduleExpiryJob.cs
│   │       └── ReportExportJob.cs
│   │
│   ├── ATS.Persistence/
│   │   ├── AtsDbContext.cs
│   │   ├── Configurations/
│   │   │   └── (one EF Core IEntityTypeConfiguration<T> per entity)
│   │   ├── Migrations/
│   │   ├── Repositories/
│   │   │   └── (one repository implementation per aggregate root)
│   │   ├── Interceptors/
│   │   │   ├── AuditableEntitySaveChangesInterceptor.cs
│   │   │   └── DomainEventDispatchInterceptor.cs
│   │   └── UnitOfWork.cs
│   │
│   └── ATS.Shared/
│       ├── Constants/
│       └── Extensions/
│
└── tests/
    ├── ATS.Domain.UnitTests/
    ├── ATS.Application.UnitTests/
    ├── ATS.Application.IntegrationTests/
    │   └── (per-module test folders mirroring ATS.Application structure)
    ├── ATS.API.IntegrationTests/
    │   └── (end-to-end tests hitting real HTTP endpoints against a test server + test DB)
    └── ATS.ArchitectureTests/
        └── LayeringRulesTests.cs   (enforces Clean Architecture dependency direction in CI)
```

---

# 17. Coding Standards

## 17.1 Naming Conventions

- **Classes/Interfaces/Enums/Public members:** PascalCase (`ApplicationService`, `IEmailSender`, `JobPostStatus`).
- **Interfaces:** prefixed with `I` (`IRepository<T>`).
- **Private fields:** camelCase with leading underscore (`_dbContext`).
- **Parameters/local variables:** camelCase.
- **Commands/Queries:** named as verb-phrases ending in `Command`/`Query` (`RejectApplicationCommand`, `GetJobPostsQuery`) — never ambiguous nouns.
- **Handlers:** `{RequestName}Handler` (`RejectApplicationCommandHandler`).
- **DTOs:** `{Entity}Dto` for full representations, `{Entity}SummaryDto` for list/lightweight representations, `{Context}{Entity}Dto` for context-specific shapes (`CandidateApplicationStatusDto`, `PublicJobPostDto`).
- **Domain Events:** past-tense (`ApplicationRejected`, not `RejectApplication`).
- **Async methods:** suffixed `Async` (`GetByIdAsync`).

## 17.2 SOLID Principles Applied

- **Single Responsibility:** each Command Handler does exactly one thing; validators only validate; mapping logic doesn't leak business rules.
- **Open/Closed:** new notification channels (SMS, §18) extend the system by adding new domain event subscribers, not by modifying existing command handlers.
- **Liskov Substitution:** repository/service interfaces are implemented such that any conforming implementation (e.g., swapping `SmtpEmailSender` for a different provider) is a drop-in replacement with no behavioral surprises for callers.
- **Interface Segregation:** prefer focused interfaces (`IApplicationRepository` with domain-specific methods) over one giant generic repository interface that every aggregate is forced to implement identically regardless of fit.
- **Dependency Inversion:** Application layer defines interfaces (`IEmailSender`, `IDocumentStorage`); Infrastructure implements them. Domain never references Infrastructure or Application at all.

## 17.3 Clean Code Practices

- Methods kept short and single-purpose; a Command Handler's `Handle` method should read as an ordered list of clearly-named steps (load aggregate → validate business rule → mutate → persist → return DTO), delegating detail to well-named private methods or domain methods rather than being a wall of inline logic.
- Avoid primitive obsession for domain-meaningful values — prefer Value Objects (§6.3) over raw strings/decimals where an invariant needs enforcing (e.g., `Money`, `EmailAddress`).
- No magic strings/numbers for business-meaningful constants (stage type checks, permission strings) — centralize as enums/constants.
- Guard clauses at the top of methods for precondition checks, avoiding deep nesting.
- Comments explain **why**, not **what** — the code itself should be readable enough to convey what it does; comments are reserved for non-obvious business rationale (e.g., "// Blind feedback default is ON per company policy — see spec §12.8").

## 17.4 Testing Standards

- Domain layer: pure unit tests, no mocking framework needed for most cases since aggregates should be testable via plain constructors/methods and assertions on resulting state/raised events.
- Application layer: unit tests for validators (input shape) and handlers (with mocked repositories/services); integration tests for handlers against a real (or realistic in-memory/test-container) database to verify EF Core mapping, concurrency behavior, and query filter correctness (especially tenant isolation — this must have dedicated, explicit test coverage given its criticality per §12.15).
- API layer: a focused set of end-to-end tests covering the critical business flows described in §11 (full pipeline from application submission through hire), authentication flows, and authorization boundary tests (verifying a Recruiter genuinely cannot access another company's data, a Candidate genuinely cannot see internal fields, etc.) — these authorization boundary tests are considered **mandatory**, not optional, given the multi-tenant nature of the system.

---

# 18. Future Improvements

These are explicitly **out of scope for v1** but the architecture (event-driven, CQRS, cleanly-separated modules) is designed to accommodate them without major rework:

1. **AI CV Ranking** — automated scoring/ranking of candidates against a job's requirements using an LLM or ML model, surfaced as a non-authoritative "suggested ranking" (never an auto-reject) to assist Recruiter screening. Would integrate as a new subscriber to `ApplicationSubmitted` populating a `CvRankingScore` read-model field.
2. **Resume Parsing** — automatic extraction of structured candidate data (skills, experience, education) from uploaded CVs to pre-fill candidate profiles and enable structured search/filtering beyond keyword search.
3. **Calendar Integration** (Google Calendar / Outlook) — two-way sync for interview scheduling, replacing/augmenting the manual `WorkingHours`-based availability model (§4.16) with real calendar free/busy data, and pushing confirmed interviews as calendar events automatically.
4. **Video Conferencing Integration** (Teams/Zoom/Meet) — auto-generate meeting links when scheduling a `Video`-mode interview rather than requiring manual link entry.
5. **SMS Notifications** — an additional notification channel alongside email/in-app, particularly valuable for time-sensitive interview reminders; architecturally, this is simply a new domain event subscriber, consistent with §6.4's design intent.
6. **Advanced Analytics** — predictive time-to-hire estimates, diversity/inclusion funnel reporting (with appropriate legal/compliance review before implementation, given the sensitivity of demographic data), cohort-based source ROI analysis.
7. **Multi-company staffing for internal users** — allowing a single person (e.g., an external recruiting consultant) to hold accounts/roles across multiple companies without needing separate email-based accounts per tenant, which would require the `RoleAssignment` join-entity evolution flagged in §5.2.4.
8. **Unified identity across Candidate and Internal User contexts** — collapsing the current dual-table identity model (§9.7) into a single identity with context-switching, if product research shows this login disambiguation is a real UX pain point.
9. **Public API / Webhooks** for third-party integrations (e.g., pushing `ApplicationSubmitted` events to an external HRIS or a customer's own systems).
10. **Career Site Builder** — richer, brandable public-facing careers page beyond the current basic public job listing endpoints.
11. **Candidate Talent Pool / Sourcing** — allowing Recruiters to proactively tag and organize candidates who applied to one job as potential fits for future roles, independent of a specific active Application.
12. **Notification Batching/Digest** — summarizing high-frequency notification bursts (flagged as a v1 minimum-viable requirement not to fail under bulk load, with batching itself deferred — see §4.12).

---

# 19. Development Roadmap

## Phase 1 — Foundation (Weeks 1–3)
- Solution/project scaffolding per §16 folder structure.
- Domain layer: all aggregates, value objects, enums (no persistence yet).
- Persistence layer: `AtsDbContext`, entity configurations, initial migration.
- Authentication module (§4.1, §4.3, §9) end-to-end: registration, login, JWT/refresh, password reset, email verification.
- Companies, Departments, Users modules (tenant foundation + user invitation flow).
- Global exception middleware, correlation ID middleware, Serilog wiring, health checks.
- CI pipeline with architecture tests enforcing layering rules from day one.

## Phase 2 — Core Recruitment Flow (Weeks 4–7)
- Job Posts module (full lifecycle: draft → approval → publish → close → archive).
- CV/Document management module (upload, snapshot-on-submit logic).
- Applications module (submission, review, stage advancement, rejection, withdrawal).
- Hiring Pipeline module (default template seeding + customization).
- Public job listing endpoints.
- Audit logging pipeline behavior wired into all Phase 1–2 commands retroactively.

## Phase 3 — Interviews & Feedback (Weeks 8–10)
- Interview Scheduling module, including the overlap-check domain service and self-scheduling flow.
- Interview Feedback module, including blind-feedback visibility rules and competency dimension configuration.
- Notifications (in-app) module, wired as event subscribers to all Phase 2–3 domain events.
- Email module, including template resolution (company override vs. platform default) and retry/queue handling.

## Phase 4 — Offers & Hiring Completion (Weeks 11–12)
- Offers module (full state machine: draft → approval → send → respond → hired/declined/retracted/rescinded).
- Offer expiry background job.
- Full end-to-end pipeline integration tests (application → hire, application → reject at each stage, application → withdraw with offer retraction cascade).

## Phase 5 — Reporting, Dashboards, Settings (Weeks 13–15)
- Dashboard module (all five role-specific dashboards).
- Reports module (all five report types + async export for large datasets).
- Settings module (company configuration, competency dimensions, rejection reasons, email template overrides).
- Performance validation against NFR targets (§13.1) under simulated load; introduce caching (§13.4) where needed.

## Phase 6 — Platform Operations & Hardening (Weeks 16–18)
- Super Admin platform module (company onboarding/suspension, impersonation flow with full audit coverage).
- Security hardening pass: rate limiting, JWT key rotation setup, penetration-test-style review of tenant isolation and authorization boundaries.
- Full audit log query module (company-scoped and platform-scoped).
- Documentation finalization (API/Swagger polish, deployment runbook).
- UAT and bug-fixing buffer.

---

# 20. Final Deliverables

## 20.1 Development Checklist

- [ ] Solution scaffolded per §16 folder structure with all 7 projects (API, Application, Domain, Infrastructure, Persistence, Shared, Tests) and correct project references enforcing dependency direction.
- [ ] Architecture tests in CI failing the build on any layering violation.
- [ ] All 26 entities from §5.2 modeled in Domain with corresponding EF Core configurations in Persistence.
- [ ] All aggregate roots from §6.1 implemented with encapsulated state (no public setters bypassing business methods) and domain event raising on meaningful state changes.
- [ ] All Commands/Queries from §7 implemented with Handler + Validator (where applicable) + DTO + mapping.
- [ ] All API endpoints from §8 implemented with correct authorization attributes matching the role/permission matrix in §2.7.
- [ ] JWT + refresh token authentication fully implemented per §9, including rotation and reuse detection.
- [ ] All domain events from §6.4 wired to their notification/email subscribers per §10.1–10.2.
- [ ] Full hiring pipeline state machine implemented and tested against the diagram in §11.2, including candidate-facing label mapping (§11.3).
- [ ] All 24 business rules in §12 covered by at least one explicit automated test.
- [ ] NFR targets (§13) validated: p95 latency, tenant isolation test suite passing, rate limiting active on auth endpoints.
- [ ] Serilog structured logging, health checks, and metrics endpoints operational per §14.
- [ ] Global exception middleware returning consistent `ProblemDetails` responses for all documented error status codes.
- [ ] Automated test suite covering: Domain unit tests, Application unit + integration tests, API end-to-end tests for critical flows and authorization boundaries (§17.4).
- [ ] Swagger/OpenAPI documentation generated and accurate against §8.
- [ ] Database migrations reviewed for index coverage matching §5.2 index lists.
- [ ] Seed data script for default `PipelineStageTemplate`, default `EmailTemplate` set, and default `RejectionReason` list on company onboarding.

## 20.2 Module Dependency Graph

```
Authentication ──► (required by all other modules for identity context)

Companies ──► Departments ──► Users
     │
     └────► Settings ──► CompetencyDimensions, RejectionReasons, EmailTemplates
     │
     └────► PipelineStageTemplate (seeded on Company onboarding)

Departments ──► JobPosts ──► JobRequirements

Users (as Recruiter/HR/Interviewer) ──► JobPosts (ownership)
                                    └──► Interviews (participation)
                                    └──► InterviewFeedback (authorship)

Candidates ──► Documents
          └──► Applications

JobPosts + Candidates ──► Applications ──► ApplicationDocuments (needs Documents)
                                       └──► ApplicationStageHistory (needs PipelineStageTemplate/PipelineStage)
                                       └──► Interviews (needs Users as interviewers)
                                                └──► InterviewFeedback (needs CompetencyDimensions)
                                       └──► Offers

All state-mutating modules ──► AuditLogs (cross-cutting, via pipeline behavior)
All domain events ──► Notifications, Email (cross-cutting, via event subscribers)
Applications + Interviews + Offers + JobPosts ──► Dashboard, Reports (read-only aggregation)
```

**Implication for build order:** Authentication and Companies/Departments/Users must be built first (Phase 1). JobPosts and Applications form the next dependency tier (Phase 2), since almost everything downstream (Interviews, Feedback, Offers, Reports) references an Application. Interviews and Feedback depend on Users-as-interviewers and CompetencyDimensions (Settings), so Settings' relevant pieces must land before Phase 3. Offers depend on Applications reaching an Offer-type stage, so logically follow Interviews. Dashboard/Reports/AuditLogs are purely additive read-side concerns that can be built incrementally alongside or after their source modules without blocking other work.

## 20.3 Recommended Implementation Order

1. Domain layer (all aggregates/VOs/events) — no dependencies, fastest to get right early.
2. Persistence layer (DbContext, configurations, first migration) — needed before any handler can be tested against a real DB.
3. Authentication + Users + Companies + Departments (Phase 1 modules).
4. Settings (minimal viable version: defaults only) — needed before Pipeline/CompetencyDimensions are meaningful.
5. JobPosts.
6. CV/Documents.
7. Applications + Hiring Pipeline.
8. Interviews.
9. Interview Feedback.
10. Offers.
11. Notifications + Email (retrofit as subscribers to all events raised by modules 3–10).
12. AuditLogs (retrofit as a pipeline behavior applied to all command handlers from modules 3–10).
13. Dashboard.
14. Reports.
15. Platform/Super Admin operations (Company onboarding/suspension, impersonation).

## 20.4 Estimated Complexity per Module

| Module | Complexity | Rationale |
|---|---|---|
| Authentication | High | Security-critical; token rotation, reuse detection, dual identity contexts (§9.7) add real complexity beyond typical CRUD auth. |
| Users | Low–Medium | Mostly CRUD + invitation state machine. |
| Companies | Medium | Tenant root; onboarding orchestration (creates multiple related records atomically). |
| Departments | Low | Simple CRUD with a dependency-check on delete. |
| Job Posts | Medium | State machine (6 states) + approval workflow branching + validation-heavy publish gate. |
| Applications | High | Central entity; duplicate/cooldown logic, snapshot-on-submit document handling, pipeline state machine interaction. |
| CV Management | Medium | File handling, async virus scan integration, immutable snapshot semantics are a subtle but important correctness requirement. |
| Hiring Pipeline | Medium–High | Versioned graph validation (exactly one Initial, ≥1 TerminalPositive), in-flight-application compatibility across versions. |
| Interview Scheduling | High | Overlap-detection concurrency correctness (§7.9) is the hardest correctness problem in the whole system alongside Offers' single-active-offer constraint. |
| Interview Feedback | Medium | Immutability + blind-feedback visibility rules require careful query-layer filtering, but no complex state machine. |
| Offers | High | Full state machine (9 states) with approval branching, auto-transition side effects on Application, and the deliberately-friction-heavy rescission path. |
| Notifications (in-app) | Low–Medium | Mostly a straightforward event-subscriber fan-out; complexity is in getting the trigger matrix (§10.1) complete, not in the mechanics. |
| Email | Medium | Template resolution fallback logic, retry/queue handling, bounce tracking integration points. |
| Dashboard | Medium | Read-only, but requires careful per-role query scoping and reasonable performance at scale. |
| Reports | Medium–High | Aggregation query complexity, async export for large datasets. |
| Settings | Low–Medium | Mostly CRUD, but touches many other modules' behavior (high fan-out risk if changed carelessly). |
| Audit Logs | Medium | Simple schema, but the cross-cutting pipeline-behavior integration and the synchronous-for-critical-actions requirement (§4.17) require careful engineering to avoid becoming a silent single point of failure. |
| Platform/Super Admin | Medium–High | Impersonation flow's security and audit requirements make it deceptively complex despite low endpoint count. |

## 20.5 Risks

1. **Tenant isolation bugs** are the single highest-severity risk category — a leak of Company A's candidate data to Company B is a critical trust and possibly legal failure. Mitigation: redundant enforcement (authorization layer + EF Core global query filter, §9.4) plus mandatory, explicit automated test coverage (§17.4) rather than relying on manual review alone.
2. **Interview overlap race conditions** — the async-validator-plus-transactional-recheck pattern (§7.9) is necessary but must be implemented correctly; a naive implementation that only checks at validation time (not again at commit) will have a real, exploitable double-booking window under concurrent load.
3. **Offer/Application state machine edge cases** — the interplay between Application withdrawal and Offer auto-retraction (§11.1, §12.18), and between Offer acceptance and Application auto-transition to Hired (§12.10), are the kind of cross-aggregate side effects that are easy to get subtly wrong (e.g., forgetting to decrement `PositionsFilled` correctly, or double-firing a notification). Mitigation: dedicated integration tests for every cross-aggregate side-effect path explicitly listed in §12.
4. **Audit log write reliability** — if synchronous audit writes for critical actions are implemented carelessly (e.g., as fire-and-forget), the immutability/completeness guarantee the whole compliance story depends on (§4.17, §12.14) is silently broken. Mitigation: explicit architecture test or code-review checklist item confirming critical-action commands include audit writes within their transaction boundary.
5. **Impersonation misuse or under-logging** — since this is the one deliberate "backdoor" into tenant data, insufficient audit rigor here undermines the entire tenant-privacy story of §2.1/§9.8. Mitigation: impersonation session start/end are the two most heavily audited events in the system by design; consider requiring a second-person approval for impersonation as a post-v1 hardening step if the business's trust model warrants it.
6. **Scope creep on Settings-driven configurability** — because so many behaviors are company-configurable (§4.16), there's a risk of combinatorial complexity in testing (e.g., `RequireJobApproval` × `RequireOfferApproval` × `BlindFeedbackEnabled` × `SelfSchedulingEnabled` interactions). Mitigation: treat each setting's effect as independently testable where the domain logic genuinely is independent (most are), and explicitly document any settings that do interact (none identified as tightly coupled in this spec, but future settings additions should be evaluated for this risk before being added).
7. **Underestimating the reporting/aggregation performance work** — Reports (§4.15) and Dashboards (§4.14) are deceptively simple to describe but can become the actual performance bottleneck at scale if implemented as naive on-the-fly joins over growing transactional tables. Mitigation: NFR §13.1/§13.4 already flag read-model/materialized-view projection as an accepted, planned escape hatch — don't wait for a production incident to consider it.

## 20.6 Notes for the Developer / AI Coding Agent

- This document is the **single source of truth** for scope and behavior. Where an implementation detail is ambiguous in code but this document specifies a rule, this document wins — raise a clarifying note in code comments rather than silently deviating.
- Build **vertical slices**, not horizontal layers-first-then-features. Even though §19's roadmap groups work by phase, within each phase prefer implementing one module fully (Domain → Persistence → Application → API → tests) before starting the next, rather than building all Domain entities for every module before touching any Application logic. This keeps the system demonstrably working end-to-end at every step, per the recommended order in §20.3.
- **Every business rule in §12 should map to at least one line of enforcement code and at least one automated test** — treat that section as a literal acceptance-criteria checklist, not narrative background.
- When in doubt about a role's access to a piece of data, default to the **more restrictive** interpretation and revisit — this system handles sensitive personal and compensation data, and over-restriction is a much cheaper mistake to fix than a data leak.
- The candidate-facing vs. internal-facing DTO separation (§7, §11.3) is not optional polish — it is a data-leakage control. Every new field added to an entity during implementation should be explicitly evaluated for whether it belongs in candidate-facing DTOs, defaulting to **excluded** unless there's a clear reason to include it.
- Do not implement any of the §18 Future Improvements in v1 scope, even if they seem easy to bolt on early — the architecture is deliberately shaped to make them additive later; implementing them prematurely risks coupling core v1 modules to speculative future requirements.

---

*End of document.*
