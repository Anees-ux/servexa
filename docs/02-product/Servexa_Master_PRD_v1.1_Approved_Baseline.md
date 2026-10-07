**SERVEXA**

**Master Product Requirements Document**

*Enterprise Field Service Operations Platform*

| **Purpose**                                                                                                                                                                                                                                                                                                               |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Translate the frozen Servexa domain model into a buildable product contract: outcomes, release scope, user capabilities, functional behavior, business rules, acceptance signals, non-functional requirements and delivery gates. This PRD intentionally stops before database schema, API shape and React screen design. |

| **Item**                  | **Value**                                                                                                                                              |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------|
| Version                   | 1.1 Approved Baseline                                                                                                                                          |
| Product                   | Servexa                                                                                                                                                |
| Product class             | Enterprise Field Service Management / Service Operations                                                                                               |
| Source baseline           | Servexa Master Domain Discovery v1.0                                                                                                                   |
| Reference operating model | Prime Technical Services                                                                                                                               |
| Delivery philosophy       | Vertical slices; modular monolith first; offline-capable field operations from R1; deterministic business rules; AI advisory                           |
| Status                    | APPROVED BASELINE - independent closure review passed; blockers resolved with minor editorial corrections applied                                                      |
| Review basis              | v1.0 independent baseline review disposition incorporated: blockers accepted/modified where justified; non-essential scope expansion rejected/deferred |

# Table of Contents

- 1\. Executive Product Contract

- 2\. Product Outcomes, Goals and Non-Goals

- 3\. Users, Jobs and Operating Context

- 4\. Product Principles and Requirement Language

- 5\. Release Strategy and Scope Model

- 6\. Cross-Cutting Platform Requirements

- 7\. Customer, Account and Site

- 8\. Asset Management

- 9\. Service Request and Work Order

- 10\. Scheduling, Dispatch and Workforce

- 11\. Field Execution, Forms and Offline Mobile

- 12\. Inventory, Parts and Procurement

- 13\. Agreements, Entitlements, Warranty, SLA and PM

- 14\. Pricebook, Estimates, Billing, Payments and Job Costing

- 15\. Customer Portal, Communication and Notifications

- 16\. AI, RAG and Enterprise Knowledge

- 17\. Reporting, Analytics, Automation and Integrations

- 18\. Security, Tenancy, Audit and Governance

- 19\. Non-Functional Requirements

- 20\. Release 1 Vertical Slice Acceptance

- 21\. Deferred Scope and Anti-Overengineering Guardrails

- 22\. Traceability, Product Metrics and Quality Gates

- 23\. Open Decisions and Validation Plan

- 24\. PRD Exit Criteria and Handoff

- Appendix A. Requirement Priority Model

- Appendix B. Canonical Product States

- Appendix C. Acceptance Scenarios

- Appendix D. v1.1 Baseline Review Disposition

# 1. Executive Product Contract

| **Product promise**                                                                                                                                                                                                                                                                                                             |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Servexa gives a service organization one trustworthy operational system from customer demand through asset-centric service execution, scheduling, parts, entitlement, invoicing, payment, customer communication and future maintenance - without collapsing operational, inventory and financial facts into one brittle model. |

## 1.1 Product definition

Servexa is a multi-tenant field-service operations platform for organizations whose technicians install, inspect, maintain and repair equipment at customer locations. It is asset-centric, workflow-complete, configurable by policy, and designed for unreliable field connectivity.

## 1.2 Canonical lifecycle

| **Core lifecycle**                                                                                                                                                                                                                                                                                     |
|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Account -\> Site -\> Asset -\> Service Request -\> Work Order -\> Resource Requirement -\> Booking -\> Resource Assignment -\> Field Execution -\> Part Usage -\> Operational Completion -\> Charge Calculation -\> Invoice -\> Payment -\> Financial Closure -\> Asset History -\> Future Maintenance |

## 1.3 Product authority model

| **Question**                                        | **Authoritative capability**                 |
|-----------------------------------------------------|----------------------------------------------|
| Who is the customer and where is service delivered? | Customer & Account                           |
| What physical equipment exists?                     | Asset Management                             |
| What work is authorized?                            | Service Management                           |
| Who performs it and when?                           | Scheduling & Workforce                       |
| What happened onsite?                               | Field Execution                              |
| Where is stock physically?                          | Inventory                                    |
| What is covered?                                    | Contracts / Entitlement / Warranty           |
| How fast was service promised?                      | SLA                                          |
| What work is due next?                              | Preventive Maintenance                       |
| What should customer pay?                           | Commercial & Billing                         |
| What did work cost?                                 | Job Costing                                  |
| Was money received?                                 | Payments                                     |
| What can AI assert?                                 | Only cited source data / deterministic tools |
| What KPI is official?                               | Governed Analytics definition                |

# 2. Product Outcomes, Goals and Non-Goals

## 2.1 Outcomes

| **Outcome**               | **Measure of success**                                                                                                   |
|---------------------------|--------------------------------------------------------------------------------------------------------------------------|
| Operational control       | A service request can be traced end-to-end through completion and financial closure without spreadsheet handoffs.        |
| Dispatcher confidence     | Eligible resources, conflicts, SLA risk and travel are explainable before booking.                                       |
| Field productivity        | Technicians can execute assignments, capture evidence and sync after connectivity loss without losing operational truth. |
| Asset intelligence        | Every asset has reconstructable service/component/warranty history.                                                      |
| Inventory integrity       | Physical custody, reservation and usage are auditable; customer billing never determines stock truth.                    |
| Commercial explainability | Every charge can be explained from actual usage, price rules and entitlement.                                            |
| Customer self-service     | Customers can request, track, approve and receive service artifacts without access to internal-only data.                |
| AI usefulness             | AI answers are permission-filtered, cited and advisory; deterministic facts stay authoritative.                          |

## 2.2 Product goals

- Provide one coherent field-service operating model across customer, asset, work, people, parts and commercial outcomes.

- Prioritize complete vertical workflows over a large number of shallow modules.

- Keep tenant-specific behavior configurable where it is policy, but keep true invariants fixed.

- Make state changes explicit, auditable and historically reconstructable.

- Enable a credible Release 1 that proves the product spine before advanced optimization, procurement automation or autonomous AI.

## 2.3 Non-goals

- Full general ledger, payroll, manufacturing MRP or enterprise HR suite.

- Autonomous AI that can bypass scheduling, safety, inventory, entitlement, billing or approval rules.

- Premature microservice decomposition or distributed transaction complexity.

- Pixel-for-pixel cloning of competitor products.

- A generic no-code platform before Servexa core workflows are reliable.

- Advanced route optimization before basic scheduling, availability and conflict correctness are proven.

# 3. Users, Jobs and Operating Context

| **Persona**             | **Primary job-to-be-done**                   | **Critical product need**                                                     |
|-------------------------|----------------------------------------------|-------------------------------------------------------------------------------|
| Customer Contact        | Get service and know what is happening       | Fast request, safe slot choices, approvals, status, reports, invoices         |
| Support Agent           | Turn demand into accurate service work       | Account/site/asset context, triage, duplicate awareness, entitlement/SLA cues |
| Dispatcher              | Put the right resource on the right job      | Eligibility, availability, travel, SLA, multi-resource booking, conflicts     |
| Technician              | Execute work correctly onsite                | Offline-ready assignments, history, forms, parts, evidence, completion        |
| Service Manager         | Protect service quality and exceptions       | Escalation, reopen/callback, scope approval, SLA/quality controls             |
| Warehouse / Procurement | Keep parts ready and traceable               | Reservations, transfer, truck stock, PO, RMA, counts                          |
| Account Manager / Sales | Sell and retain service relationships        | Estimates, agreements, renewals, coverage summary                             |
| Finance                 | Turn service into correct financial outcomes | Charges, invoice, payment, credits, financial closure                         |
| Operations Manager      | Run capacity and performance                 | Backlog, utilization, SLA, margin, cross-branch visibility                    |
| System Administrator    | Govern the tenant                            | Users, permissions, policies, integrations, audit, AI configuration           |

## 3.1 Reference operating scenarios

- Reactive repair: hospital generator will not start; triage -\> emergency WO -\> dispatch -\> repair -\> part usage -\> invoice/claim.

- Preventive maintenance: quarterly generator occurrence -\> WO -\> resource/parts readiness -\> inspection -\> next due date.

- Multi-visit job: diagnosis today, part procurement, return visit later under one Work Order.

- Multi-resource booking: one customer appointment with lead technician, two supporting technicians and optional equipment resource.

- Offline execution: technician works in basement, captures forms/parts/signature offline, then syncs with conflict handling.

# 4. Product Principles and Requirement Language

