# Servexa UX Information Architecture v1.0

**Status:** Candidate for UX/Domain Validation  
**Source of truth:** Servexa Master PRD v1.1 Approved Baseline

## 1. Objective
Translate approved product requirements into a clear role-based product structure: who sees what, where each task starts, how users move between records, which screens exist in R1, what belongs on desktop vs field mobile, and what exception/offline states must be visible.

This document does not define styling, database schema, APIs, or React components.

## 2. UX Principles
- Role-first, not module-first.
- Keep operational context visible: Account, Site, Asset, Work Order, Booking, SLA.
- Exceptions are first-class queues.
- Technician mobile is task-sequenced, not a small back-office UI.
- Internal vs customer-visible information is explicit.
- One authoritative operational home per business fact.
- R1 prioritizes end-to-end completion over feature count.

## 3. Role Workspaces

### Support / Service Desk
Navigation:
- Home / Queue
- Service Requests
- Customers
- Sites
- Assets
- Work Orders
- Communications

Home queues:
- New/untriaged requests
- SLA-at-risk
- Duplicate candidates
- Awaiting customer info
- Recently created Work Orders

### Dispatcher
Navigation:
- Dispatch Board
- Unscheduled Work
- Bookings
- Resources
- Availability
- Exceptions

Key actions:
- Find candidates
- Create booking
- Add/remove assignment
- Reschedule
- Dispatch
- Cancel
- Inspect conflict reason

### Technician Mobile
Primary navigation:
- Today
- My Jobs
- Sync
- Knowledge
- Profile / Availability

Job sequence:
1. Job brief
2. Start travel
3. Arrive
4. Access/safety checks
5. Start work
6. Diagnose
7. Tasks/inspections/readings
8. Parts
9. Additional work
10. Customer sign-off
11. Complete assignment/visit
12. Sync status

Always visible:
- Online/offline state
- Sync state
- Current job
- Site
- Asset
- Priority/SLA where relevant
- Safety warnings

### Service Manager
Navigation:
- Operations Overview
- Work Orders
- Exceptions
- SLA
- Callbacks
- Service Reports
- Approvals

Queues:
- Follow-up required
- Completion review where policy requires
- Callback candidates
- Customer report review
- SLA breaches
- Offline reconciliation exceptions

### Warehouse / Inventory
Navigation:
- Stock
- Reservations
- Pick / Issue
- Transfers
- Truck Stock
- Returns / RMA
- Reconciliation

### Finance
Navigation:
- Billing Queue
- Draft Invoices
- Posted Invoices
- Payments
- Credits / Rebill
- Financial Closure
- Job Cost

### Account / Commercial
R1:
- Accounts
- Contacts
- Sites
- Commercial history

Later:
- Estimates
- Agreements
- Entitlements
- Warranty
- Renewals
- Recurring billing

### Administrator
Navigation:
- Users
- Roles / Permissions
- Branches
- Territories
- Work Types
- Skills
- Certifications
- Policies
- Audit
- Integrations
- Feature Flags

## 4. Global Desktop Shell
- Workspace switcher
- Global search
- Notifications/exceptions
- Create action
- Tenant/branch context
- User menu

Navigation is permission-aware. Do not show every module to every role.

## 5. R1 Screen Inventory

### Foundation
- Sign in
- Tenant / branch context
- User & role management
- Audit viewer
- Basic configuration

### Customer / Service
- Account list/detail
- Contact detail
- Site detail
- Asset list/tree
- Asset detail/history
- Service Request queue/detail
- Work Order list/detail

### Scheduling
- Unscheduled Work
- Dispatch Board
- Candidate Finder
- Booking detail
- Resource detail/calendar
- Scheduling conflict panel

### Field Mobile
- Today
- Job brief
- Travel / arrival
- Work execution
- Diagnosis
- Tasks / inspections
- Reading capture
- Part usage
- Customer signature
- Completion
- Sync / conflict state

### Inventory
- Product lookup
- Location stock
- Truck stock
- Part usage history
- Reconciliation queue

### Commercial / Billing
- Base Pricebook
- Billing Queue
- Draft Invoice
- Posted Invoice
- Credit / rebill entry
- Payment record
- Financial Closure review

### Operations
- Operational overview
- SLA/exception view
- Follow-up required queue

## 6. Core Record Pages

### Account Details
Tabs:
- Overview
- Contacts
- Sites
- Assets
- Service History
- Financial summary
- Documents

### Site Details
Show:
- Account
- Address/timezone
- Access instructions
- Hazards
- Service hours
- Contacts
- Assets
- Open work
- Upcoming bookings
- Service history

### Asset Details
Show:
- Identity
- Equipment model
- Serial
- Parent/child hierarchy
- Site
- Status
- Service history
- Readings
- Component changes
- Open work

### Service Request Details
Show:
- Customer/site/asset
- Reported symptom
- Source/channel
- Priority
- SLA candidate
- Communications
- Triage
- Duplicate indicators
- Resulting Work Order

Primary actions:
- Triage
- Resolve without field work
- Create Work Order
- Request more information

