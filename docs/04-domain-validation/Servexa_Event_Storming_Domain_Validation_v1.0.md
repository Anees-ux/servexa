# Servexa Event Storming & Domain Validation v1.0

**Status:** Targeted R1 Validation Complete  
**Source Inputs:** Servexa Master PRD v1.1 Approved Baseline + Servexa UX Information Architecture v1.0  
**Scope:** Critical R1 workflows only  
**Purpose:** Validate commands, domain events, invariants, ownership boundaries and exception behavior before architecture/data design.

---

# 1. Validation Principles

1. **Commands express intent.** Example: `ScheduleBooking`.
2. **Events express facts that already happened.** Example: `BookingScheduled`.
3. **One domain owns each authoritative state transition.**
4. **Cross-domain reactions happen through explicit policies/events, not hidden side effects.**
5. **R1 validation focuses on business correctness, not database/API implementation.**
6. **No architecture mechanism is fixed here unless it is a product invariant.**
7. **Offline evidence is preserved; conflicting authoritative state is not silently overwritten.**
8. **Operational completion and financial closure remain separate.**
9. **Physical inventory, operational part usage, pricing and job cost remain separate facts.**

---

# 2. Context Ownership Map

| Context | Authoritative responsibility |
|---|---|
| Customer & Account | Account, Contact, Site identity and relationships |
| Asset Management | Equipment Models, Assets, component/history lifecycle |
| Service Management | Service Requests, Work Orders, authorized scope, operational status |
| Scheduling & Workforce | Resource Requirements, Bookings, Assignments, dispatch, schedule conflicts |
| Field Execution | Travel/work intervals, diagnosis, tasks, inspections, readings, evidence, signatures |
| Inventory | Physical stock, custody, consumption ledger, reconciliation |
| Commercial & Billing | Pricebook, charge calculation, invoices, credits |
| Payments | Payment receipt/allocation/refund |
| Job Costing | Labour/part/travel/subcontract fulfillment cost |
| Identity & Platform | Tenant, permissions, audit, idempotency, concurrency policies |

---

# 3. Flow ES-01 — Normal Repair

## Business scenario
Customer reports a generator failure. A technician is scheduled, performs the repair, records one part usage and captures customer sign-off. The job is then billed and financially closed.

## Command / Event sequence

1. **Actor:** Support Agent  
   **Command:** `CreateServiceRequest`  
   **Event:** `ServiceRequestCreated`  
   **Owner:** Service Management

2. **Actor:** Support Agent  
   **Command:** `TriageServiceRequest`  
   **Event:** `ServiceRequestTriaged`

3. **Actor:** Support Agent / Service Manager  
   **Command:** `CreateWorkOrder`  
   **Event:** `WorkOrderCreated`

4. **Policy:** Work Order requires schedulable capability  
   **Command:** `CreateResourceRequirement`  
   **Event:** `ResourceRequirementCreated`  
   **Owner:** Scheduling & Workforce

5. **Actor:** Dispatcher  
   **Command:** `FindSchedulingOptions`  
   **Result:** Candidate read model only; no capacity committed.

6. **Actor:** Dispatcher  
   **Command:** `ScheduleBooking`  
   **Event:** `BookingScheduled`

7. **Actor:** Dispatcher  
   **Command:** `AssignResource`  
   **Event:** `ResourceAssigned`

8. **Actor:** Dispatcher  
   **Command:** `DispatchBooking`  
   **Event:** `BookingDispatched`

9. **Actor:** Technician  
   **Command:** `StartTravel`  
   **Event:** `TravelStarted`

10. **Actor:** Technician  
    **Command:** `MarkArrival`  
    **Event:** `TechnicianArrived`

11. **Actor:** Technician  
    **Command:** `StartWork`  
    **Event:** `WorkStarted`

12. **Actor:** Technician  
    **Command:** `RecordDiagnosis`  
    **Event:** `DiagnosisRecorded`

13. **Actor:** Technician  
    **Command:** `RecordPartUsage`  
    **Events:**  
    - `PartUsageRecorded` — Field Execution / Service context  
    - `PartConsumed` — Inventory context  
    - `AssetComponentChanged` if applicable — Asset Management

14. **Actor:** Technician  
    **Command:** `CaptureCustomerSignature`  
    **Event:** `CustomerSignatureCaptured`

15. **Actor:** Technician / Lead  
    **Command:** `CompleteAssignment`  
    **Event:** `AssignmentCompleted`

