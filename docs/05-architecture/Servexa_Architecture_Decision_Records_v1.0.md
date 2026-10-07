# Servexa Architecture Decision Records (ADRs) v1.0

**Status:** Architecture Baseline Candidate  
**Depends on:**  
- Servexa Master PRD v1.1 — Approved Baseline  
- Servexa UX Information Architecture v1.0  
- Servexa Event Storming & Domain Validation v1.0  

**Target implementation direction:** ASP.NET Core / .NET 10, React + TypeScript, SQL Server  
**Architecture style:** Modular Monolith first  
**Decision philosophy:** Enterprise correctness with minimum necessary complexity

---

# 0. Purpose

This document converts Servexa's approved product and validated domain behavior into explicit technical architecture decisions.

It answers:

> **How will Servexa implement the required guarantees without creating premature distributed complexity?**

It does **not** define the final database schema, endpoint catalogue, UI component library, or engineering backlog.

Every ADR contains:

- Context
- Decision
- Why this decision
- Alternatives considered
- Consequences
- Guardrails
- Revisit triggers

A future change to an accepted ADR must be recorded as a superseding ADR rather than silently rewriting the decision.

---

# 1. Architecture Principles

## AP-01 — Correct business boundaries before deployment boundaries

A bounded context does not automatically become a microservice.

Servexa starts as a modular monolith because core workflows frequently require tightly coordinated changes across Work Order, Booking, Field Execution, Inventory and Billing.

## AP-02 — Strong consistency where the business invariant requires it

Examples:

- same exclusive technician cannot be committed to overlapping work,
- same serialized unit cannot be consumed twice,
- a posted invoice cannot be silently rewritten.

Do not replace these guarantees with eventual consistency simply because asynchronous architecture is fashionable.

## AP-03 — Event-driven where it reduces coupling, not everywhere

Use events for meaningful reactions and durable handoff.

Do not convert simple local method calls into asynchronous messaging without a reliability or decoupling reason.

## AP-04 — One source of truth per business fact

Examples:

- Scheduling owns committed Booking/Assignment state.
- Inventory owns physical stock.
- Billing owns invoice facts.
- Field Execution owns onsite evidence.

Read models may combine these facts, but they do not become new authoritative sources.

## AP-05 — Offline is a data architecture concern, not a UI toggle

Offline field execution affects:

- command identity,
- timestamps,
- local persistence,
- synchronization,
- conflict handling,
- authorization expiry,
- evidence upload,
- inventory reconciliation.

## AP-06 — Security is server-side

Frontend visibility is user experience only.

Tenant isolation, authorization, financial immutability and state transitions are enforced in backend/application/domain layers.

## AP-07 — Provider abstractions only at real external boundaries

Good abstractions:

- object storage provider,
- email/SMS provider,
- maps provider,
- identity provider,
- LLM provider.

Avoid creating interfaces for every internal class merely to appear "clean."

## AP-08 — Avoid infrastructure that has no current product requirement

R1 does not need:

- Kubernetes,
- Kafka,
- distributed sagas,
- service mesh,
- event sourcing,
- generic workflow engine,
- dedicated vector database,
- separate database per module,
- distributed cache for correctness.

Introduce them only after measurable need.

---

# 2. Decision Summary

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Modular Monolith and module boundaries | Accepted |
| ADR-002 | Shared-database multi-tenancy with mandatory TenantId isolation | Accepted |
| ADR-003 | Standards-based authentication + RBAC with explicit contextual scope | Accepted |
| ADR-004 | Command handling, idempotency and concurrency strategy | Accepted |
| ADR-005 | Offline-first field synchronization architecture | Accepted |
| ADR-006 | Domain events + transactional outbox, no broker in R1 | Accepted |
| ADR-007 | Object storage for files; SQL stores metadata | Accepted |
| ADR-008 | Durable background processing using .NET workers + database-backed work | Accepted |
| ADR-009 | Integration ports/adapters and anti-corruption boundary | Accepted |
| ADR-010 | Future-ready AI Gateway boundary; no R1 AI infrastructure | Accepted |
| ADR-011 | OpenTelemetry-based observability and correlation | Accepted |
| ADR-012 | Deployment topology: simple stateless API + worker + SQL + object storage | Accepted |