| **Principle**                  | **Product implication**                                                                    |
|--------------------------------|--------------------------------------------------------------------------------------------|
| Semantics before convenience   | Account, Site, Asset, Service Request, Work Order, Booking and Assignment remain distinct. |
| Vertical completeness          | A smaller number of end-to-end capabilities beats dozens of disconnected CRUD screens.     |
| Deterministic core             | Scheduling, stock, SLA, entitlement, payments and permissions use deterministic rules.     |
| Explainability                 | Eligibility, pricing, entitlement, automation and AI outputs expose why.                   |
| Offline as a state             | Mobile sync, command identity, timestamps and conflicts are modeled explicitly.            |
| Policy vs invariant            | Tenant configuration changes policy, never core integrity rules.                           |
| History over overwrite         | Versioning, compensating actions and explicit transitions preserve prior truth.            |
| Recommendation before autonomy | Optimization and AI advise first; high-impact automation has gates.                        |
| Modular monolith first         | Clean domain boundaries without distributed-system tax before scale requires it.           |

## 4.1 Requirement priority

| **Priority** | **Meaning**                                                                       |
|--------------|-----------------------------------------------------------------------------------|
| P0           | Release-blocking core product behavior or integrity requirement.                  |
| P1           | High-value capability required for enterprise credibility soon after core spine.  |
| P2           | Important extension once core operational reliability is proven.                  |
| Deferred     | Explicitly outside current release; architecture must not prevent later addition. |

# 5. Release Strategy and Scope Model

| **Delivery rule**                                                                                                                                                       |
|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Do not implement by module completion. Implement vertical slices that cross UI, domain rules, persistence, audit and tests so the product is usable at every milestone. |

| **Release**                               | **Scope**                                                                                                                                                                                                                                                      | **Proof**                                                                                                                                                                         |
|-------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| R0 - Foundation                           | Tenant, identity, permissions, audit, configuration, core references                                                                                                                                                                                           | Secure tenant can be created and governed.                                                                                                                                        |
| R1 - Core Service Spine                   | Account/Site/Asset, Service Request, Work Order, basic Resource/Booking/Assignment, essential offline-capable field execution for downloaded assignments, basic truck stock/part usage, base pricebook/simple invoice, core concurrency/idempotency safeguards | Request -\> dispatch -\> disconnected/connected service -\> part -\> signature -\> invoice -\> asset history works end-to-end without duplicate effects or silent double booking. |
| R1.5 - Reliability & Operations Hardening | Advanced offline reconciliation tooling, bulk recovery/retry operations, import/migration tools, richer operational reporting, device/admin support, performance and observability hardening                                                                   | Core spine survives broader recovery, migration and operational-support scenarios at realistic scale.                                                                             |
| R2 - Commercial Operations                | Estimates, customer approvals, richer price rules, payments, credits, job costing                                                                                                                                                                              | Operational work produces explainable financial outcomes.                                                                                                                         |
| R3 - Recurring Service                    | Agreements, entitlement, warranty, SLA, preventive maintenance                                                                                                                                                                                                 | Reactive + preventive contract lifecycle works.                                                                                                                                   |
| R4 - Customer & Scale                     | Portal, notifications, multi-branch controls, advanced inventory/procurement                                                                                                                                                                                   | Customer self-service and larger operations supported.                                                                                                                            |
| R5 - Intelligence                         | RAG, governed AI assistants, advanced analytics, automation, optimizer enhancements                                                                                                                                                                            | AI/automation adds value without becoming system of truth.                                                                                                                        |

# 6. Cross-Cutting Platform Requirements

| **ID**  | **Requirement**             | **Product behavior**                                                                                                                                     | **Priority** | **Acceptance signal**                                                                                                                                                                 |
|---------|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| PLT-001 | Tenant isolation            | Every business record and action must resolve within one tenant boundary; cross-tenant relationships are prohibited.                                     | P0           | Automated isolation tests prove a user from Tenant A cannot read or mutate Tenant B data.                                                                                             |
| PLT-002 | Role + scope authorization  | Permissions combine capability (what action) with contextual scope (tenant/branch/site/account/assignment where needed) and are enforced server-side.    | P0           | A user with the action permission but outside the target scope is denied and no business mutation occurs; the denial is auditable.                                                    |
| PLT-003 | Audit timeline              | Critical state changes and overrides must record actor, time, reason and before/after where material.                                                    | P0           | Every sensitive workflow has reconstructable audit history.                                                                                                                           |
| PLT-004 | Configurable policy         | Tenant-specific policies such as confirmations, overtime, thresholds and approval rules must be configurable without code changes.                       | P1           | Two tenants can run different policy settings on same build.                                                                                                                          |
| PLT-005 | Effective dating/versioning | Commercial terms, templates, policies and pricebooks that affect history must support version/effective dates.                                           | P0           | Historical job renders the rules that applied when it occurred.                                                                                                                       |
| PLT-006 | Idempotent commands         | Retry-sensitive operations (payments, offline sync, webhooks, inventory movement) must not duplicate effects.                                            | P0           | Replaying same command yields one business effect.                                                                                                                                    |
| PLT-007 | Concurrency protection      | Conflicting mutable operations must be detected at the business consistency boundary rather than silently overwrite or create contradictory commitments. | P0           | Two concurrent attempts to create overlapping committed assignments for the same exclusive resource cannot both succeed; the rejected attempt is revalidated and shown as a conflict. |
| PLT-008 | Feature flags / rollout     | Risky capabilities and integrations can be enabled per tenant/segment.                                                                                   | P1           | New capability can be piloted without global activation.                                                                                                                              |

# 7. Customer, Account and Site

Provide enterprise-grade customer identity and service-location context without flattening billing entity, human contact, physical site and portal access into one record.

## 7.1 Functional requirements

| **ID**  | **Requirement**                 | **Product behavior**                                                                                         | **Priority** | **Acceptance signal**                                                                                      |
|---------|---------------------------------|--------------------------------------------------------------------------------------------------------------|--------------|------------------------------------------------------------------------------------------------------------|
| CUS-001 | Account hierarchy               | Support legal/billing accounts, parent-child relationships, status, billing terms and commercial references. | P0           | Parent/child account relationships are visible and historically retained.                                  |
| CUS-002 | Contact roles                   | A contact can have multiple roles such as onsite, billing, approver and portal user candidate.               | P0           | Same human can be onsite contact for one site and approver for account.                                    |
| CUS-003 | Site as operational entity      | Site carries address, geolocation, hours, hazards, access instructions, branch/territory and site contacts.  | P0           | Changing postal address does not erase site operational history.                                           |
| CUS-004 | Billing separation              | Service site may bill to a different account according to configured relationship.                           | P1           | WO at Site A can invoice parent account without re-parenting the Site.                                     |
| CUS-005 | Status controls                 | Inactive/closed accounts and sites block or warn on new work according to policy.                            | P1           | Normal work cannot be created for deactivated site unless authorized exception applies.                    |
| CUS-006 | Customer-specific configuration | Allow customer/site preferences, service windows and externally visible notes/documents.                     | P1           | Dispatcher and technician see relevant site instructions; private internal notes remain hidden externally. |

## 7.2 Business rules

- Account, Contact and Site remain distinct concepts.

- Site history survives ownership/billing changes.

- Deletion is avoided for entities with service/financial history; deactivation preserves history.

- Portal identity is not the same as Contact.

## 7.3 Permission / visibility rules

- Customer Support can create/maintain operational customer details but cannot change financial controls unless permitted.

- Finance controls credit hold/billing terms; portal users only see authorized account/site scope.

## 7.5 Required edge-case behavior

- Corporate parent pays for franchised location.

- Site moves or changes owner but physical asset history must remain.

- Customer contact leaves company; access is revoked without deleting historical approvals.

# 8. Asset Management

Make installed equipment a first-class lifecycle entity so every service interaction, component replacement, warranty and maintenance decision has a stable physical reference.

## 8.1 Functional requirements

| **ID**  | **Requirement**         | **Product behavior**                                                                                               | **Priority** | **Acceptance signal**                                                                                 |
|---------|-------------------------|--------------------------------------------------------------------------------------------------------------------|--------------|-------------------------------------------------------------------------------------------------------|
| ASS-001 | Equipment model catalog | Maintain manufacturer/model templates, manuals, default maintenance metadata and compatible parts references.      | P0           | Multiple assets can reference one equipment model without sharing lifecycle state.                    |
| ASS-002 | Asset registration      | Register serialized/non-serialized installed assets with site, ownership, install/commission data and status.      | P0           | Duplicate serial policy is enforced within applicable manufacturer/model scope.                       |
| ASS-003 | Component hierarchy     | Support parent/child asset components and component replacement history.                                           | P1           | Replacing a compressor preserves old component and installs new child identity.                       |
| ASS-004 | Asset movement          | Move asset between sites using explicit lifecycle action with historical location trace.                           | P1           | Past WOs remain tied to historical site context.                                                      |
| ASS-005 | Asset lifecycle         | Support pre-installation/active/degraded/decommissioned/replaced states.                                           | P0           | Decommissioned assets stop normal PM generation and require authorized reactivation/replacement flow. |
| ASS-006 | Asset service timeline  | Expose service requests, WOs, readings, inspections, parts/component changes and warranties in one asset timeline. | P0           | Technician can review prior relevant history before execution.                                        |