### Work Order Details
Header:
- WO number
- status
- customer/site
- asset(s)
- priority
- SLA
- operational/financial state

Sections:
- Scope
- Assets
- Bookings
- Resource requirements
- Tasks/execution
- Parts
- Evidence
- Timeline
- Commercial summary
- Related follow-up/callback

Critical rule: completed Booking must not visually imply entire Work Order is complete.

### Booking Details
Show:
- Date/time
- Site
- Work Order
- Assignments
- Travel window
- Status
- Dispatch state
- Conflicts
- Customer window

Actions:
- Assign
- Replace resource
- Reschedule
- Dispatch
- Cancel
- Complete visit where authorized

## 7. Critical R1 Journeys

### J-01 Normal Repair
Service Request → Triage → Work Order → Candidate Finder → Booking → Assignment → Dispatch → Travel → Diagnose → Part Usage → Signature → Booking Complete → WO Completion Evaluation → Billing Queue → Base Pricebook → Invoice → Payment → Financial Closure → Asset History.

### J-02 Multi-Visit Repair
Visit 1 diagnosis → part unavailable → Booking Complete → WO Paused/Awaiting Part → part available → second Booking → repair → signature → Booking Complete → WO Completion Evaluation → Operationally Complete.

### J-03 Multi-Technician Visit
One Booking with multiple Resource Assignments. Each resource records own travel/work and completes own Assignment. Booking closes when visit conditions are satisfied. Work Order completion is evaluated separately.

### J-04 Concurrent Scheduling Conflict
Two dispatchers try to commit same exclusive resource. First valid commit succeeds; second receives visible conflict and refreshed availability.

### J-05 Offline Technician Conflict
Office cancels/reassigns while technician is offline. Field evidence is preserved on sync, authoritative server state is not silently overwritten, sync is marked conflict, and office reconciliation is created.

## 8. Exception UX
Core exception types:
- SLA at risk
- No eligible resource
- Concurrent scheduling conflict
- Booking cancelled while offline
- Part unavailable
- Negative inventory reconciliation
- Financial closure blocked
- Posted invoice correction required
- Permission denied
- Stale record/concurrency conflict
- Failed integration
- Failed notification
- Failed sync

Every exception should answer:
1. What happened?
2. What remains safe?
3. What action is required?
4. Who owns the next action?

## 9. Empty / Loading / Error States
Operational queues must define:
- Empty
- Loading
- Partial data
- Stale
- Permission-limited
- Error
- Offline where relevant

## 10. Permission-Driven Visibility
- Technician sees assigned jobs, not unrestricted customer history.
- Dispatcher sees scheduling data, not sensitive margin unless allowed.
- Warehouse sees stock/custody, not customer margin.
- Finance sees billing/cost, not private technician notes unless required.
- Customer-facing reports exclude internal notes by default.

UI hiding is convenience only; server authorization remains authoritative.

## 11. Desktop vs Mobile Boundary

Desktop-first:
- Dispatch Board
- Complex scheduling
- Warehouse management
- Billing/credits
- Admin
- Broad analytics

Mobile-first:
- Daily jobs
- Travel/arrival
- Field tasks
- Inspections
- Photos/readings
- Barcode/serial capture
- Parts
- Signature
- Sync

## 12. R1 vs Later UX Scope

### R1
- Core service spine
- Basic scheduling
- Field execution
- Essential offline foundation
- Basic truck stock/part usage
- Base pricebook
- Invoice/payment
- Asset history
- Core audit/permissions

### R1.5
- Richer offline reconciliation
- Import/migration
- Operational reporting
- Reliability/admin tooling

### R2+
- Advanced estimates
- Richer pricing
- Reservations/procurement
- Agreements/PM
- Customer portal
- Advanced notifications
- AI/RAG
- Advanced analytics/automation

Later-release screens should stay hidden unless feature-enabled.

## 13. Traceability
Detailed UX should reference PRD IDs:
- Dispatch Board → scheduling requirements
- Offline conflict → FIE-007 / FIE-008
- Base Pricebook → PRI-001A
- Invoice immutability → PRI-005
- Multi-asset execution → FIE-011
- Offline stock reconciliation → INV-011

## 14. Exit Criteria
UX IA is sufficient when:
- every R1 persona has a workspace,
- every R1 screen is identified,
- critical R1 journeys have a clear path,
- desktop/mobile responsibilities are clear,
- exception/offline states are represented,
- customer/internal visibility boundaries are explicit,
- no screen contradicts the approved PRD,
- later-release capabilities are not pulled into R1.

**Verdict:** PASS — UX IA v1.0 is sufficient to proceed to targeted Event Storming / Domain Validation.

## 15. Next Essential Step
Targeted Event Storming only for critical R1 flows:
1. Normal repair
2. Multi-visit repair
3. Multi-technician booking
4. Concurrent scheduling conflict
5. Offline technician + part usage conflict
6. Invoice correction / financial closure

The purpose is to validate commands, events, invariants and context ownership before architecture/data design.