---

# ADR-001 — Modular Monolith and Module Boundaries

## Context

Servexa has clear business domains:

- Customer & Account
- Asset Management
- Service Management
- Scheduling & Workforce
- Field Execution
- Inventory
- Commercial & Billing
- Payments
- Job Costing
- Identity & Platform

The validated R1 workflows cross several of these domains in a single business journey.

Premature microservices would introduce distributed transactions, network failure modes, deployment complexity and operational overhead before Servexa has scale evidence.

## Decision

Build Servexa as **one deployable modular monolith** for R1.

Inside the solution, use explicit business modules with controlled dependencies.

Recommended R1 modules:

1. `Platform`
2. `Customers`
3. `Assets`
4. `Service`
5. `Scheduling`
6. `FieldExecution`
7. `Inventory`
8. `Commercial`
9. `Payments`
10. `JobCosting`

Later modules can include Contracts, PreventiveMaintenance, CustomerExperience, KnowledgeAI and Analytics.

### Internal module shape

Each major module may contain:

- Domain
- Application
- Infrastructure
- Contracts

Do not force this four-project structure for tiny modules if it adds no value. Physical project boundaries should follow meaningful compile-time boundaries, not architecture ceremony.

### Database

Use **one SQL Server database initially**.

Prefer module-owned database schemas where practical, for example:

- `service`
- `scheduling`
- `inventory`
- `billing`

A module must not casually update another module's tables.

Cross-module behavior goes through:

- application contracts,
- explicit commands,
- domain/application events,
- approved orchestration.

### EF Core

Use **module-focused DbContexts for substantial bounded contexts**, rather than one enormous DbContext.

Do not create a separate DbContext for every tiny feature.

Cross-module navigation properties are discouraged. Store foreign identities where business references are needed, but retrieve authoritative details through the owning module/read model.

## Why

This gives Servexa:

- strong domain separation,
- simple deployment,
- efficient local transactions,
- easier debugging,
- future extraction path if a module genuinely needs independent scaling.

## Alternatives rejected

### Microservices now
Rejected because team size, traffic shape and independent deployment requirements do not justify distributed complexity.

### One giant layered application
Rejected because domain ownership would degrade into shared entities/repositories.

### Event sourcing as primary persistence
Rejected because Servexa requires auditability, not event-sourcing complexity.

## Consequences

Positive:
- simpler operations,
- easier transactions,
- lower infrastructure cost.

Tradeoff:
- discipline is required to keep module boundaries clean.

## Guardrails

- No direct repository access across modules.
- No "SharedDomain" project containing all entities.
- Shared kernel limited to true primitives: IDs, time abstractions, result/error primitives, tenant context where justified.
- Domain modules cannot reference UI/provider SDKs.

## Revisit when

Extract a module only if there is evidence of:
- independent scaling,
- independent release cadence,
- regulatory isolation,
- sustained contention,
- separate team ownership,
- technology requirement impossible inside current process.

---

# ADR-002 — Multi-Tenancy Isolation Strategy

## Context

Servexa is multi-tenant from day one.

Cross-tenant leakage is a product-blocking security failure.

At the same time, database-per-tenant from R1 would multiply migrations, backups, provisioning and operations before tenant scale or regulation requires it.

## Decision

Use a **shared SQL Server database with shared tables and mandatory `TenantId` ownership** for tenant-owned records.

### Rules