## 8.2 Business rules

- Asset is distinct from Equipment Model and Inventory Product.

- Component replacement never rewrites prior component history.

- Asset movement is blocked or controlled while incompatible active work exists.

- Asset decommissioning is an authorized lifecycle action, not free-form technician edit.

## 8.4 Key product events

- AssetRegistered

- AssetCommissioned

- AssetMoved

- ComponentReplaced

- AssetDecommissioned

## 8.5 Required edge-case behavior

- Leasing company owns asset installed at customer site.

- Asset identity remains stable when site ownership changes.

- Serialized inventory unit becomes installed asset/component on installation.

# 9. Service Request and Work Order

Turn incoming demand into authorized work while preserving triage, scope, state, callbacks, multiple visits and the distinction between operational and financial completion.

## 9.1 Functional requirements

| **ID**  | **Requirement**                  | **Product behavior**                                                                                                                                                                                                     | **Priority** | **Acceptance signal**                                                                                                                       |
|---------|----------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| SER-001 | Multi-channel request capture    | Create Service Request from staff, portal, API/monitoring or preventive process with account/site/asset context and evidence.                                                                                            | P0           | Every request has source, requester, created time and triage status.                                                                        |
| SER-002 | Triage                           | Support resolve-without-field-work, duplicate/merge reference, priority assignment and WO creation.                                                                                                                      | P0           | A request may close without WO; duplicate keeps trace to primary request.                                                                   |
| SER-003 | WO authorization                 | Create Work Order with site, asset(s), scope, service type, priority, SLA/entitlement snapshots and resource/part requirements.                                                                                          | P0           | WO cannot exist without valid tenant/site context and authorized scope.                                                                     |
| SER-004 | WO state model                   | Support Draft/Approved/Scheduled/In Progress/Paused/Operationally Complete/Cancelled and explicit reopen/callback rules.                                                                                                 | P0           | Illegal transitions are rejected and history preserved.                                                                                     |
| SER-005 | Multi-visit                      | One WO may require many Bookings across days/resources. Completing a Booking closes that visit only; the WO is then evaluated for follow-up, pause or overall completion.                                                | P0           | Diagnosis visit can complete while WO remains Paused/Awaiting Parts and later accepts a second Booking.                                     |
| SER-006 | Additional scope                 | Field-discovered additional work creates approval/estimate path rather than silently mutating approved scope.                                                                                                            | P1           | Rejected extra work is recorded while original scope may complete.                                                                          |
| SER-007 | Operational vs financial closure | Operational completion occurs only after authorized scope and completion gates are satisfied; financial closure occurs later after commercial review. Assignment/Booking completion never silently forces WO completion. | P0           | A completed visit can leave the WO Paused/Follow-up Required; only a completion evaluation can transition the WO to Operationally Complete. |

## 9.2 Business rules

- Service Request is unverified demand; Work Order is authorized work.

- Closed history is not silently rewritten.

- Work Order may cover multiple assets only when same-site/business policy allows.

- Financial Closure cannot precede Operational Completion except explicit cancellation/no-service cases.

## 9.4 Key product events

- ServiceRequestCreated

- ServiceRequestTriaged

- WorkOrderCreated

- WorkOrderApproved

- WorkOrderPaused

- WorkOrderOperationallyCompleted

- WorkOrderReopened

- CallbackWorkOrderCreated

## 9.5 Required edge-case behavior

- Wrong asset reported.

- Customer cancels after dispatch.

- Issue returns next day after financially closed job - create callback rather than rewriting history.

# 10. Scheduling, Dispatch and Workforce

Translate Work Order resource demand into qualified, available and explainable capacity while separating eligibility, optimization, booking and individual participation.

## 10.1 Functional requirements

| **ID**  | **Requirement**                   | **Product behavior**                                                                                                                                                                                                                                           | **Priority** | **Acceptance signal**                                                                                                                                                                 |
|---------|-----------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| SCH-001 | Resource master                   | Represent human technicians, subcontractors, named crews and optional schedulable equipment separately from user accounts.                                                                                                                                     | P0           | Non-login subcontractor resource can be scheduled; finance user cannot.                                                                                                               |
| SCH-002 | Requirements                      | WO can declare role, quantity, duration, required/preferred skills, certifications and equipment needs.                                                                                                                                                        | P0           | Requirement exists before actual person is chosen.                                                                                                                                    |
| SCH-003 | Availability calendar             | Calculate shifts, breaks, leave, existing bookings, travel, on-call and overtime.                                                                                                                                                                              | P0           | Free/busy result changes correctly after leave or booking update.                                                                                                                     |
| SCH-004 | Eligibility engine                | Filter by active status, tenant, hard certification, capacity, mandatory territory/site rules and impossible overlap. Eligibility is revalidated when committing or rescheduling an assignment so concurrent schedulers cannot create conflicting commitments. | P0           | If two dispatchers concurrently target overlapping time for the same exclusive resource, at most one committed assignment succeeds and the other returns a visible schedule conflict. |
| SCH-005 | Candidate scoring                 | Rank eligible resources using configurable travel, skill depth, continuity, overtime, utilization and customer preference.                                                                                                                                     | P1           | Score explanation shows positive/negative factors.                                                                                                                                    |
| SCH-006 | Booking                           | Create customer visit window for one WO; support proposed/confirmed/pinned/rescheduled/cancelled lifecycle.                                                                                                                                                    | P0           | Booking history survives reschedule.                                                                                                                                                  |
| SCH-007 | Assignments                       | One Booking supports many Resource Assignments with lead role and individual participation times.                                                                                                                                                              | P0           | Three-person visit appears as one customer appointment and three assignments.                                                                                                         |
| SCH-008 | Dispatch/progress                 | Track dispatch, travel, arrival and work-start statuses without conflating them with WO lifecycle.                                                                                                                                                             | P0           | Dispatcher sees planned vs actual status.                                                                                                                                             |
| SCH-009 | Cross-branch override             | Allow authorized cross-branch assignment with audit/cost attribution.                                                                                                                                                                                          | P1           | Local dispatcher cannot silently use another branch resource.                                                                                                                         |
| SCH-010 | Recommendation-first optimization | System recommends schedule changes; automatic movement of pinned/confirmed work is blocked or gated.                                                                                                                                                           | P2           | Optimizer cannot silently move protected booking.                                                                                                                                     |

## 10.2 Business rules

- Eligibility and scoring are distinct stages.

- Skills and Certifications are distinct.

- Branch and Territory are distinct.

- Travel consumes capacity where route data is available.

- Same human resource cannot hold incompatible overlapping committed assignments.

- Pinned bookings are not automatically moved.

## 10.3 Permission / visibility rules

- Dispatcher schedules within scope; Regional/Operations roles can cross branch and override selected constraints.

- Technician can only act on assigned/authorized bookings.

## 10.5 Required edge-case behavior

- Technician sick before shift.

- Job overrun threatens later SLA.

- Offline technician unaware of cancellation.

- Certification expires between scheduling and execution.

# 11. Field Execution, Forms and Offline Mobile

Give technicians a task-oriented, evidence-driven mobile workflow that remains trustworthy under poor connectivity and supports individual participation, inspections, parts and customer acceptance.

## 11.1 Functional requirements

| **ID**  | **Requirement**           | **Product behavior**                                                                                                                                                                                                                                        | **Priority** | **Acceptance signal**                                                                                                                                                                                  |
|---------|---------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| FIE-001 | Pre-job package           | Provide assignment, site instructions, asset history, work scope, forms, likely parts and authorized knowledge needed for upcoming jobs.                                                                                                                    | P0           | Technician can open required job package before going offline.                                                                                                                                         |
| FIE-002 | Execution session         | Track per-assignment travel, arrival, work start/pause/resume/end and labour intervals.                                                                                                                                                                     | P0           | Individual technician truth is independent from booking envelope.                                                                                                                                      |
| FIE-003 | Task/checklist/inspection | Support distinct tasks, checklists and typed inspections with conditional logic and validation.                                                                                                                                                             | P0           | Required inspection can gate completion.                                                                                                                                                               |
| FIE-004 | Versioned forms           | Published Form Template versions remain tied to historical Form Responses.                                                                                                                                                                                  | P0           | Template edit never changes old response rendering.                                                                                                                                                    |
| FIE-005 | Diagnosis model           | Capture symptom, diagnosis, root cause confidence and resolution separately.                                                                                                                                                                                | P1           | Reported symptom remains intact after diagnosis is added.                                                                                                                                              |
| FIE-006 | Evidence                  | Capture photos, readings, barcode/QR and signatures with operational context.                                                                                                                                                                               | P0           | Evidence can be traced to WO/booking/asset/task and author/time.                                                                                                                                       |
| FIE-007 | Offline commands          | Mutations created offline carry idempotency identity, local timestamp and sync status.                                                                                                                                                                      | P0           | Retry after reconnection does not duplicate part usage or completion.                                                                                                                                  |
| FIE-008 | Offline conflict handling | Server/device conflicts create explicit reconciliation. Field evidence is preserved with device-observed time and original context; contradictory server state is not silently overwritten.                                                                 | P0           | If a Booking is cancelled on the server while a technician works offline, captured evidence is persisted, the cancelled Booking is not silently reopened, and an office reconciliation item is raised. |
| FIE-009 | Completion gates          | Completion follows Assignment -\> Booking -\> Work Order evaluation. Mandatory tasks/evidence gate the relevant level; after a Booking completes, Servexa explicitly decides whether the WO is Operationally Complete or remains Paused/Follow-up Required. | P0           | Completing one technician assignment in a multi-resource or multi-visit job cannot automatically close the WO while required work remains.                                                             |
| FIE-010 | AI-assisted notes         | AI may draft summaries/structured notes, but technician confirms before save.                                                                                                                                                                               | P2           | No AI-generated operational update is silently committed.                                                                                                                                              |
| FIE-011 | Multi-asset attribution   | When one Work Order covers multiple Assets, tasks, inspection responses, readings, evidence and part usage that relate to a specific Asset must retain that Asset reference within the WO asset set.                                                        | P0           | For a three-generator WO, a reading or installed part recorded for Generator B appears in Generator B history and not in A/C history.                                                                  |