16. **Policy:** Required assignment/visit conditions satisfied  
    **Command:** `CompleteBooking`  
    **Event:** `BookingCompleted`

17. **Policy / Authorized user:** Evaluate full Work Order scope  
    **Command:** `EvaluateWorkOrderCompletion`  
    **Outcome A Event:** `WorkOrderOperationallyCompleted`  
    **Outcome B Event:** `WorkOrderFollowUpRequired`

18. **Actor:** Finance / system  
    **Command:** `CalculateCharges`  
    **Event:** `ChargesCalculated`

19. **Actor:** Finance  
    **Command:** `CreateInvoice`  
    **Event:** `InvoiceCreated`

20. **Actor:** Finance  
    **Command:** `PostInvoice`  
    **Event:** `InvoicePosted`

21. **Actor:** Finance / gateway integration  
    **Command:** `RecordPayment`  
    **Event:** `PaymentRecorded`

22. **Actor:** Finance  
    **Command:** `CloseWorkOrderFinancially`  
    **Event:** `WorkOrderFinanciallyClosed`

## Critical invariants
- A Work Order must exist before a Booking can be scheduled for it.
- A completed Booking does **not** automatically mean the Work Order is complete.
- Part usage and inventory consumption must both be recorded when company stock is physically used.
- Posted invoice data is immutable; corrections use compensating documents.
- Financial Closure cannot occur before operational and financial gates are satisfied.

## Validation result
**PASS**

---

# 4. Flow ES-02 — Multi-Visit Repair

## Business scenario
First technician visit diagnoses a failed control board. The board is unavailable. The visit closes, but the Work Order remains open. Once the part is available, a second visit completes the repair.

## Command / Event sequence

1. Existing `WorkOrderCreated`
2. `ScheduleBooking` → `BookingScheduled`
3. `AssignResource` → `ResourceAssigned`
4. `DispatchBooking` → `BookingDispatched`
5. `StartWork` → `WorkStarted`
6. `RecordDiagnosis` → `DiagnosisRecorded`
7. `RecordPartRequirement` → `PartRequirementRecorded`
8. Inventory confirms shortage → `RequiredPartUnavailable`
9. `CompleteAssignment` → `AssignmentCompleted`
10. `CompleteBooking` → `BookingCompleted`
11. `EvaluateWorkOrderCompletion`
12. Scope not fulfilled → `WorkOrderFollowUpRequired`
13. Work Order status becomes `Paused/AwaitingPart`
14. Inventory later records `PartAvailable` / `PartPreparedForJob`
15. Dispatcher `ScheduleBooking` → second `BookingScheduled`
16. Resource assigned and dispatched
17. Technician repairs asset
18. `PartUsageRecorded` + `PartConsumed`
19. `CustomerSignatureCaptured`
20. second `BookingCompleted`
21. `EvaluateWorkOrderCompletion`
22. full scope satisfied → `WorkOrderOperationallyCompleted`

## Critical invariants
- First visit completion cannot close the Work Order if authorized scope is not complete.
- One Work Order may have multiple Bookings.
- Historical first-visit diagnosis remains immutable and visible.
- Follow-up visit is a new Booking, not a replacement of the first one.
- Awaiting-part state is a Work Order operational state/reason, not an Inventory state.

## Validation result
**PASS**

---

# 5. Flow ES-03 — Multi-Technician Booking

## Business scenario
One customer visit requires a lead technician plus another technician. Both participate in one Booking.

## Command / Event sequence

1. `CreateResourceRequirement`
   - Lead Technician ×1
   - Technician ×1

2. Dispatcher `ScheduleBooking` → `BookingScheduled`

3. `AssignResource(Ali, Lead)` → `ResourceAssigned`
4. `AssignResource(Bilal, Technician)` → `ResourceAssigned`

5. `DispatchBooking` → `BookingDispatched`

6. Ali and Bilal independently:
   - `StartTravel`
   - `MarkArrival`
   - `StartWork`

7. Bilal finishes earlier:
   - `CompleteAssignment(Bilal)`
   - `AssignmentCompleted`

8. Booking remains active because required visit conditions are not complete.

9. Ali completes lead tasks:
   - `CompleteAssignment(Ali)`
   - `AssignmentCompleted`

10. `CompleteBooking` → `BookingCompleted`

11. `EvaluateWorkOrderCompletion`
   - `WorkOrderOperationallyCompleted`
   - or `WorkOrderFollowUpRequired`