1. Tenant identity comes from the authenticated server-side execution context.
2. Client payloads cannot choose or override TenantId for ordinary commands.
3. Every tenant-owned table includes TenantId.
4. Unique business constraints that are tenant-local include TenantId.
5. Queries are tenant-scoped by default.
6. Background jobs and integration messages carry explicit TenantId.
7. Cross-tenant references are rejected by application/domain validation.
8. Administrative cross-tenant operations use explicit privileged pathways, never hidden query-filter bypass.

### Defense layers

R1:
- server tenant context,
- EF Core query filters / scoped repositories,
- tenant-aware command validation,
- database constraints/indexes,
- automated cross-tenant security tests.

Later, if risk/requirements justify it:
- SQL Server Row-Level Security can be added as defense-in-depth,
- selected enterprise tenants may move to dedicated databases.

Do **not** make RLS the only tenant-isolation mechanism because application behavior, jobs, caches and integrations also require tenant awareness.

## Why

This is the best balance between:
- security,
- operational simplicity,
- cost,
- migration simplicity,
- future scale.

## Alternatives rejected

### Database per tenant from R1
Too expensive operationally for a solo/small team and unnecessary without compliance demand.

### Schema per tenant
Creates migration and object-count complexity without giving the operational isolation of separate databases.

### TenantId only in frontend
Unacceptable; not a security boundary.

## Consequences

- Every design/query needs tenant awareness.
- Composite indexes often begin with or include TenantId depending query shape.
- Test suites must include deliberate cross-tenant attack cases.

## Guardrails

- No repository method named `IgnoreTenant()` available to normal module code.
- Cache keys, object-storage paths and background jobs include tenant context.
- Imports must validate that every referenced entity belongs to the same tenant.

## Revisit when

A tenant contract requires:
- dedicated encryption keys,
- dedicated DB,
- data residency,
- independent backup/restore,
- extremely high workload.

---

# ADR-003 — Authentication and Authorization Model

## Context

Servexa requires:

- internal staff,
- technicians,
- administrators,
- future customer portal identities,
- future enterprise SSO.

Authorization also depends on business scope such as Branch, Territory, Site and Assignment.

A generic ABAC policy language would be premature.

## Decision

Separate **authentication** from **Servexa authorization**.

### Authentication

Use standards-based authentication:

- OpenID Connect / OAuth-compatible identity provider boundary.
- Do not build a custom token protocol.
- For the first-party browser application, prefer secure server-managed session / HttpOnly cookie flow where deployment topology allows it.
- Do not store long-lived access tokens in browser localStorage.
- Future native mobile/partner clients use standards-based authorization flows such as Authorization Code + PKCE.

The exact external identity provider remains replaceable.

### Authorization

Use:

1. **Capability permissions**
   - e.g. `Booking.Create`, `WorkOrder.Complete`, `Invoice.Post`

2. **Contextual scope**
   - Tenant
   - Branch
   - Territory
   - Site
   - Resource Assignment
   - Account where needed

3. **Resource-based authorization**
   when permission depends on the actual loaded business record.

### Role model

Roles are collections of capabilities.

Scope is assigned separately.

Example:

`Dispatcher`
+ `Booking.Create`
+ scope `Branch=Mardan`

This is simpler than creating hundreds of branch-specific roles.

## Why

This maps directly to Servexa's PRD and avoids both extremes:

- coarse role-only security,
- overbuilt generic policy DSL.

ASP.NET Core policy/resource-based authorization aligns well with this model.

## Alternatives rejected

### Role names embedded throughout code
Too rigid and difficult to evolve.

### Generic ABAC expression engine
Unnecessary complexity for R1.

### Frontend-only permissions
Not secure.

## Consequences

Every command/query handler must know:
- required capability,
- relevant resource scope.

## Guardrails

- Claims describe identity/context, not unrestricted business authority.
- Sensitive authorization happens server-side.
- Denied actions are security-auditable.
- Portal users receive separate external scopes; internal roles are not reused blindly.

## Revisit when

Real customer requirements show the need for delegated administration or more expressive policy combinations.

---