## 11.2 Business rules

- Field app is not a smaller office UI.

- Customer signature proves captured acceptance/evidence but does not erase dispute rights.

- Offline sync preserves device-observed and server-received time.

- Assignment completion, Booking completion and WO operational completion may occur at different moments.

## 11.5 Required edge-case behavior

- Form template updates while technician is offline.

- Inventory stock conflict on offline part usage.

- Resource reassigned while old technician is offline.

# 12. Inventory, Parts and Procurement

Control physical custody and parts readiness without confusing stock movement, operational usage, customer charge or job cost.

## 12.1 Functional requirements

| **ID**  | **Requirement**              | **Product behavior**                                                                                                                                                                                                                            | **Priority** | **Acceptance signal**                                                                                                                                                            |
|---------|------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| INV-001 | Product master               | Maintain SKU/product tracking policy, UOM, serialization/lot behavior, substitutes and compatible model references.                                                                                                                             | P0           | Inventory-tracked and non-stock products behave differently.                                                                                                                     |
| INV-002 | Inventory locations          | Support warehouse, truck/van, site store, transit and consignment locations.                                                                                                                                                                    | P0           | Stock is attributed to location, not user account.                                                                                                                               |
| INV-003 | Stock balances               | Expose On Hand, Reserved, Available, On Order, In Transit and Quarantined.                                                                                                                                                                      | P0           | Reservation changes Available but not On Hand.                                                                                                                                   |
| INV-004 | Reservations                 | Reserve/partially reserve stock for WO/booking requirements and release on cancellation.                                                                                                                                                        | P1           | Last unit cannot satisfy two exclusive reservations.                                                                                                                             |
| INV-005 | Transfers                    | Move stock with source/destination, optional in-transit state and receipt variance.                                                                                                                                                             | P1           | Sent 10 / received 9 creates variance, not silent equality.                                                                                                                      |
| INV-006 | Part usage                   | Record physical consumption linked to WO/booking/asset and source inventory location, including offline-originated usage.                                                                                                                       | P0           | Physical consumption reduces/records stock reality regardless of customer charge; a retry cannot duplicate the movement.                                                         |
| INV-007 | Serialized lifecycle         | Track individual serial from receipt through truck, installation, removal, quarantine/RMA.                                                                                                                                                      | P1           | Same serial cannot be available and installed simultaneously.                                                                                                                    |
| INV-008 | Counts/adjustments           | Stock changes outside normal flow use explicit adjustment with reason/approval.                                                                                                                                                                 | P1           | User cannot directly edit quantity without auditable movement.                                                                                                                   |
| INV-009 | Purchasing                   | Support requisition, PO, approval, partial receipt and direct-to-job procurement.                                                                                                                                                               | P2           | Partial PO receipt updates only received quantity.                                                                                                                               |
| INV-010 | Returns/RMA                  | Support defect reporting, quarantine, inspection, return-to-stock/vendor/scrap disposition.                                                                                                                                                     | P2           | Defective returned item does not immediately become Available.                                                                                                                   |
| INV-011 | Offline stock reconciliation | If valid offline physical usage synchronizes against insufficient server stock, Servexa preserves the usage and creates an explicit inventory reconciliation exception according to negative-stock policy rather than discarding field reality. | P0           | Server shows zero truck stock, technician syncs one verified offline consumption: usage/evidence remain recorded exactly once and warehouse receives a reconciliation exception. |

## 12.2 Business rules

- Physical stock movement != operational usage != customer charge != job cost.

- Reservation is not consumption.

- Warranty/discount never reverses genuine physical consumption.

- Negative stock behavior is policy-controlled; offline field reality may create reconciliation exception.

- Ownership is separate from physical location.

## 12.5 Required edge-case behavior

- Customer-supplied part.

- Defective part immediately replaced by second part.

- Two users reserve last serial concurrently.

- Truck inventory syncs after offline usage.

# 13. Agreements, Entitlements, Warranty, SLA and Preventive Maintenance

Model the customer service promise as effective-dated, explainable rights and obligations rather than a single "covered" flag.

## 13.1 Functional requirements

| **ID**  | **Requirement**             | **Product behavior**                                                                                                                                                          | **Priority** | **Acceptance signal**                                                                                                                               |
|---------|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| AGR-001 | Service agreements          | Create effective-dated, versioned commercial agreement with covered scope, plans, billing terms and lifecycle.                                                                | P1           | Amendment changes future decisions without rewriting prior jobs.                                                                                    |
| AGR-002 | Service plans               | Support reusable plan templates and customer-specific adopted versions.                                                                                                       | P1           | Editing generic template does not silently alter signed agreement.                                                                                  |
| AGR-003 | Entitlement evaluation      | Evaluate coverage per labour/parts/travel/fees/allowances using current context and preserve snapshot.                                                                        | P1           | WO shows why each category is covered, discounted or chargeable.                                                                                    |
| AGR-004 | Allowance usage             | Track visit/hour/value allowances with concurrency-safe consumption.                                                                                                          | P2           | Last free visit cannot be consumed by two jobs.                                                                                                     |
| AGR-005 | Warranty                    | Register manufacturer/internal warranty, effective period and incident-level coverage decision.                                                                               | P1           | Active warranty may still yield denied incident with reason.                                                                                        |
| AGR-006 | Warranty claim              | Track manufacturer recovery separately from customer WO completion.                                                                                                           | P2           | Customer job can close while vendor claim remains open.                                                                                             |
| AGR-007 | SLA milestones              | Support response/dispatch/arrival/restoration/resolution targets with business calendar.                                                                                      | P1           | Deadline calculation is reproducible from policy snapshot.                                                                                          |
| AGR-008 | SLA pause/escalation        | Allowed pause reasons and at-risk/breach escalation are explicit and audited.                                                                                                 | P1           | Internal staff shortage does not pause unless policy explicitly allows.                                                                             |
| AGR-009 | Maintenance plans           | Define time/meter/condition recurrence for covered assets/site groups.                                                                                                        | P1           | Plan generates occurrence, not direct technician booking.                                                                                           |
| AGR-010 | Occurrence generation       | Generate idempotent PM occurrences/WOs within configurable horizon; support fixed vs rolling recurrence.                                                                      | P1           | Running generator twice does not duplicate same occurrence.                                                                                         |
| AGR-011 | Recurring agreement billing | Active agreements with recurring fees produce idempotent billing occurrences/batches independently from Work Order billing, according to effective term and billing schedule. | P1           | A monthly retainer generates one due billing occurrence per period even when no Work Order is billable; rerunning generation does not duplicate it. |

## 13.2 Business rules

- Agreement, Service Plan, Entitlement, Warranty, SLA and Maintenance Plan remain distinct.

- Historical entitlement and SLA snapshots survive later contract edits.

- Maintenance Plan defines when; Template defines what; Work Order represents actual authorized execution.

- SLA sets obligation; Scheduling chooses resource.

## 13.5 Required edge-case behavior

- Overlapping agreements with precedence/stacking policy.

- Warranty active but misuse excluded.

- Agreement expires while WO open.

- PM completed late under fixed vs rolling recurrence.

# 14. Pricebook, Estimates, Billing, Payments and Job Costing

Transform service into explainable commercial outcomes while keeping customer price, cash and internal fulfillment cost as separate facts.

## 14.1 Functional requirements