## Critical invariants
- One Booking can have many Assignments.
- Each resource has independent actual travel/work intervals.
- Completing one Assignment does not complete the Booking.
- Completing the Booking does not automatically complete the Work Order.
- At most one active lead assignment is permitted where the work type requires a lead.

## Validation result
**PASS**

---

# 6. Flow ES-04 — Concurrent Scheduling Conflict

## Business scenario
Two dispatchers simultaneously try to schedule the same exclusive technician into overlapping time windows.

## Command / Event sequence

1. Dispatcher A runs `FindSchedulingOptions`
2. Dispatcher B runs `FindSchedulingOptions`
3. Both see Technician T1 as available.
4. Dispatcher A submits `ScheduleBooking/AssignResource`.
5. Commit-time scheduling validation passes.
6. `ResourceAssigned` / `BookingScheduled` is committed.
7. Dispatcher B submits overlapping commitment.
8. Commit-time scheduling validation re-checks current resource availability.
9. Conflict detected.
10. **Event:** `SchedulingConflictDetected`
11. Dispatcher B receives refreshed availability and must choose another option.

## Critical invariants
- Candidate search is **not** a reservation.
- Same exclusive resource cannot have overlapping committed assignment/travel intervals.
- Concurrency guarantee is enforced server-side/business-side, not UI-only.
- At most one of the conflicting commits may succeed.

## Product-level requirement
The PRD does **not** prescribe how this is implemented technically. Architecture may choose version tokens, exclusion records, transaction locking, or another safe mechanism.

## Validation result
**PASS**

---

# 7. Flow ES-05 — Offline Technician + Part Usage Conflict

## Business scenario
Technician is offline. Office cancels/reassigns the Booking, but the technician has already started onsite and uses a physical part. The device reconnects later.

## Command / Event sequence

### Server side while technician is offline
1. Dispatcher `CancelBooking` or `ReplaceAssignedResource`
2. Events:
   - `BookingCancelled`
   - or `ResourceReassigned`

### Technician device while offline
3. Local command `StartWork`
4. Local fact: work start timestamp preserved
5. Local command `RecordInspection`
6. Local evidence captured
7. Local command `RecordPartUsage`
8. Local part usage + source truck/location identity recorded
9. Local command `CompleteAssignment`

### Reconnection
10. Device replays idempotent commands.
11. Server detects parent Booking state conflict.
12. Field evidence is persisted without rewriting authoritative Booking state.
13. `OfflineExecutionConflictDetected`
14. `SyncCompletedWithConflict`
15. Office reconciliation work item is created.
16. Technician sees `Synced — Requires Office Review`.

### Inventory branch
17. If server stock is sufficient:
    - `PartConsumed`
18. If server stock is insufficient:
    - genuine physical usage is still recorded exactly once
    - stock discrepancy/negative state is represented according to policy
    - `InventoryReconciliationExceptionCreated`

## Critical invariants
- Field evidence must not be silently discarded because office state changed.
- Cancelled server Booking must not silently revert to In Progress.
- Offline command replay is idempotent.
- Physical inventory usage must not be rejected merely to preserve a clean digital balance.
- Conflicts become explicit reconciliation work.

## Validation result
**PASS**

---

# 8. Flow ES-06 — Invoice Correction & Financial Closure

## Business scenario
A posted invoice is later found to contain an incorrect charge.

## Command / Event sequence

1. Work Order is `OperationallyCompleted`.
2. `CalculateCharges` → `ChargesCalculated`
3. `CreateInvoice` → `InvoiceCreated`
4. `PostInvoice` → `InvoicePosted`
5. Error discovered after posting.

### Prohibited path
- Modify posted line
- Delete invoice
- Silent void of posted financial history

### Corrective path
6. Finance `IssueCreditNote`
7. `CreditNoteIssued`
8. If corrected billing is required:
   - `CreateReplacementInvoice`
   - `InvoiceCreated`
   - `PostInvoice`
   - `InvoicePosted`

9. Payment/allocation state is recalculated explicitly where affected.
10. Financial closure is evaluated only after correction is resolved.
11. `CloseWorkOrderFinancially` → `WorkOrderFinanciallyClosed`

## Critical invariants
- Posted invoice financial facts are immutable.
- Corrections are explicit compensating records.
- Credit Note references the original invoice.
- Payment state and invoice correction are separate concerns.
- Work Order can remain operationally complete while financially open.

## Validation result
**PASS**

---

# 9. Cross-Flow Invariants Confirmed

The six R1 scenarios confirm the following domain invariants:

1. `Service Request ≠ Work Order`
2. `Work Order ≠ Booking`
3. `Booking ≠ Resource Assignment`
4. One Work Order may have many Bookings.
5. One Booking may have many Assignments.
6. Search/recommendation does not reserve resource capacity.
7. Committed overlapping assignments for one exclusive resource are prohibited.
8. Assignment completion, Booking completion and Work Order completion are separate transitions.
9. Physical part usage, inventory consumption, customer charge and job cost are separate facts.
10. Offline evidence is preserved under conflict.
11. Idempotency is mandatory for retry-sensitive/offline commands.
12. Posted invoices are immutable.
13. Operational Completion precedes Financial Closure.
14. Tenant boundaries and authorization apply to every command/query.

---

# 10. Important Policies Confirmed

## POL-01 Work Order Completion Policy
On `BookingCompleted`, evaluate whether all authorized operational scope and required gates are satisfied.

- If yes → `WorkOrderOperationallyCompleted`
- If no but more service is expected → `WorkOrderFollowUpRequired` / Paused with reason

## POL-02 Scheduling Commit Policy
Eligibility is revalidated when committing or rescheduling an Assignment. A stale candidate recommendation cannot override current availability.

## POL-03 Offline Conflict Policy
Preserve genuine field evidence, preserve server authoritative state, surface explicit reconciliation.

## POL-04 Part Consumption Policy
Valid physical consumption is recorded independently from pricing and customer coverage.

## POL-05 Financial Correction Policy
Posted financial records are corrected through compensating records, never silent mutation.

---

# 11. Read Models Needed by UX

These are projections/read views, not aggregate ownership decisions.

- Service Request Queue
- Unscheduled Work Queue
- Dispatcher Resource Timeline
- Candidate Scheduling Options
- Work Order Operational Timeline
- Technician Today / My Jobs
- Sync Status / Conflict Queue
- Truck Stock Snapshot
- Inventory Reconciliation Queue
- Billing Queue
- Financial Closure Blockers

---

# 12. Domain Questions Closed at This Stage

## Q-ES-01 Who completes the Work Order?
**Decision:** Technician/lead completes their Assignment/visit. Work Order operational completion occurs only after explicit completion evaluation. Tenant policy may require manager review for selected work types.

## Q-ES-02 Does first diagnosis visit become a new Work Order?
**Decision:** No. Normally it remains the same Work Order with another Booking unless scope is genuinely a separate authorized job.

## Q-ES-03 Does a candidate search reserve the technician?
**Decision:** No. Capacity is committed only during successful booking/assignment commit.

## Q-ES-04 What wins during offline conflict?
**Decision:** Neither side is silently erased. Server state remains authoritative for lifecycle; genuine field evidence is retained and reconciled.

## Q-ES-05 Can posted invoices be edited?
**Decision:** No. Use credit/rebill compensation.

---

# 13. Items Intentionally Deferred

The following do not block R1 domain validation:

- Exact database lock/concurrency mechanism
- Exact offline storage technology
- API route design
- Event bus technology
- Detailed accounting journal mapping
- Advanced inventory reservations/procurement
- Contract/SLA/PM event storms for R3
- Customer portal event storms for R4
- AI/RAG event storms for R5

These belong to later architecture/release-specific design.

---

# 14. Validation Verdict

## R1 Domain Validation: PASS

The approved PRD and UX IA are coherent across the six highest-risk R1 scenarios.

No new domain blocker was discovered.

The validated R1 backbone is:

`Service Request → Work Order → Resource Requirement → Booking → Assignment → Field Execution → Part Usage → Booking Completion → Work Order Completion Evaluation → Charge Calculation → Invoice → Payment → Financial Closure`

Exception paths for multi-visit work, concurrent scheduling, offline execution and posted-invoice correction are explicit.

---

# 15. Next Essential Step

## Architecture Decision Records (ADRs)

Now that product behavior and domain flows are validated, the next phase should decide **how Servexa will implement the approved guarantees** without overengineering.

The first ADR set should cover only essential platform decisions:

1. Modular Monolith module boundaries
2. Multi-tenancy isolation strategy
3. Authentication + authorization scope model
4. Command/idempotency/concurrency strategy
5. Offline-first field synchronization architecture
6. Reliable domain-event/outbox strategy
7. File/object storage strategy
8. Background jobs
9. Integration adapter boundary
10. AI gateway boundary (future-ready, not full R5 implementation)

After ADRs:
Logical Data Model → API Contracts → Security detail → Release backlog → Implementation.