# ADR-004 — Command, Idempotency and Concurrency Strategy

## Context

Validated R1 scenarios require:

- retry-safe offline commands,
- retry-safe webhooks/payments,
- prevention of resource double-booking,
- stale update protection,
- exact-once business effects where possible.

"Exactly once delivery" cannot be assumed across networks.

## Decision

Use explicit **application commands** for business mutations.

### Optimistic concurrency default

Mutable consistency boundaries use an application/database concurrency token.

For SQL Server/EF Core, `rowversion` is the preferred default where appropriate.

On stale write:
- reject with conflict,
- return current version/context,
- UI refreshes or asks user to retry.

### Idempotency

Require idempotency keys for operations vulnerable to retry duplication:

- offline synchronization commands,
- payment callbacks,
- inbound webhooks,
- stock movements,
- external message commands where duplicate side effects matter.

Store processed key + tenant + command type + result reference for a bounded retention period.

Do not add idempotency persistence to every trivial interactive command unless retry duplication is a real risk.

### Scheduling concurrency

Generic optimistic concurrency alone is insufficient because two different Bookings may race for the same Resource.

Decision:

At commit/reschedule time, Scheduling performs **transactional availability revalidation** and acquires a narrow database consistency guard for the relevant exclusive resource/time commitment.

Implementation should remain SQL-local in R1.

Acceptable implementation patterns include:
- resource scheduling guard row lock,
- controlled pessimistic lock around availability commit,
- another SQL Server transaction pattern proven to serialize competing commitments.

Do **not** introduce distributed Redis locks for R1.

### Inventory serial uniqueness

Serialized unit state transitions are protected by transaction + concurrency/unique constraints so the same serial cannot be committed twice.

## Why

Use optimistic concurrency broadly, but apply narrow stronger locking only where the business invariant spans competing records.

## Alternatives rejected

### Global pessimistic locking
Hurts scalability and increases deadlock risk.

### Redis distributed locks
Adds infrastructure without need in a single-database monolith.

### UI-only collision detection
Unsafe.

## Consequences

- Some commands return HTTP/domain conflict results and require refresh.
- Scheduling commit path deserves dedicated race-condition tests.

## Guardrails

- Candidate search never reserves capacity.
- Lock scope must be short.
- No network calls inside scheduling DB critical section.
- Retries must be safe and bounded.

## Revisit when

Scheduling is extracted to a separate service or database.

---

# ADR-005 — Offline-First Field Synchronization

## Context

R1 requires technicians to continue working when connectivity disappears.

Validated conflict example:

- office cancels/reassigns a Booking,
- technician works offline,
- technician captures inspection and part usage,
- device reconnects later.

Servexa must preserve real evidence without corrupting authoritative lifecycle state.

## Decision

Implement the technician experience as an **offline-capable PWA architecture**.

### Local storage

Use browser persistent storage through an IndexedDB-backed abstraction.

Store only the authorized field package required for the technician's work horizon:

- assigned Bookings,
- necessary Work Order summary,
- Site/Asset details,
- form/checklist versions,
- required product lookup subset,
- approved knowledge snippets where needed,
- pending commands,
- pending media metadata.

Do not offline-cache the entire tenant database.

### Local command outbox

Every offline mutation stores:

- `CommandId` (globally unique)
- command type
- TenantId/user/device context
- target IDs
- local observed timestamp
- expected entity/version where relevant
- payload
- sync status

Statuses:

- Pending
- Sending
- Synced
- SyncedWithConflict
- FailedRetryable
- FailedPermanent

### Server sync

Sync API accepts idempotent command batches.

Server evaluates each command against current authoritative state.

The server never blindly applies the client's cached entity snapshot.

### Conflict policy

Evidence facts such as:
- photos,
- inspection responses,
- readings,
- observed time,
- legitimate physical part usage

are preserved where they represent real field activity.

Lifecycle commands that conflict with newer authoritative state do not overwrite it.