| **ID**   | **Requirement**         | **Product behavior**                                                                                                                                                                                                                          | **Priority** | **Acceptance signal**                                                                                                                       |
|----------|-------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|---------------------------------------------------------------------------------------------------------------------------------------------|
| PRI-001A | Base pricebook          | Provide tenant-default sell prices for inventory products plus simple labour/service rates sufficient for deterministic R1 charge calculation.                                                                                                | P0           | R1 invoice lines resolve a configured base part/labour price without hardcoded Product/User price fields.                                   |
| PRI-001B | Advanced pricebooks     | Support effective-dated pricing by customer/segment/territory/currency plus flat rates, minimums, surcharges and overrides.                                                                                                                   | P1           | Historical advanced price resolution is reproducible from the effective pricebook/version.                                                  |
| PRI-002  | Estimate/quote          | Create versioned scope/price proposal with internal approval, presentation, acceptance/rejection/expiry.                                                                                                                                      | P1           | Presented estimate cannot be silently overwritten.                                                                                          |
| PRI-003  | Change estimate         | Additional field scope creates new approval path and preserves accepted baseline.                                                                                                                                                             | P1           | Customer can reject extra work while original WO continues.                                                                                 |
| PRI-004  | Charge calculation      | Calculate deterministic lines from actual usage, base pricing, entitlement, approved discounts/surcharges and tax reference.                                                                                                                  | P0           | Every line explains source usage + pricing rule + entitlement.                                                                              |
| PRI-005  | Invoice lifecycle       | Draft invoices may be recalculated or cancelled/voided. Once Posted/Confirmed, invoice financial facts are immutable; corrections use explicit Credit Note or formal Credit-and-Rebill compensating records referencing the original invoice. | P0           | Attempting to alter a posted invoice is rejected; an approved correction creates a linked compensating document and preserves the original. |
| PRI-006  | Payments                | Record payments independently and allocate one payment across one/many invoices.                                                                                                                                                              | P1           | Unapplied payment can exist before allocation.                                                                                              |
| PRI-007  | Refunds/credits         | Separate accounting credit from money refund.                                                                                                                                                                                                 | P1           | Credit note does not imply cash refund until refund recorded.                                                                               |
| PRI-008  | Job costing             | Capture labour, inventory, travel, subcontract/equipment and recovery costs independently of customer price.                                                                                                                                  | P1           | Warranty-covered zero invoice still shows real job cost.                                                                                    |
| PRI-009  | Agreement profitability | Combine recurring agreement revenue with service costs for contract profitability.                                                                                                                                                            | P2           | Zero-billed PM visit contributes cost to agreement margin.                                                                                  |
| PRI-010  | Financial closure       | WO can financially close only when configured billing/costing blockers are resolved, including required invoice posting/accepted accounting handoff and reconciliation blockers.                                                              | P0           | Operationally complete WO with a draft/unresolved invoice or blocking reconciliation remains financially open and shows the blockers.       |

## 14.2 Business rules

- Estimate != Work Order.

- Customer charge != internal job cost.

- Payment != invoice.

- Posted invoice history is corrected by compensating actions.

- Servexa is an operational billing/subledger, not full GL.

## 14.5 Required edge-case behavior

- Tax rules change after estimate but before invoice.

- Payment arrives before invoice.

- Warranty recovery later denied by manufacturer.

- Customer disputes invoice without undoing service history.

# 15. Customer Portal, Communication and Notifications

Expose a safe external experience for service request, scheduling, approvals, reports and finance without leaking internal-only data or raw operational models.

## 15.1 Functional requirements

| **ID**  | **Requirement**                         | **Product behavior**                                                                                                                                                                                                                                            | **Priority** | **Acceptance signal**                                                                                                       |
|---------|-----------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|-----------------------------------------------------------------------------------------------------------------------------|
| POR-001 | Portal identity                         | Provision portal identity linked to Contact with explicit account/site scope and revocable access.                                                                                                                                                              | P2           | Removing login does not delete Contact/history.                                                                             |
| POR-002 | Request self-service                    | Customer can create/track Service Requests and upload evidence.                                                                                                                                                                                                 | P2           | Request is automatically tenant/account/site scoped.                                                                        |
| POR-003 | Safe scheduling                         | Expose validated slot options from Scheduling, optionally with temporary hold.                                                                                                                                                                                  | P2           | Customer never sees raw technician calendars.                                                                               |
| POR-004 | Approvals                               | Customer can approve/reject estimates and additional-work requests with evidence/time.                                                                                                                                                                          | P2           | Approval feeds authorized scope without exposing internal cost.                                                             |
| POR-005 | Service visibility & report publication | Show booking window, policy-limited technician status, customer-visible asset history and a curated service report compiled only from approved externally visible evidence. Publication may be automatic by configured rules or require service-manager review. | P2           | Internal notes/margins never appear in the report; a work type configured for manager review cannot publish until approved. |
| POR-006 | Financial visibility                    | Show invoices, balances, payment state and approved documents.                                                                                                                                                                                                  | P2           | User with site-only role cannot see other account invoices.                                                                 |
| POR-007 | Notifications                           | Event-driven notifications use versioned templates, preferences, delivery attempts and idempotent retry.                                                                                                                                                        | P1           | Retry failure does not create duplicate SMS/email storms.                                                                   |
| POR-008 | Communication threads                   | Link inbound/outbound messages to operational context.                                                                                                                                                                                                          | P2           | Customer reply remains associated with Request/WO/Invoice thread.                                                           |

## 15.2 Business rules

- Portal is curated projection, not back-office UI.

- Internal content is private unless explicitly externally visible.

- Provider delivery != customer read/acknowledgement.

- Real-time technician location is policy-limited to relevant service window.

## 15.5 Required edge-case behavior

- Customer belongs to multiple sites with different roles.

- Notification provider retries after timeout.

- Customer reschedules while slot is being held by another session.

# 16. AI, RAG and Enterprise Knowledge

Add permission-aware retrieval and assistive intelligence without allowing the model to become an ungoverned source of operational truth.

## 16.1 Functional requirements

| **ID** | **Requirement**                      | **Product behavior**                                                                                                                                                                                                         | **Priority** | **Acceptance signal**                                                                                                                           |
|--------|--------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|-------------------------------------------------------------------------------------------------------------------------------------------------|
| AI-001 | Knowledge ingestion                  | Ingest manuals, SOPs, customer documents and selected operational history with metadata/version/security classification.                                                                                                     | P2           | Document change creates new indexed version; expired version remains historical.                                                                |
| AI-002 | Permission-filtered retrieval        | Apply tenant/account/site/asset/document permissions before context reaches model.                                                                                                                                           | P2           | Before any AI retrieval feature is enabled, unauthorized chunks are excluded before prompt/context assembly.                                    |
| AI-003 | Cited answers                        | Document-based answers include source document/page/section or record provenance.                                                                                                                                            | P1           | Technician can inspect evidence used by answer.                                                                                                 |
| AI-004 | Technician troubleshooting assistant | Retrieve relevant manuals + asset history + similar jobs and propose checks.                                                                                                                                                 | P2           | Answer is advisory; technician records diagnosis.                                                                                               |
| AI-005 | Pre-job/work summary                 | Summarize relevant site/asset/work history from authorized facts.                                                                                                                                                            | P2           | Summary cannot modify Work Order state.                                                                                                         |
| AI-006 | Draft assistance                     | Draft notes, inspection templates, customer messages or estimate narrative with human review.                                                                                                                                | P2           | Generated content remains Draft until approved.                                                                                                 |
| AI-007 | Deterministic tool access            | AI queries structured truth (stock, schedule, SLA, payments) through tools, not vector guessing.                                                                                                                             | P1           | Same schedule query returns deterministic engine result.                                                                                        |
| AI-008 | Human-confirmed AI-assisted actions  | AI may propose or prepare an action, but business state changes execute only as a standard authorized command after explicit human confirmation and deterministic domain validation; AI has no direct database/state bypass. | P2           | An AI-proposed schedule/inventory/billing change cannot mutate state until an authorized user confirms and the normal command validations pass. |
| AI-009 | Provider abstraction                 | LLM, embedding and retrieval providers sit behind interfaces.                                                                                                                                                                | P1           | Changing provider does not change business-domain code.                                                                                         |
| AI-010 | Evaluation/governance                | Version prompts/tool schemas/model metadata and maintain representative evaluation/feedback sets.                                                                                                                            | P2           | Release gate includes regression set for supported AI use cases.                                                                                |

## 16.2 Business rules

- AI can retrieve, summarize, suggest and explain; deterministic domain engines remain authoritative.

- Permission filtering happens before retrieval.

- Prompt injection in uploaded documents is treated as untrusted content.

- Tenant data is not implicitly used to train/fine-tune models.

## 16.5 Required edge-case behavior

- Obsolete manual ranked higher than current version.

- Prompt injection embedded in PDF.

- Cross-tenant document metadata error.

- AI recommends incompatible part despite deterministic compatibility data.

# 17. Reporting, Analytics, Automation and Integrations

Provide governed operational insight and safe extensibility while keeping transactional correctness independent from reports and external-provider quirks.