Create explicit reconciliation work.

### Media

Large media files upload separately/resumably from command metadata to avoid making one sync transaction enormous.

### Security

- local data minimized,
- authenticated session required to sync,
- device/session revocation prevents future server access,
- sensitive local records have expiry/cleanup policy,
- do not claim device-side web storage is equivalent to server encryption boundary.

## Why

This gives genuine offline operation without attempting complex collaborative offline editing.

## Alternatives rejected

### "Online first now, offline later"
Would force major rewrite.

### Full local replica + automatic merge
Too complex and dangerous for business workflows.

### Last-write-wins
Destroys auditability and field truth.

## Consequences

- UI explicitly displays sync state.
- Conflict workflows become part of operations.
- Commands must be designed for replay.

## Guardrails

- No silent conflict resolution for lifecycle/financial/inventory-critical facts.
- Form template version used offline remains attached to response.
- Server timestamps and device-observed timestamps are both retained where meaningful.

## Revisit when

Native mobile becomes necessary due to platform/device requirements that PWA cannot satisfy.

---

# ADR-006 — Domain Events and Transactional Outbox

## Context

Servexa has meaningful cross-module reactions:

`PartUsageRecorded`
may affect:
- Inventory,
- Asset history,
- Job Costing.

`WorkOrderOperationallyCompleted`
may trigger:
- Billing readiness,
- service report generation.

We need reliable reactions without turning R1 into a message-broker platform.

## Decision

Use two event categories.

### 1. Domain/Application events

Used inside the modular monolith to communicate meaningful completed business facts.

Handlers execute in-process when synchronous behavior is appropriate.

### 2. Integration events

For work that can occur after the source transaction and must survive process failure.

Persist them using a **transactional outbox** in the same SQL transaction as the source business change.

A background worker dispatches outbox records.

### R1 transport

No Kafka/RabbitMQ required.

The outbox dispatcher may initially invoke in-process module integration handlers.

The stored outbox format creates a future migration path to an external broker without changing domain code.

### Transaction rule

If two state changes must be atomically true for the business command to succeed, prefer:
- one local transaction/orchestrated application operation

rather than pretending asynchronous events provide atomicity.

## Why

This provides crash-safe asynchronous handoff while retaining monolith simplicity.

## Alternatives rejected

### Fire-and-forget in-memory events for critical reactions
Can lose work on process failure.

### Message broker in R1
Operational complexity without independent services.

### Event sourcing
Not required.

## Consequences

- eventual processing for selected reactions,
- handlers must be idempotent,
- outbox needs retention/cleanup.

## Guardrails

- Domain events are business facts, not CRUD notifications.
- Do not emit an event for every property change.
- Integration events are versioned contracts.
- Failed outbox items become observable/retryable.

## Revisit when

Multiple independently deployed services genuinely need the stream.

---

# ADR-007 — File and Object Storage

## Context

Servexa stores:

- photos,
- signatures,
- service reports,
- customer documents,
- manuals,
- future AI knowledge files.

Binary files inside SQL Server would increase database size, backup cost and operational friction.

## Decision

Store binary content in **object storage**.

SQL Server stores metadata:

- FileId
- TenantId
- owner/context reference
- original name
- content type
- size
- hash
- storage key
- classification
- uploaded actor/time
- visibility
- retention state

Use an internal `IObjectStorage` boundary.

Initial production provider may be Azure Blob Storage or another S3-compatible provider based on deployment choice.

Local filesystem is allowed for local development only.

### Access

- private objects by default,
- access through authorized application endpoints or short-lived signed access,
- no permanent public object URLs for business evidence.

### Integrity

Capture cryptographic content hash for evidence/signature artifacts where audit integrity matters.

### Malware scanning

Design an upload-status hook:
`PendingScan → Available / Rejected`

Actual scanner integration can be introduced according to deployment need.

## Why

Object storage is cheaper and operationally cleaner for large unstructured files.

## Alternatives rejected

### SQL varbinary for all files
Unnecessary DB growth.

### Provider SDK called directly from domain modules
Creates vendor coupling.

## Consequences

Object metadata and object bytes are two resources; cleanup must handle orphan prevention.

## Guardrails

- object key includes non-guessable identity and tenant partitioning,
- authorization never relies on filename/path secrecy,
- deletion respects audit/legal retention.

---

# ADR-008 — Background Jobs and Scheduled Work

## Context

R1 needs durable asynchronous work such as:

- outbox dispatch,
- notification delivery,
- file processing,
- reconciliation processing,
- cleanup.

Later releases add:

- SLA escalation,
- PM occurrence generation,
- agreement billing,
- reminders.

A full workflow/scheduler platform is unnecessary now.

## Decision

R1 uses:

- .NET `BackgroundService` / Worker processes,
- database-backed durable work records for tasks that must survive restart,
- outbox-driven processing where triggered by domain changes.

Deploy the worker either:
- inside the same host for simple environments, or
- as a separate worker process using the same codebase when operational isolation is needed.

The architecture must support separating the worker without splitting business modules into microservices.

### Scheduling

For R1:
- simple periodic polling/timers are sufficient for housekeeping.

When R3 introduces complex recurring schedules, evaluate a focused scheduler library such as Quartz.NET rather than building a scheduler from scratch.

Do not introduce Hangfire/Quartz merely for R1 outbox polling.

## Why

It uses native .NET hosting for current needs and preserves a path to dedicated scheduling later.

## Alternatives rejected

### Generic workflow engine
Overengineered.

### Cloud-vendor-specific functions for every background task
Fragments local development and business transactions.

## Consequences

Workers must:
- be idempotent,
- support graceful shutdown,
- expose health/metrics,
- handle retry/backoff.

---

# ADR-009 — External Integration Boundary

## Context

Servexa will integrate with:

- payment providers,
- email/SMS,
- maps/routing,
- accounting/ERP,
- SSO,
- storage,
- future IoT.

Vendor SDKs must not leak into domain logic.

## Decision

Use ports/adapters at **actual external boundaries**.

Example:

- `IPaymentGateway`
- `INotificationProvider`
- `IRoutingProvider`
- `IObjectStorage`

Provider implementations live in Infrastructure/Integration projects.

### Inbound

Inbound webhook pipeline:

1. authenticate/verify provider signature,
2. persist/identify provider event,
3. enforce idempotency,
4. translate into Servexa command/event,
5. execute normal application rules,
6. return provider response.

### Outbound

Use outbox/durable work for side effects where retry is required.

### System of record

Each integration documents who owns the fact.

Example:
- Servexa owns Work Order state.
- payment gateway owns provider transaction confirmation.
- external ERP may own General Ledger posting.

Avoid two-way "last update wins" synchronization.

## Why

This keeps vendor changes local and protects domain vocabulary.

## Guardrails

- No external provider DTOs in Domain.
- Every adapter has timeout/retry/circuit-breaker policy appropriate to provider.
- User transaction must not stay open while waiting on slow external HTTP calls.
- Failed external sync creates visible reconciliation when business-important.

---

# ADR-010 — AI Gateway Boundary

## Context

AI/RAG is planned for later releases, not R1.

However, early architecture should avoid embedding AI calls directly into domain modules.

At the same time, building vector infrastructure now would be premature.

## Decision

Create only a **future architectural seam**, not a full AI subsystem.

Future module boundary:

`KnowledgeAI`

Potential interfaces when implemented:

- `ILlmProvider`
- `IEmbeddingProvider`
- `IRetrievalService`
- `IAiToolExecutor`

### Authority

AI never becomes system of record.

Structured truth is retrieved from authoritative modules.

### Retrieval security

Permission/tenant filtering occurs before retrieved content reaches the model.

### Actions

AI can:
- read,
- summarize,
- draft,
- propose.