## 17.1 Functional requirements

| **ID**  | **Requirement**          | **Product behavior**                                                                                                                                                                                                                                                  | **Priority** | **Acceptance signal**                                                                                                   |
|---------|--------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|-------------------------------------------------------------------------------------------------------------------------|
| REP-001 | Governed KPIs            | Define KPI formula, scope, exclusions and version.                                                                                                                                                                                                                    | P1           | FTF rate is reproducible from published metric definition.                                                              |
| REP-002 | Operational dashboards   | Expose backlog, SLA, utilization, execution, inventory, commercial and finance views by authorized scope.                                                                                                                                                             | P1           | Dashboard values reconcile to source domain facts.                                                                      |
| REP-003 | Analytical projections   | Use read models/projections instead of distorting transactional aggregates for dashboards.                                                                                                                                                                            | P1           | Changing report shape does not require rewriting core aggregate model.                                                  |
| REP-004 | Domain automations first | Ship concrete event/time-driven automations for proven FSM needs (for example SLA escalation, PM generation, agreement expiry and low-stock actions). A generic tenant-configurable Trigger-Condition-Action designer is deferred until repeated demand justifies it. | P2           | A concrete automation run records trigger, result and retry history without requiring a generic no-code rules platform. |
| REP-005 | Automation safety        | All automations are idempotent, auditable and policy-gated; high-impact actions require approval. Advanced dry-run/loop/rate-control features are added only where configurable automation scope requires them.                                                       | P2           | Repeated trigger cannot duplicate a protected action and high-impact action cannot bypass approval.                     |
| REP-006 | Integration adapters     | External providers map through integration/anti-corruption layer.                                                                                                                                                                                                     | P1           | Domain code contains no provider-specific assumptions.                                                                  |
| REP-007 | System-of-record mapping | Each integration defines authoritative owner per data domain.                                                                                                                                                                                                         | P0           | Bidirectional sync does not create two authorities for same fact.                                                       |
| REP-008 | Reliable handoff         | Outbound durable outbox/inbox style handoff; inbound webhook idempotency and reconciliation queue.                                                                                                                                                                    | P1           | Temporary provider failure becomes visible pending/retry state.                                                         |
| REP-009 | Public API/Webhooks      | Expose tenant-scoped domain-aligned APIs and versioned event webhooks.                                                                                                                                                                                                | P2           | Partner retry of command is safe and authorized.                                                                        |

## 17.2 Business rules

- KPI narrative from AI cannot redefine governed metric.

- Integration failure is visible reconciliation work, not silent divergence.

- No direct database integration contract.

- High-impact automation requires explicit policy/approval gate.

# 18. Security, Tenancy, Audit and Governance

| **ID**  | **Requirement**                               | **Product behavior**                                                                                                                                                                                                                                                                                       | **Priority** | **Acceptance signal**                                                                                                                                                                  |
|---------|-----------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| SEC-001 | Authentication                                | Support secure interactive authentication and enterprise-ready OIDC/SAML path; session/token controls are centrally managed.                                                                                                                                                                               | P0           | Disabled user loses new access; active session behavior follows policy.                                                                                                                |
| SEC-002 | Authorization                                 | Enforce action + scope authorization server-side for every protected operation.                                                                                                                                                                                                                            | P0           | UI hiding alone is insufficient; API denies unauthorized operation.                                                                                                                    |
| SEC-003 | Tenant isolation                              | Tenant identity is mandatory context for data access, jobs, caches, AI retrieval and integration processing.                                                                                                                                                                                               | P0           | Cross-tenant test suite passes for reads/writes/background jobs.                                                                                                                       |
| SEC-004 | Branch/site/account scope                     | Selected permissions may be constrained to organizational/operational scope.                                                                                                                                                                                                                               | P1           | Branch dispatcher cannot mutate unrelated branch unless elevated.                                                                                                                      |
| SEC-005 | Approval controls                             | High-risk actions such as financial override, high-value adjustment, entitlement override and permission changes can require approval.                                                                                                                                                                     | P1           | Approval state and approver evidence are recorded.                                                                                                                                     |
| SEC-006 | Audit classification                          | Differentiate normal operational timeline from immutable security/compliance audit.                                                                                                                                                                                                                        | P0           | Permission changes and financial overrides appear in immutable audit stream.                                                                                                           |
| SEC-007 | Data protection                               | Secrets, sensitive files and cached mobile data use least-privilege access, encryption and retention policy.                                                                                                                                                                                               | P0           | Offline device stores only authorized job subset and supports revocation strategy.                                                                                                     |
| SEC-008 | Retention, privacy and tenant de-provisioning | Historical business facts follow configurable retention/legal-hold policy; anonymization/legal deletion is separate from ordinary record deactivation. Tenant offboarding defines export, access shutdown, retention/hold and eventual purge, with shorter retention available for location/GPS telemetry. | P1           | Deactivating a tenant blocks access without immediately deleting protected history; an authorized purge cannot remove records under legal/financial hold and follows auditable policy. |

# 19. Non-Functional Requirements

| **NFR area**              | **Requirement**                                                                                                                                                                                                                                                             | **Initial target / principle**                                                                                                                                          |
|---------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Availability              | Core back-office and field sync should degrade gracefully under partial provider failure.                                                                                                                                                                                   | External maps/SMS/AI failure must not make core WO data unavailable.                                                                                                    |
| Performance               | Interactive business screens should remain responsive under realistic tenant volumes.                                                                                                                                                                                       | Target p95 \< 2s for common filtered queries; specialized optimizer/report jobs may be asynchronous.                                                                    |
| Scale                     | Architecture supports hundreds of tenants and large per-tenant work/asset histories without giant aggregates.                                                                                                                                                               | Use paging, read models, bounded aggregate transactions and background processing.                                                                                      |
| Reliability               | Retry-sensitive operations are idempotent and observable.                                                                                                                                                                                                                   | Payments, inventory, webhooks, offline sync and outbox processing are explicitly covered.                                                                               |
| Offline                   | R1 field workflow continues for downloaded assignments with local persistence, idempotent command queue/replay and core conflict signaling; richer reconciliation tooling follows in R1.5.                                                                                  | Technician can start/record work/evidence offline and later sync exactly once; contradictory server state produces visible reconciliation rather than silent overwrite. |
| Observability             | Logs, traces, metrics and correlation IDs cover API, jobs and integration flows.                                                                                                                                                                                            | Every critical failure can be traced from user action to background/integration outcome.                                                                                |
| Accessibility             | Office, portal and mobile experiences use semantic controls, keyboard support and readable states.                                                                                                                                                                          | No critical workflow depends only on color.                                                                                                                             |
| Localization / Time zones | Store canonical instants while preserving the operational timezone context needed to reproduce decisions. SLA policy defines its business calendar/timezone (default Site timezone); site access windows use Site timezone; resource shifts use Resource calendar timezone. | Cross-time-zone dispatch and SLA calculation produce the same deadline regardless of dispatcher browser timezone; UI clearly labels relevant local time.                |
| Data integrity            | Financial/inventory/booking integrity uses transactional and concurrency controls.                                                                                                                                                                                          | No silent double booking, duplicate payment or duplicate serial consumption.                                                                                            |
| Security                  | Least privilege, secure defaults and dependency/secret management are release gates.                                                                                                                                                                                        | Tenant isolation and authz tests are mandatory CI gates.                                                                                                                |
| Maintainability           | Domain modules expose explicit contracts and avoid cyclic dependencies.                                                                                                                                                                                                     | Architecture fitness checks enforce dependency rules.                                                                                                                   |
| AI quality                | Supported AI use cases have evaluation cases and source/provenance checks.                                                                                                                                                                                                  | AI release blocked on cross-tenant leakage or uncited critical answer regressions.                                                                                      |

# 20. Release 1 Vertical Slice Acceptance

| **R1 success definition**                                                                                                                                                                                                                                                                                                                                                                                                    |
|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| A realistic service company can onboard a customer/site/asset, capture a request, authorize work, schedule a technician, execute a downloaded assignment with or without connectivity, consume a part, capture evidence/signature, calculate a simple charge from the Base Pricebook, post an invoice, and see asset history - with permissions, audit, concurrency protection, idempotent replay and tests across the flow. |

1.  Administrator creates tenant, users and scoped permissions.

2.  Support creates Account, Contact, Site and Asset.

3.  Customer issue becomes Service Request, triaged into Work Order.

4.  WO declares basic resource requirement; dispatcher creates Booking and Assignment after eligibility checks.

5.  Technician receives assignment, starts travel, arrives, records diagnosis, completes required checklist, records one part usage and captures signature.

6.  Truck stock decreases exactly once; asset timeline records service/component change where applicable.

7.  WO reaches Operationally Complete after gates pass.

8.  Base Pricebook (PRI-001A) calculates simple labour/part charge; invoice is created and posted.

9.  WO reaches Financial Closure after billing gate passes.