State-changing action uses normal application commands and authorization, with required human confirmation.

### R1

Do not provision:
- dedicated vector DB,
- embeddings pipeline,
- model orchestration framework,
- autonomous agents.

Only preserve module/provider boundaries in architecture documentation.

## Why

This avoids both:
- vendor lock-in,
- speculative infrastructure.

## Revisit when

R5 implementation starts and real retrieval volume/quality requirements are known.

---

# ADR-011 — Observability and Operational Diagnostics

## Context

Enterprise reliability requires understanding failures across:

- HTTP requests,
- background workers,
- outbox processing,
- offline sync,
- integrations.

Ad-hoc log statements are insufficient.

## Decision

Adopt **OpenTelemetry** as the observability standard.

Capture:

- traces,
- metrics,
- structured logs.

### Correlation

Propagate:

- Trace/Correlation ID,
- TenantId where safe,
- User/actor ID where appropriate,
- CommandId,
- external provider correlation ID.

Do not place secrets or sensitive customer content into telemetry by default.

### Important metrics

R1 examples:

- HTTP latency/error rate
- command failures
- concurrency conflicts
- outbox backlog age/count
- worker failures
- offline sync success/conflict rate
- integration retries
- DB connection/query health

### Health

Expose:
- liveness,
- readiness,
- dependency health where meaningful.

## Why

OpenTelemetry is vendor-neutral and supported in modern .NET.

## Alternatives rejected

### Vendor-specific instrumentation embedded everywhere
Creates lock-in.

### Logs only
Insufficient for distributed/background causality even inside a monolith.

## Consequences

Telemetry becomes part of acceptance for production-critical flows.

---

# ADR-012 — Initial Deployment Topology

## Context

The codebase needs enterprise-quality boundaries, but R1 should remain easy to deploy and operate.

## Decision

Initial production topology:

1. **Web/API application**
   - ASP.NET Core
   - serves API
   - may serve/proxy React application depending deployment

2. **Background Worker**
   - same solution/modules
   - may initially run in same deployment for small environment
   - separable when reliability/load requires

3. **SQL Server**
   - single primary operational database

4. **Object Storage**
   - documents/photos

5. **React/PWA frontend**
   - static assets/CDN/app host as appropriate

Optional infrastructure such as Redis is **not mandatory for correctness in R1**.

### Stateless server goal

Web/API instances should not depend on in-memory session state for authoritative business data.

This enables later horizontal scale.

### Cache

Use caching only for measured read-performance problems.

Never use cache as the only source for:
- permissions,
- committed schedule,
- inventory truth,
- invoice state.

## Why

This topology is simple enough for a small team and strong enough to scale vertically/horizontally before a distributed redesign is justified.

## Alternatives rejected

### Kubernetes-first
Operational burden without evidence.

### Redis-first
Not needed for business correctness.

### Per-module deployables
Premature microservices.

---

# 3. Cross-ADR Architecture Shape

```text
React / Technician PWA
          |
          v
+-----------------------------+
| ASP.NET Core Web/API Host   |
|                             |
| Platform                    |
| Customers                   |
| Assets                      |
| Service                     |
| Scheduling                  |
| FieldExecution              |
| Inventory                   |
| Commercial                  |
| Payments / JobCosting       |
+-----------------------------+
      |              |
      |              +------> Object Storage
      |
      v
  SQL Server
      |
      +--> Outbox / Durable Work
                  |
                  v
          .NET Worker Host
                  |
          External Adapters
```

Future AI is a module/adaptor boundary, not a separate deployment requirement.

---

# 4. Transaction and Consistency Rules

## Same command / same invariant
Use a local SQL transaction.

Examples:
- create Booking + its Assignments,
- consume serialized inventory unit,
- post invoice lines + invoice total.

## Cross-module reaction that can be delayed
Use outbox.

Examples:
- generate notification after Booking dispatched,
- refresh analytics projection,
- send integration webhook.

## External provider call
Do not keep SQL transaction open while waiting on provider.

Commit intent/state first, then perform durable external action.

## Strong business invariant across records
Use database transaction + targeted concurrency strategy.

Example:
- resource schedule commitment.

---

# 5. Failure Handling Strategy

| Failure | Architectural response |
|---|---|
| User sends stale update | Optimistic concurrency conflict |
| Two dispatchers race | Commit-time scheduling revalidation + narrow lock |
| Offline command resent | Idempotency key returns prior result |
| App crashes after DB commit before notification | Outbox resumes notification |
| Worker crashes | Durable work remains pending |
| External API times out | Retry/backoff; visible reconciliation if necessary |
| File upload interrupted | resumable/retry upload path |
| AI provider unavailable | core FSM continues; AI feature degrades |
| Cache unavailable | correctness continues against authoritative store |

---

# 6. Security Architecture Guardrails

- Tenant context established server-side.
- Every authoritative query/mutation tenant scoped.
- Capability + scope authorization.
- Secrets use managed environment/secret store, never source control.
- HTTPS only in production.
- Sensitive logs redacted.
- File objects private by default.
- Background jobs execute under explicit tenant/system identity.
- Database credentials use least privilege.
- Admin bypass operations audited.
- Posted financial records protected from mutation.
- No AI/tool path bypasses application authorization.

Detailed threat modeling remains a later Security Design activity.

---

# 7. Performance Guardrails

Do not optimize speculative bottlenecks.

R1 baseline:

- pagination for large lists,
- purpose-built read projections for Dispatch Board/queues,
- indexes based on real query patterns,
- no lazy-loading-driven N+1 behavior,
- no loading giant aggregate history collections,
- background work for slow side effects,
- object storage for media,
- measure before adding Redis.

Scheduling candidate search may use dedicated read queries/read models without forcing aggregate loading.

---

# 8. Testing Implications

Architecture-critical automated tests must include:

1. Cross-tenant isolation tests
2. Capability/scope authorization tests
3. Same-resource concurrent booking race
4. Offline command replay idempotency
5. Server-cancelled / offline-work conflict
6. Duplicate stock consumption replay
7. Posted invoice immutability
8. Outbox crash/retry
9. Webhook duplicate delivery
10. Module dependency architecture tests

---

# 9. Decisions Explicitly Not Made Yet

To prevent premature commitment, these belong to later phases:

- exact table/column design
- final aggregate class structure
- exact REST route names
- exact React state-management library
- exact IndexedDB helper library
- exact identity vendor
- exact object-storage vendor
- exact cloud deployment platform
- exact scheduler library for R3
- exact AI model/provider
- exact vector-store technology
- exact Redis usage
- microservice extraction

---

# 10. ADR Exit Criteria

Architecture Decision phase is complete enough to proceed when:

- module ownership is explicit,
- tenant isolation strategy is defined,
- authentication/authorization boundaries are defined,
- concurrency/idempotency rules are defined,
- offline synchronization architecture is defined,
- reliable asynchronous processing is defined,
- file and background-work strategies are defined,
- external providers are isolated,
- AI does not force premature infrastructure,
- observability/deployment baseline is defined,
- no decision contradicts approved PRD or validated domain flows.

## Verdict

**PASS — Servexa Architecture Decision Baseline v1.0 is sufficiently defined for Logical Data Modeling.**

---

# 11. Next Essential Step

## Logical Data Model

The next phase should translate the approved domain + ADRs into:

- aggregate roots and entities,
- value objects,
- identifiers,
- relationships,
- module-owned tables,
- tenant keys,
- effective-dated structures,
- concurrency tokens,
- inventory ledger structures,
- invoice/credit structures,
- outbox/idempotency records,
- audit metadata,
- indexes and uniqueness invariants at a logical level.

The Logical Data Model must preserve these ADR decisions; it must not reopen product scope.