10. Operations can trace request -\> WO -\> booking -\> execution -\> stock -\> invoice -\> asset history from one correlated business journey.

## 20.1 R1 exit gates

| **Gate**          | **Required evidence**                                                                                                                                                                                                  |
|-------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Functional        | All R1 scenarios pass automated acceptance/API tests and manual UX validation.                                                                                                                                         |
| Security          | Tenant isolation + permission negative tests pass.                                                                                                                                                                     |
| Integrity         | Concurrency tests cover resource booking conflicts, inventory/serial consumption and retry-sensitive commands; only one conflicting resource commitment may succeed.                                                   |
| Offline execution | R1 demonstrates local persistence + command queue + idempotent replay for downloaded assignments; core cancellation/reassignment conflicts surface for office review. Advanced reconciliation tooling may remain R1.5. |
| Observability     | Critical workflow has trace/correlation and actionable error logs.                                                                                                                                                     |
| Performance       | Seeded realistic data shows no obvious N+1/full-table scan path in core list and timeline views.                                                                                                                       |
| Usability         | Support, Dispatcher and Technician can complete R1 flow without technical knowledge or hidden admin workarounds.                                                                                                       |

# 21. Deferred Scope and Anti-Overengineering Guardrails

| **Do now**                                                      | **Do later when evidence justifies it**                                                                                      |
|-----------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| Modular monolith with strong module boundaries                  | Microservices only after independent scaling/deployment/team ownership becomes concrete.                                     |
| Manual dispatcher + explainable recommendations                 | Full autonomous optimizer / solver after real scheduling data and constraints are validated.                                 |
| Core truck/warehouse inventory                                  | Deep WMS/bin orchestration only for tenants that need it.                                                                    |
| Simple pricebook and invoice in R1                              | Country-specific tax/e-invoice adapters and complex revenue accounting later.                                                |
| Provider abstractions for AI/vector                             | Multi-provider orchestration, fine-tuning and advanced agent workflows after core use cases prove value.                     |
| Operational reporting projections + concrete domain automations | Dedicated warehouse/lakehouse and generic no-code automation designer only when scale/repeated tenant demand justifies them. |
| PWA/mobile offline-capable architecture in R1                   | Native mobile app only if field/device requirements justify platform-specific capability.                                    |
| Core RBAC + scoped permissions                                  | Generic ABAC policy language only after repeated real policy patterns emerge.                                                |

| **Architecture discipline**                                                                                                                   |
|-----------------------------------------------------------------------------------------------------------------------------------------------|
| Do not create infrastructure because it looks senior. Senior design minimizes irreversible complexity while preserving a clean path to scale. |

# 22. Traceability, Product Metrics and Quality Gates

## 22.1 Requirement traceability

Every implementation epic/story should reference one or more PRD requirement IDs. Architecture Decision Records (ADRs) should reference the requirements or constraints that caused the decision. Acceptance tests should reference requirement IDs where practical.

## 22.2 Product metrics

| **Metric family** | **Initial measures**                                                               |
|-------------------|------------------------------------------------------------------------------------|
| Demand            | Request volume, backlog age, emergency mix                                         |
| Scheduling        | Time-to-schedule, unfilled requirements, reschedule rate, travel ratio             |
| Service           | Arrival SLA, resolution SLA, first-time-fix, repeat visit/callback                 |
| Field             | Completion cycle time, checklist compliance, offline sync failure rate             |
| Assets            | Repeat failures, MTTR, PM compliance                                               |
| Inventory         | Stockout, fill rate, truck variance, RMA rate                                      |
| Commercial        | Estimate acceptance, invoice cycle time, discount rate                             |
| Finance           | Revenue, gross contribution, receivables, agreement profitability                  |
| Customer          | Cancellation/no-show, complaint rate, portal adoption                              |
| AI                | Cited-answer rate, user acceptance, correction/unsafe feedback, retrieval failures |

## 22.3 Quality gates

- No P0 requirement is accepted without testable acceptance behavior.

- Every state-changing feature defines authorization, audit, concurrency and failure behavior.

- Every external integration defines authority, retry/idempotency and reconciliation path.

- Every offline-capable mutation defines conflict behavior.

- Every AI use case defines source authority, permission scope, citation/provenance and human-control level.

- Every financial/inventory adjustment path is explicit; no hidden direct edits of posted/ledger facts.

# 23. Open Decisions and Validation Plan

| **Area**                | **Open decision**                                        | **Current assumption**                                                                                    | **Validation method**                                 |
|-------------------------|----------------------------------------------------------|-----------------------------------------------------------------------------------------------------------|-------------------------------------------------------|
| Crew                    | Static vs ad-hoc frequency                               | Support named crew + ad-hoc assignments                                                                   | Interview 3-5 service operators / observe dispatch    |
| Vehicle                 | Mandatory allocation?                                    | Optional schedulable/linkable resource                                                                    | Validate HVAC/solar/facility workflows                |
| Asset ownership         | Owner vs service/billing account                         | Allow different accounts                                                                                  | Validate leasing/managed-service use cases            |
| Inventory valuation     | Average/FIFO/standard/external ERP                       | Operational cost + integration-friendly                                                                   | Decide during architecture/finance integration design |
| Tax/e-invoice           | Jurisdictional responsibility                            | Billing interface + country adapter                                                                       | Defer until target launch geography selected          |
| Agreement stacking      | Multiple coverage combination                            | Policy-driven precedence                                                                                  | Validate contract examples                            |
| PM grouping             | Per asset vs grouped site visit                          | Configurable strategy                                                                                     | Validate maintenance operations                       |
| Offline merge           | Auto-merge fields                                        | Conservative; explicit conflicts                                                                          | Prototype sync conflict matrix                        |
| Portal booking          | Which jobs instant-book                                  | Simple single-resource types first                                                                        | Usability + scheduling pilot                          |
| AI deployment           | Local vs hosted                                          | Provider abstraction                                                                                      | Benchmark cost/latency/privacy after R1               |
| WO completion authority | Who confirms Operational Completion after final Booking? | Lead/service manager or policy-based automatic evaluation; Assignment completion never closes WO directly | Validate during UX + event-storming scenarios         |
| SLA timezone            | Which timezone/calendar governs deadlines?               | SLA policy timezone/calendar; default Site timezone. Resource shifts remain resource-calendar-local       | Validate multi-region scheduling scenarios            |

# 24. PRD Exit Criteria and Handoff

| **Artifact / decision**                 | **Status**                              |
|-----------------------------------------|-----------------------------------------|
| Product outcomes and non-goals          | Complete                                |
| Personas and operating scenarios        | Complete                                |
| Cross-cutting platform requirements     | Complete                                |
| Core functional capability requirements | Complete at product level               |
| Release strategy / R1 vertical slice    | Revised in v1.1; closure review passed |
| NFRs and quality gates                  | Complete                                |
| Security/tenancy/audit requirements     | Complete at product level               |
| AI governance requirements              | Complete at product level               |
| Open questions / validation plan        | Complete                                |
| UX information architecture             | Next phase after approved baseline      |
| Event storming validation               | Next phase after approved baseline      |
| Architecture Decision Records           | Next after UX/domain validation begins  |
| Logical data model                      | Not started by design                   |
| API contracts                           | Not started by design                   |

| **PRD verdict**                                                                                                                                                                                                                                                                    |
|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CONDITIONAL PASS - v1.0 independent review blockers have been incorporated into this v1.1 Approved Baseline. Do not promote to Approved Baseline until a focused closure review confirms BLK-001 through BLK-004 and the agreed high-severity corrections are resolved without regression. |

## 24.1 Immediate next sequence

11. UX Information Architecture: role workspaces, navigation, screen inventory and task journeys.

12. Event Storming / Domain Validation: validate commands, events, invariants and exception paths against R1 scenarios.

13. Architecture Decision Records: modular boundaries, tenancy, auth, offline sync, messaging, file storage, AI gateway and integrations.

14. Logical Data Model: map accepted domain semantics to entities/value objects/keys/effective dating/ledger and concurrency models.

15. API & Integration Contracts: define REST/action semantics, webhooks, idempotency and adapters.

16. Security Model: detailed permission catalogue, scope model and threat controls.

17. Release Slicing and Engineering Backlog: convert R1 into epics/stories/acceptance tests/technical enablers.

18. Implementation: build vertical slices with tests, observability and demoable product outcomes.

**Baseline approval: APPROVED.** Product-level requirements are frozen as the working v1.1 baseline. Future material changes must be recorded through controlled requirement/domain decisions rather than silent edits.

# Appendix A. Requirement Priority Model

| **Priority** | **Ship rule**                                             | **Typical examples**                                                                                                                                          |
|--------------|-----------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| P0           | Must exist before core workflow can be trusted            | Tenant isolation, WO/Booking integrity, essential offline command/sync foundation, part consumption integrity, Base Pricebook/charge source, completion gates |
| P1           | Required for enterprise-operable product soon after spine | Estimates, SLA, agreements, recurring agreement billing, job costing, notifications, integration reliability                                                  |
| P2           | High-value extension after reliability baseline           | Portal/self-service, advanced procurement, AI assistants, configurable automation, advanced optimizer                                                         |
| Deferred     | Explicitly postponed                                      | Microservices, full WMS, OLAP warehouse, autonomous AI, generic rules engine                                                                                  |

# Appendix B. Canonical Product States

| **Entity**             | **Representative states / notes**                                                                                                                                                           |
|------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Service Request        | New -\> Triaged -\> Resolved without field / Converted to WO / Closed                                                                                                                       |
| Work Order             | Draft -\> Approved -\> Scheduled/In Progress -\> Paused/Follow-up Required -\> Operationally Complete -\> Financially Closed; Booking completion triggers evaluation, not automatic closure |
| Booking                | Proposed -\> Scheduled -\> Confirmed -\> Dispatched -\> Traveling -\> Arrived -\> In Progress -\> Completed; Rescheduled/Cancelled/No Access paths                                          |
| Estimate               | Draft -\> Pending Approval -\> Presented -\> Accepted/Rejected/Expired/Superseded                                                                                                           |
| Invoice                | Draft -\> Posted/Confirmed -\> Partially Paid/Paid; Draft may be cancelled/voided; Posted corrections use Credit Note or Credit-and-Rebill only                                             |
| Agreement              | Draft -\> Proposed -\> Approved -\> Active -\> Expiring -\> Renewed/Expired; Suspended/Terminated                                                                                           |
| RMA                    | Requested -\> Authorized -\> Received -\> Inspected -\> Dispositioned -\> Closed                                                                                                            |
| Maintenance Occurrence | Planned -\> Generated -\> Scheduled -\> Completed; Skipped/Deferred                                                                                                                         |

# Appendix C. Acceptance Scenarios

| **Scenario**                 | **Expected product behavior**                                                                                                                                                                                       |
|------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AC-01 Reactive repair        | Hospital generator request -\> emergency WO -\> qualified booking -\> field repair -\> board usage -\> signature -\> invoice/claim -\> asset history.                                                               |
| AC-02 Multi-visit            | Diagnosis Booking/Assignments complete; WO evaluates to Paused/Awaiting Parts; after part readiness a second Booking performs repair; only final completion evaluation can transition WO to Operationally Complete. |
| AC-03 Multi-resource         | One chiller Booking has lead + two technicians; individual Assignments/labour complete independently; consolidated customer visit/signature remains one Booking and WO closes only when scope gates are satisfied.  |
| AC-04 Warranty + agreement   | Manufacturer covers part, agreement covers labour; customer charge zero while inventory decreases and manufacturer claim remains open.                                                                              |
| AC-05 Offline conflict       | Server cancels Booking while offline technician starts work; sync persists field evidence/idempotent usage, does not reopen cancelled Booking, and creates a reconciliation item for office review.                 |
| AC-06 Inventory variance     | Warehouse sends 10, destination receives 9; transfer variance recorded, not silently balanced.                                                                                                                      |
| AC-07 Additional work (R2)   | R2 scenario: technician discovers extra repair; customer rejects change estimate; original authorized scope can continue/complete and refusal/hazard evidence is recorded.                                          |
| AC-08 Preventive maintenance | Plan creates occurrence -\> WO within horizon -\> schedule + standard kit -\> inspection -\> occurrence complete -\> next due date.                                                                                 |
| AC-09 Payment allocation     | One payment is recorded then allocated across two invoices; invoices update balances without duplicating cash receipt.                                                                                              |
| AC-10 AI troubleshooting     | Technician asks about error code; RAG retrieves authorized current manual + asset history, cites evidence and does not modify diagnosis until technician confirms.                                                  |

# Source Baseline

This PRD is derived from Servexa Master Domain Discovery v1.0 and intentionally preserves its frozen business semantics. It converts that domain baseline into product requirements; it does not yet prescribe SQL schema, API controller design or React component structure.

# Appendix D. v1.1 Baseline Review Disposition

This appendix records the independent v1.0 PRD review findings accepted, modified or deliberately not adopted. The purpose is to preserve traceability without turning product requirements into premature implementation design.

| Review ID     | Finding                               | Disposition         | v1.1 resolution                                                                                                                                                                                |
|---------------|---------------------------------------|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| REV-BLK-001   | Offline release contradiction         | ACCEPT              | R1 now includes offline-capable local persistence, command queue, idempotent replay and core conflict signaling; R1.5 keeps advanced reconciliation/operations tooling.                        |
| REV-BLK-002   | Requirement ID collisions / AI syntax | ACCEPT              | Portal IDs re-keyed to POR-xxx; AI IDs corrected to AI-xxx.                                                                                                                                    |
| REV-BLK-003   | Booking/Assignment vs WO completion   | ACCEPT              | Completion chain explicitly separates Assignment, Booking and Work Order evaluation; multi-visit WO can remain Paused/Follow-up Required.                                                      |
| REV-BLK-004   | R1 pricing dependency                 | ACCEPT              | PRI-001 split into Base Pricebook P0/R1 and Advanced Pricebooks P1/R2.                                                                                                                         |
| REV-H-001     | Scheduling concurrency                | MODIFY              | Product invariant is specified: overlapping exclusive-resource commitments cannot both succeed. Exact lock/version mechanism is deferred to Architecture ADRs.                                 |
| REV-H-002     | Posted invoice void ambiguity         | ACCEPT              | Posted invoices are immutable; correction is Credit Note or Credit-and-Rebill. Void/cancel applies only before posting.                                                                        |
| REV-H-003     | Recurring agreement invoicing         | ACCEPT              | AGR-011 added as distinct agreement billing occurrence/batch behavior.                                                                                                                         |
| REV-H-004     | Offline negative inventory            | ACCEPT              | INV-011 preserves physical usage and raises explicit reconciliation exception; detailed accounting treatment remains architecture/finance design.                                              |
| REV-H-005     | Multi-asset execution attribution     | ACCEPT              | FIE-011 requires asset-scoped tasks/readings/evidence/part usage where a WO covers multiple assets.                                                                                            |
| REV-H-006     | Customer service report publication   | MODIFY              | POR-005 adds curated report and configurable auto-publish vs manager-review policy; manual approval is not universally mandatory.                                                              |
| REV-H-007     | AI priorities / direct writes         | MODIFY              | AI retrieval security is P2 but mandatory when AI is enabled. AI may propose actions only; state changes require human confirmation plus normal deterministic authorization/domain validation. |
| REV-SCOPE-001 | Generic automation engine             | ACCEPT AS GUARDRAIL | R5 starts with concrete domain automations. Generic tenant no-code automation designer is deferred until repeated demand justifies it.                                                         |
| REV-NFR-001   | Timezone ambiguity                    | MODIFY              | SLA policy owns its calendar/timezone with Site timezone default; site access uses Site timezone; resource shifts use Resource calendar timezone; canonical instants are stored.               |
| REV-NFR-002   | Retention/de-provisioning depth       | ACCEPT              | SEC-008 now covers tenant offboarding, legal holds, anonymization/purge policy and shorter telemetry/GPS retention.                                                                            |

## Critical Acceptance Clarifications

| ID                                      | Acceptance clarification                                                                                                                                                                                                                                                                                |
|-----------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| CAC-001 - Scoped authorization          | Given a user has an action permission but the target record is outside the user's authorized tenant/branch/site/account scope, when the protected command/query is attempted, then Servexa denies it server-side, performs no business mutation, and records an auditable denial where policy requires. |
| CAC-002 - Concurrent booking            | Given the same exclusive resource is free in two dispatcher views, when two overlapping commitments are submitted concurrently, then at most one succeeds; the other is rejected/revalidated as a schedule conflict and the resource calendar is refreshed.                                             |
| CAC-003 - Offline cancellation conflict | Given the server cancels a Booking while a technician works offline, when the device reconnects, then captured evidence and idempotent usage are preserved, the cancelled Booking is not silently reopened, and an office reconciliation item is created.                                               |
| CAC-004 - Financial closure             | Given a WO is Operationally Complete but a required invoice remains Draft or a blocking financial/inventory reconciliation is unresolved, when Finance attempts Financial Closure, then closure is denied and the unresolved blockers remain visible.                                                   |
| CAC-005 - Posted invoice correction     | Given an invoice is Posted, when a user attempts to alter its financial lines/totals, then the original remains unchanged and correction must be performed by a linked Credit Note or Credit-and-Rebill workflow.                                                                                       |

## Closure Review Result

A focused closure review was completed against the v1.1 Candidate. All original blockers BLK-001 through BLK-004 and the accepted/modified high-severity findings were confirmed resolved, with no new blocker or high-severity regression. The review loop is closed for this baseline; further detail now belongs to UX Information Architecture, domain validation, Architecture Decision Records, logical data modeling and API/integration design.
