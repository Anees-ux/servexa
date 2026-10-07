# Servexa Engineering Architecture and Backend Standards v1.0 --- Approved Baseline

**Product:** Servexa\
**Document Type:** Backend Engineering Architecture & Standards\
**Status:** APPROVED BASELINE\
**Platform:** ASP.NET Core / .NET 10 / C# 14 / SQL Server 2025\
**Architecture:** Clean Architecture physical solution + Modular
Monolith domain ownership\
**Persistence:** EF Core Code First\
**Scope:** R0 Foundation through R1 Core Service Spine, with controlled
seams for later releases

------------------------------------------------------------------------

## 1. Purpose

This document is the canonical backend engineering architecture baseline
for Servexa.

Servexa is a multi-tenant enterprise Field Service Management / Field
Service Operations SaaS for organizations operating customers, sites,
assets, service requests, work orders, technicians/resources,
scheduling, dispatch, field execution, inventory, billing and related
workflows.

The backend must optimize for:

-   domain correctness;
-   tenant isolation;
-   maintainability;
-   explicit module ownership;
-   transactional integrity;
-   concurrency safety;
-   idempotency;
-   auditability;
-   reliable asynchronous processing;
-   performance-by-design;
-   controlled extensibility;
-   testability;
-   operational observability;
-   pragmatic enterprise engineering without premature microservices.

------------------------------------------------------------------------

## 2. Approved Backend Stack

  -----------------------------------------------------------------------
  Concern                             Approved Direction
  ----------------------------------- -----------------------------------
  Runtime                             .NET 10

  Language                            C# 14

  API                                 ASP.NET Core Controllers

  Database                            SQL Server 2025

  ORM                                 Entity Framework Core

  Database Workflow                   Code First

  Physical Architecture               Classic Clean Architecture

  Deployment Architecture             Modular Monolith

  Domain Style                        Pragmatic DDD

  Application Pattern                 Logical CQRS

  Mediator                            MediatR

  Repositories                        Aggregate-specific repositories

  Unit of Work                        EF Core DbContext

  Validation                          FluentValidation + domain
                                      invariants + DB constraints

  Mapping                             AutoMapper + explicit DTO contracts

  Errors                              Central exception handling +
                                      ProblemDetails

  Domain Events                       Yes

  Reliable Async                      Transactional Outbox

  Background Jobs                     Hangfire from day one

  Cache                               Redis only when measured/justified

  Realtime                            SignalR where operationally useful

  Files                               Object-storage abstraction

  AI                                  Provider abstraction;
                                      advisory/tool-driven

  Auth                                JWT-based authentication

  Authorization                       Scoped RBAC/capability checks
                                      enforced server-side

  Observability                       OpenTelemetry-aligned
                                      instrumentation

  Multi-tenancy                       From day one

  Deployment                          API + background processing + SQL +
                                      object storage; Redis optional
  -----------------------------------------------------------------------

Exact package versions are implementation-time decisions and must be
verified for .NET 10 compatibility, maintenance and licensing.

------------------------------------------------------------------------

## 3. Architecture Position

Servexa remains a **Modular Monolith** as a deployable system.

The physical solution follows classic Clean Architecture:

``` text
Servexa.sln
│
├── Servexa.Domain
├── Servexa.Application
├── Servexa.Infrastructure
└── Servexa.Api
```

Modules remain first-class namespaces/folders inside those layers.

Example:

``` text
Servexa.Domain/
├── Customers/
├── Assets/
├── Service/
├── Scheduling/
├── Field/
├── Inventory/
├── Billing/
└── Platform/
```

The same module ownership should be recognizable through Application and
Infrastructure.

This provides Clean Architecture dependency control without turning each
module into unnecessary projects or deployables.

------------------------------------------------------------------------

## 4. Dependency Direction

Canonical dependency direction:

``` text
Domain
   ↑
Application
   ↑
Infrastructure
   ↑
API / Composition Root
```

More precisely:

-   **Domain** depends on no Infrastructure/API concerns.
-   **Application** depends on Domain and abstractions.
-   **Infrastructure** implements persistence, external services and
    technical adapters.
-   **API** composes the application and exposes HTTP endpoints.

Do not leak:

-   EF Core concerns into core domain behavior unnecessarily;
-   Controllers into Application;
-   HTTP concepts into Domain;
-   infrastructure implementations into domain entities.

------------------------------------------------------------------------

## 5. Domain Boundaries

Servexa must preserve the approved domain distinctions:

-   Account ≠ Contact.
-   Site ≠ Address.
-   Asset ≠ Equipment Model.
-   Service Request ≠ Work Order.
-   Work Order ≠ Booking.
-   Booking ≠ Resource Assignment.
-   Operational Completion ≠ Financial Closure.
-   Resource ≠ User Account.
-   Agreement ≠ Service Plan ≠ Entitlement ≠ Warranty ≠ SLA ≠
    Maintenance Plan.
-   Product ≠ Inventory Item ≠ Part Usage ≠ Asset/component.
-   Physical stock ≠ operational usage ≠ customer charge ≠ job cost.
-   Estimate/Quote ≠ Work Order.
-   Task ≠ Checklist ≠ Inspection ≠ Form Template ≠ Form Response.

Application/API convenience must not collapse these distinctions.

------------------------------------------------------------------------

## 6. Bounded Module Direction

Core logical modules/contexts include:

``` text
Platform / Identity
Customers
Assets
Service
Scheduling
Field
Inventory
Billing
Payments
Job Costing
```

Additional product domains can evolve under the approved PRD/release
plan, including agreements, preventive maintenance, purchasing, portal,
communications, reporting, automation, integrations and AI knowledge.

Module-to-module communication must be explicit.

Avoid uncontrolled cross-module repository access.

------------------------------------------------------------------------

## 7. Pragmatic DDD

DDD is used where it protects business rules---not as ceremony.

Use:

-   aggregates;
-   aggregate roots;
-   entities;
-   value objects where meaningful;
-   domain invariants;
-   domain services only when behavior does not naturally belong to an
    entity/value object;
-   domain events for meaningful committed domain facts.

Avoid:

-   an anemic model where all rules live in handlers;
-   value objects for every primitive merely to appear "DDD";
-   giant aggregates;
-   loading entire graphs when an operation only needs a narrow
    consistency boundary.

Aggregate boundaries should reflect transactional consistency needs.

------------------------------------------------------------------------

## 8. CQRS and MediatR

Servexa uses **logical CQRS with the same primary database**.

Commands and queries are separated conceptually and structurally,
without introducing separate write/read databases in R1.

Representative structure:

``` text
Application/
└── Service/
    └── WorkOrders/
        ├── Commands/
        │   ├── CreateWorkOrder/
        │   ├── CompleteWorkOrder/
        │   └── CancelWorkOrder/
        └── Queries/
            ├── GetWorkOrder/
            └── SearchWorkOrders/
```

MediatR is the approved application dispatch mechanism.

### Commands

Commands:

-   express intent;
-   enforce authorization;
-   validate input;
-   load appropriate aggregate(s);
-   invoke domain behavior;
-   persist atomically;
-   produce domain events/outbox messages where required.

### Queries

Queries:

-   do not mutate business state;
-   may use optimized projections;
-   should avoid unnecessarily reconstructing aggregates;
-   may use EF projections or dedicated read services.

CQRS does **not** imply Event Sourcing.

------------------------------------------------------------------------

## 9. Repositories and Unit of Work

Use **aggregate-specific repositories**.

Examples:

``` text
IWorkOrderRepository
IAssetRepository
IBookingRepository
IInventoryRepository
```

Do not create a universal generic repository that merely hides EF Core.

EF Core `DbContext` is the Unit of Work.

A command should normally commit through one controlled transaction
boundary.

Repository abstractions should express aggregate persistence needs
rather than expose arbitrary IQueryable access across the entire
application.

------------------------------------------------------------------------

## 10. EF Core Code-First Workflow

Servexa explicitly uses EF Core Code First.

Canonical workflow:

``` text
Approved Logical Data Model
        ↓
Approved Physical Data Model
        ↓
C# Domain/Persistence Model
        ↓
IEntityTypeConfiguration<T>
        ↓
EF Migration
        ↓
Review Generated Migration/SQL
        ↓
Apply to Development Database
        ↓
Verify Constraints / Indexes / FKs
        ↓
Merge through PR
```

A migration is a **code-review artifact**, not an automatic side effect.

Do not blindly run production `Database.Migrate()` as the deployment
strategy.

Migration scripts/deployment steps must be controlled and reviewable.

------------------------------------------------------------------------

## 11. Physical Database Standards

Approved physical standards include:

-   SQL schemas by bounded area, e.g. `platform`, `customers`, `assets`,
    `service`, `scheduling`, `field`, `inventory`, `billing`,
    `payments`, `jobcost`;
-   `uniqueidentifier` IDs generated application-side with
    UUIDv7-compatible ordering strategy;
-   `TenantId` on every tenant-owned row;
-   tenant-safe composite foreign keys where required;
-   `datetime2(3)` for persisted UTC timestamps;
-   `decimal(19,4)` for money;
-   `decimal(18,4)` for quantities;
-   `binary(32)` where SHA-256-style hashes are persisted;
-   JSON only for genuinely flexible payloads, protected by `ISJSON`
    where applicable;
-   `smallint` for controlled persisted status values where appropriate;
-   `rowversion` on mutable concurrency-sensitive roots.

The physical model must not mechanically create one table for every
logical concept. Persistence is designed intentionally.

------------------------------------------------------------------------

## 12. Multi-Tenancy

Multi-tenancy is a foundational invariant.

Every tenant-owned entity includes `TenantId`.

Required protections:

1.  Tenant context comes from authenticated/validated server context.
2.  Client-supplied tenant identifiers are never blindly trusted.
3.  Queries must be tenant-scoped.
4.  Foreign-key relationships must not permit cross-tenant association.
5.  Unique constraints frequently include `TenantId`.
6.  Background jobs/outbox handlers explicitly restore/validate tenant
    context.
7.  Cache keys include tenant context where relevant.
8.  Object-storage paths/metadata are tenant scoped.
9.  Realtime subscriptions are tenant/user scoped.
10. Tests intentionally attempt cross-tenant access.

Tenant isolation is a security boundary, not merely a query convention.

------------------------------------------------------------------------

## 13. Authentication

### Locked Direction: JWT

Servexa uses JWT-based authentication.

The backend design must support:

-   short-lived access tokens;
-   refresh-token rotation;
-   server-side session/refresh-token tracking;
-   revocation;
-   logout;
-   expiry handling;
-   replay-risk controls;
-   future external OIDC/SSO integration.

The exact browser refresh-token transport/storage strategy receives a
focused security design before implementation.

Never derive authorization solely from claims that cannot be safely
invalidated for the required risk profile.

------------------------------------------------------------------------

## 14. Authorization

Use scoped RBAC/capability-based checks.

Authorization may consider:

-   tenant;
-   user;
-   role;
-   permission/capability;
-   branch;
-   site/account scope;
-   resource ownership/assignment;
-   workflow state.

Controller visibility is not authorization.

Every sensitive command/query enforces authorization server-side.

Examples:

``` text
WorkOrders.View
WorkOrders.Create
WorkOrders.Dispatch
Bookings.Reschedule
Inventory.Consume
Invoices.Post
Administration.ManageAppearance
```

Exact permission taxonomy should be domain-oriented and avoid exploding
into meaningless micro-permissions.

------------------------------------------------------------------------

## 15. Validation Strategy

Validation is layered.

### Transport/Application Validation

FluentValidation handles:

-   required input;
-   lengths;
-   formats;
-   simple cross-field rules;
-   request-shape validation.

### Domain Invariants

Domain behavior enforces rules such as:

-   invalid lifecycle transition;
-   completion prerequisites;
-   aggregate consistency;
-   forbidden business state.

### Database Constraints

SQL Server enforces durable invariants such as:

-   uniqueness;
-   foreign keys;
-   nullability;
-   check constraints where appropriate;
-   concurrency primitives.

Never rely on frontend validation for business correctness.

------------------------------------------------------------------------

## 16. DTOs and Mapping

API contracts use DTOs; domain entities are not returned directly.

AutoMapper is approved for controlled mapping profiles.

Rules:

-   mappings are explicit and reviewable;
-   do not hide meaningful business transformations inside mapping
    configuration;
-   commands should not blindly map arbitrary DTO graphs onto tracked
    aggregates;
-   domain behavior changes state;
-   read projections may map efficiently at query level.

------------------------------------------------------------------------

## 17. HTTP API

Use ASP.NET Core Controllers.

Controller responsibilities:

-   HTTP binding;
-   authentication/authorization entry checks where appropriate;
-   dispatch command/query;
-   map result to HTTP semantics.

Controllers should remain thin.

Representative API semantics:

``` text
POST   /api/work-orders
GET    /api/work-orders/{id}
POST   /api/work-orders/{id}/complete
POST   /api/bookings
POST   /api/bookings/{id}/reschedule
POST   /api/inventory/part-usages
POST   /api/invoices/{id}/post
```

Prefer intention-revealing operations for domain transitions instead of
forcing every workflow through generic CRUD endpoints.

------------------------------------------------------------------------

## 18. ProblemDetails and Exception Handling

Use centralized exception handling and ASP.NET Core ProblemDetails.

Classify at least:

-   validation;
-   authentication;
-   forbidden;
-   not found;
-   concurrency conflict;
-   business-rule conflict;
-   idempotency conflict;
-   unexpected failure.

Do not expose stack traces or sensitive internals to clients.

Responses should carry a trace/correlation identifier.

Expected domain/business failures should map deliberately rather than
becoming generic 500 responses.

------------------------------------------------------------------------

## 19. Idempotency

Commands vulnerable to retries/duplication must support idempotency.

High-value examples:

-   offline technician commands;
-   part usage;
-   payment/integration callbacks;
-   external webhooks;
-   create operations where client retries are expected.

Idempotency requires durable server enforcement, not merely an in-memory
check.

Where appropriate, use a unique constraint over a tenant-scoped
idempotency/business key and handle duplicate-key outcomes
intentionally.

------------------------------------------------------------------------

## 20. Concurrency

Concurrency is explicit.

Use `rowversion` on mutable roots where appropriate.

For normal aggregate edits:

``` text
Client reads version
    ↓
Command submits expected version
    ↓
EF/DB concurrency check
    ↓
Conflict → explicit 409 / resolution UX
```

Do not silently overwrite concurrent changes.

### Scheduling

Booking overlap is a business/concurrency problem, not just frontend
validation.

Approved R1 physical strategy uses `ResourceScheduleGuards` as a
technical serialization mechanism.

For scheduling commands:

1.  determine affected resources;
2.  acquire/update guards in deterministic `ResourceId` order;
3.  re-query authoritative overlapping bookings inside the transaction;
4.  reject invalid overlap;
5.  persist booking/assignment changes;
6.  commit.

Deterministic lock ordering reduces deadlock risk.

------------------------------------------------------------------------

## 21. Scheduling Domain Rules

Canonical relationship:

``` text
Work Order
   ↓
Resource Requirement
   ↓
Eligibility
   ↓
Candidate Scoring
   ↓
Booking
   ↓
Resource Assignment(s)
   ↓
Resource
   ↓
Dispatch / Travel / Work
```

Rules:

-   one Work Order may have many Bookings;
-   one Booking may have many Resource Assignments;
-   eligibility is separate from scoring;
-   skill ≠ certification;
-   branch ≠ territory;
-   availability is calendar-based;
-   travel consumes capacity;
-   manual dispatcher authority is primary;
-   AI is not scheduling authority;
-   Booking completion, Assignment completion and Work Order completion
    are distinct.

------------------------------------------------------------------------

## 22. Domain Events

Domain events represent meaningful facts raised by aggregates.

Examples:

``` text
WorkOrderCreated
BookingScheduled
BookingRescheduled
AssignmentCompleted
PartUsageRecorded
WorkOrderOperationallyCompleted
InvoicePosted
```

Domain events are not automatically external integration events.

Handlers that must survive process failure should ultimately rely on the
transactional outbox rather than in-memory delivery alone.

------------------------------------------------------------------------

## 23. Transactional Outbox

Servexa uses a transactional outbox for reliable post-commit processing.

Canonical flow:

``` text
Domain change
   +
Outbox record
   ↓
Same SQL transaction commits
   ↓
Background dispatcher reads outbox
   ↓
Handler performs side effect
   ↓
Outbox marked processed
```

Use for operations such as:

-   notification dispatch;
-   integration messages;
-   search/read-model updates where applicable;
-   reliable SignalR publication;
-   downstream automation;
-   future external events.

Consumers must tolerate duplicate delivery where relevant.

The outbox protects transactional reliability; it is not replaced by
Hangfire.

------------------------------------------------------------------------

## 24. Background Jobs --- Hangfire

### Locked Decision

Hangfire is included from day one for background job
scheduling/execution.

This supersedes the earlier ADR-008 preference for only
`BackgroundService` where that ADR conflicts with this approved
baseline.

Use Hangfire for suitable jobs such as:

-   scheduled maintenance;
-   retryable background work;
-   recurring operational jobs;
-   deferred processing;
-   controlled outbox dispatch orchestration where appropriate;
-   cleanup/retention tasks.

Important distinction:

``` text
Transactional Outbox = durable record that work must happen
Hangfire             = background execution/scheduling infrastructure
```

Do not enqueue a Hangfire job as the sole transactional guarantee for a
business change if failure between DB commit and enqueue could lose
required work.

------------------------------------------------------------------------

## 25. Inventory Integrity

Inventory separates:

``` text
Physical Stock
Operational Usage
Customer Charge
Job Cost
```

Do not collapse them.

Inventory states distinguish:

-   On Hand;
-   Reserved;
-   Available;
-   On Order;
-   In Transit;
-   Quarantined.

Reservation ≠ consumption.

`InventoryMovement` is an immutable ledger/event-style record.

`StockBalance` is a projection/current balance representation.

Serialized inventory identity persists.

Offline technician consumption may create reconciliation exceptions when
physical reality conflicts with the server's expected stock. The system
preserves the physical event/evidence rather than silently deleting
reality.

------------------------------------------------------------------------

## 26. Commercial / Billing Integrity

Rules:

-   Pricebook is separate from Product.
-   Pricebooks are effective-dated.
-   Estimate/Quote is versioned and separate from Work Order.
-   Job Cost is independent from customer charge.
-   Payment ≠ allocation ≠ invoice.
-   Operational Completion precedes Financial Closure.
-   Servexa provides service billing/subledger behavior; it is not a
    full general-ledger ERP.

### Posted Invoice Immutability

Once posted, an invoice is immutable.

Corrections occur through controlled credit/rebill/correction flows
rather than editing the posted financial document.

Enforcement should include:

-   domain/application restrictions;
-   EF persistence boundaries;
-   persisted seal/version data where approved;
-   automated tests.

------------------------------------------------------------------------

## 27. Offline Command Semantics

The technician PWA is an offline client, but backend correctness remains
authoritative.

Offline commands include:

-   stable client command ID;
-   idempotency key;
-   device timestamp;
-   server receipt timestamp;
-   relevant entity/version;
-   command payload.

The server must distinguish:

-   duplicate replay;
-   valid delayed command;
-   concurrency conflict;
-   authorization revoked;
-   entity state no longer compatible;
-   reconciliation-required physical event.

Do not use last-write-wins blindly.

------------------------------------------------------------------------

## 28. Evidence and Documents

Files/evidence use an object-storage abstraction.

The relational database stores metadata/reference information, not large
binary payloads by default.

Requirements:

-   tenant-scoped storage paths/metadata;
-   authorized upload/download;
-   content type/size restrictions;
-   malware/security strategy appropriate to deployment;
-   evidence metadata;
-   hash/seal/provenance where tamper evidence is required;
-   retention policy hooks.

The exact cloud provider remains replaceable behind the abstraction.

------------------------------------------------------------------------

## 29. Realtime

SignalR is used where realtime materially improves operations.

Examples:

-   Dispatch Board booking changes;
-   assignment changes;
-   operational notifications;
-   selected work-order state changes.

Server remains authoritative.

Realtime events should normally tell clients what changed or trigger
invalidation rather than trying to replicate the full domain model over
SignalR.

Connections/groups must respect tenant/user authorization.

------------------------------------------------------------------------

## 30. Caching

Redis is **not mandatory R1 infrastructure**.

Introduce it only after a measured need such as:

-   high-value distributed cache;
-   scale-out coordination;
-   rate limiting/session use where justified;
-   expensive reference-data caching.

Rules:

-   cache is not a correctness dependency unless explicitly designed as
    one;
-   cache keys are tenant-aware;
-   invalidation strategy is defined;
-   no premature caching of everything.

Start with correct SQL/query design and measure.

------------------------------------------------------------------------

## 31. Query and SQL Performance

Performance is designed but evidence-driven.

Guidelines:

-   project only required columns;
-   use `AsNoTracking` for read-only EF queries where appropriate;
-   paginate large result sets;
-   avoid N+1 queries;
-   inspect generated SQL for important queries;
-   create composite indexes aligned to filter/order patterns;
-   use covering indexes selectively;
-   avoid loading giant aggregate graphs for reads;
-   use dedicated projections for operational grids;
-   measure with real execution plans and representative data.

A known pattern such as:

``` sql
WHERE TenantId = @tenant
ORDER BY CreatedAt DESC
OFFSET ...
```

should generally drive index design around the tenant/filter/order
pattern rather than relying on a TenantId-only index.

Do not create indexes speculatively without workload justification.

------------------------------------------------------------------------

## 32. Auditability

Audit requirements are first-class for sensitive operational changes.

Capture as appropriate:

-   tenant;
-   actor/user;
-   timestamp;
-   operation;
-   target entity;
-   important before/after or change metadata;
-   correlation/trace ID;
-   source/client where relevant.

Do not use application logs as the only business audit trail.

Sensitive values must be redacted according to policy.

------------------------------------------------------------------------

## 33. Observability

Backend instrumentation follows OpenTelemetry-compatible practices.

Capture:

-   traces;
-   structured logs;
-   metrics;
-   correlation IDs;
-   HTTP spans;
-   database spans where useful;
-   background-job spans;
-   outbox processing;
-   integration calls;
-   high-value business-operation telemetry.

Never log:

-   passwords;
-   raw refresh/access tokens;
-   secrets;
-   unnecessary sensitive customer data.

Observability should make a production failure diagnosable across HTTP →
command → SQL → outbox/background work.

------------------------------------------------------------------------

## 34. Integrations

External systems live behind explicit integration boundaries/adapters.

Each integration defines:

-   system of record;
-   direction of ownership;
-   authentication;
-   idempotency;
-   retry policy;
-   webhook verification;
-   failure handling;
-   mapping/anti-corruption layer;
-   audit/observability.

Never let vendor DTOs become Servexa domain entities.

Duplicate webhooks must be safe.

------------------------------------------------------------------------

## 35. AI Boundary

AI is advisory/tool-driven and never the deterministic business
authority.

Rules:

-   provider abstraction;
-   permission-filtered retrieval before AI access;
-   citations/provenance where knowledge is surfaced;
-   structured system truth queried deterministically;
-   writes classified by risk;
-   meaningful writes require human confirmation where appropriate;
-   no implicit training on tenant data;
-   AI cannot bypass RBAC, tenant isolation, scheduling rules, financial
    rules or inventory correctness.

Vector database/provider choice is deferred until a concrete AI
capability requires it.

------------------------------------------------------------------------

## 36. Security Baseline

Required practices include:

-   HTTPS;
-   secure secret management;
-   JWT lifecycle controls;
-   server-side authorization;
-   tenant isolation;
-   input validation;
-   parameterized EF/SQL behavior;
-   safe file handling;
-   output encoding;
-   CORS policy appropriate to deployment;
-   rate limiting on abuse-sensitive endpoints where justified;
-   security headers;
-   dependency review;
-   audit logging;
-   least privilege for DB/storage/service identities.

Do not implement custom cryptography unless a well-defined requirement
makes it unavoidable.

------------------------------------------------------------------------

## 37. Testing Strategy

### Domain Unit Tests

Test invariants and lifecycle rules without infrastructure where
possible.

Examples:

-   invalid Work Order transition;
-   completion prerequisites;
-   posted invoice mutation rejection;
-   booking/assignment semantics.

### Application Tests

Test handlers/use cases with controlled dependencies.

### Integration Tests

Use real persistence behavior for:

-   EF mappings;
-   constraints;
-   tenant filters;
-   unique keys;
-   concurrency;
-   transactions;
-   outbox.

### API Tests

Verify:

-   authentication;
-   authorization;
-   ProblemDetails;
-   contract semantics.

### Critical R1 Tests

Mandatory high-risk coverage includes:

1.  tenant isolation;
2.  scoped authorization;
3.  concurrent booking race;
4.  offline command replay;
5.  offline conflict;
6.  duplicate stock consumption;
7.  posted invoice immutability;
8.  outbox crash/retry;
9.  duplicate webhook;
10. module dependency boundaries where enforceable.

------------------------------------------------------------------------

## 38. Solution / Project Conventions

Recommended solution:

``` text
Servexa.sln
├── src/
│   ├── Servexa.Domain/
│   ├── Servexa.Application/
│   ├── Servexa.Infrastructure/
│   └── Servexa.Api/
├── tests/
│   ├── Servexa.Domain.Tests/
│   ├── Servexa.Application.Tests/
│   ├── Servexa.Infrastructure.Tests/
│   └── Servexa.Api.Tests/
└── docs/
```

Avoid a project-per-feature explosion.

Modules remain visible through namespaces/folders and architectural
tests/conventions.

------------------------------------------------------------------------

## 39. Dependency Injection

Register dependencies by layer/module with extension methods.

Example conceptual composition:

``` text
services.AddServexaApplication(...)
services.AddServexaInfrastructure(...)
services.AddCustomersModule(...)
services.AddSchedulingModule(...)
```

Do not create a giant unstructured `Program.cs`.

Composition root owns wiring; domain code does not resolve services
through service locator patterns.

------------------------------------------------------------------------

## 40. Configuration

Use typed options for infrastructure/application configuration.

Examples:

``` text
JwtOptions
DatabaseOptions
ObjectStorageOptions
HangfireOptions
SignalROptions
AiProviderOptions
```

Validate critical configuration at startup.

Secrets are supplied through secure environment/platform secret
facilities, never committed to source.

------------------------------------------------------------------------

## 41. Background Reliability and Retry

Retries must distinguish transient from permanent failures.

Rules:

-   bounded retries;
-   exponential/backoff strategy where appropriate;
-   idempotent handlers;
-   poison/dead-letter handling or failed-job visibility;
-   structured diagnostics;
-   operator recovery path.

Do not retry permanent domain validation failures endlessly.

------------------------------------------------------------------------

## 42. Migration Sequence

Initial database implementation should follow controlled dependency
order:

``` text
1. platform
2. customers / assets
3. service
4. scheduling
5. field
6. inventory
7. billing
```

Payments remain physically deferred until its release requires
activation unless an R1 requirement explicitly changes that decision.

Each module migration is reviewed with:

-   generated operations;
-   FK ordering;
-   tenant-safe relationships;
-   indexes;
-   defaults;
-   destructive operations;
-   SQL impact.

------------------------------------------------------------------------

## 43. Release-Aware Engineering

Servexa release sequence:

``` text
R0   Foundation
R1   Core Service Spine
R1.5 Reliability
R2   Commercial Operations
R3   Recurring Service
R4   Customer & Scale
R5   Intelligence
```

Do not implement later-release infrastructure merely because the logical
model anticipates it.

Design seams now; materialize complexity when the release needs it.

------------------------------------------------------------------------

## 44. R0 Backend Foundation

Before broad feature development, establish:

1.  solution/projects;
2.  dependency rules;
3.  module folder conventions;
4.  SQL Server + EF Core infrastructure;
5.  tenant context;
6.  JWT authentication foundation;
7.  scoped authorization foundation;
8.  ProblemDetails/exception handling;
9.  FluentValidation pipeline;
10. MediatR pipeline;
11. DbContext/UoW;
12. aggregate repository conventions;
13. domain-event infrastructure;
14. transactional outbox;
15. Hangfire;
16. OpenTelemetry instrumentation;
17. object-storage abstraction;
18. API/OpenAPI conventions;
19. test projects;
20. CI quality gates;
21. migration conventions;
22. architecture/developer README.

Foundation should establish necessary contracts without building
speculative frameworks.

------------------------------------------------------------------------

## 45. R1 Vertical Slice Order

After foundation:

``` text
Customers / Accounts
    ↓
Sites
    ↓
Assets
    ↓
Service Requests
    ↓
Work Orders
    ↓
Resource Requirements
    ↓
Scheduling / Bookings / Assignments
    ↓
Field Execution
    ↓
Part Usage / Inventory
    ↓
Operational Completion
    ↓
Invoice
    ↓
Asset History
```

Each slice should include only the infrastructure needed to make that
slice production-shaped.

------------------------------------------------------------------------

## 46. Feature Delivery Workflow

Typical backend slice:

``` text
Approved requirement/invariant
    ↓
Domain model change
    ↓
Command/query contract
    ↓
Handler + authorization + validation
    ↓
Repository/persistence mapping
    ↓
Migration if required
    ↓
API endpoint
    ↓
Tests
    ↓
OpenAPI verification
    ↓
Self-review
    ↓
PR + CI
    ↓
Merge
```

Migration and feature code remain in the same feature branch when they
belong to the same change.

------------------------------------------------------------------------

## 47. Git / Branch Strategy

Use trunk-oriented short-lived branches.

``` text
main
feature/r0-platform-foundation
feature/r1-customer-asset-spine
feature/r1-work-orders
feature/r1-scheduling
feature/r1-field-execution
fix/*
chore/*
```

Rules:

-   no `develop` branch initially;
-   no direct commits to `main`;
-   no branch per entity;
-   focused commits;
-   self-review before PR;
-   CI before merge;
-   migration reviewed as part of the feature.

AI agents receive bounded tasks, not "generate all entities/modules."

------------------------------------------------------------------------

## 48. Engineering Quality Gates

A backend feature is not complete until relevant checks pass:

-   tenant isolation;
-   authorization;
-   request validation;
-   domain invariants;
-   DB constraints;
-   concurrency behavior;
-   idempotency where retryable;
-   transaction boundary;
-   audit implications;
-   ProblemDetails semantics;
-   query performance;
-   indexes where justified;
-   tests proportional to risk;
-   OpenAPI contract;
-   logging/trace behavior;
-   no architecture-boundary violation.

------------------------------------------------------------------------

## 49. Explicitly Rejected / Deferred Approaches

### Rejected Initially

-   microservices;
-   event sourcing;
-   generic repository over EF Core;
-   separate CQRS databases;
-   distributed transactions;
-   Redis as mandatory correctness infrastructure;
-   arbitrary cross-module DbSet/repository access;
-   controllers containing business logic;
-   domain entities exposed as API DTOs;
-   blind production `Database.Migrate()`;
-   Hangfire replacing transactional outbox;
-   client-side tenant ID as security authority;
-   last-write-wins for important concurrency;
-   mutable posted invoices;
-   generic CRUD endpoints for every lifecycle transition;
-   premature abstraction/framework building.

### Deferred Until Required

-   Redis;
-   vector database;
-   external OIDC/SSO provider;
-   microservice extraction;
-   separate read database;
-   advanced event bus;
-   full payment subsystem materialization;
-   high-scale distributed scheduling infrastructure.

------------------------------------------------------------------------

## 50. ADR Reconciliation

This approved baseline intentionally updates earlier architecture
decisions in two areas.

### Authentication

Earlier ADR guidance preferred a server-managed HttpOnly browser session
where topology allowed.

**Current approved product decision:** JWT-based authentication.

The security implementation must still avoid naive long-lived browser
tokens and must preserve future OIDC/SSO compatibility.

### Background Jobs

Earlier ADR-008 preferred .NET `BackgroundService`/Worker and
specifically avoided adding Hangfire merely for outbox dispatch.

**Current approved product decision:** Hangfire is included from day
one.

The transactional outbox remains mandatory. Hangfire is
execution/scheduling infrastructure, not the transactional guarantee.

A future ADR revision should mark the conflicting portions as superseded
rather than leaving contradictory canonical documents.

------------------------------------------------------------------------

## 51. Locked Backend Decisions

Unless deliberately superseded by a later ADR:

1.  Classic Clean Architecture physical solution.
2.  Modular Monolith deployment/module ownership.
3.  Pragmatic DDD.
4.  Logical CQRS, same primary DB.
5.  MediatR everywhere in the application dispatch path.
6.  Aggregate-specific repositories.
7.  EF Core DbContext as Unit of Work.
8.  EF Core Code First.
9.  Domain Events.
10. Transactional Outbox.
11. Hangfire from day one.
12. Redis only when measured/justified.
13. FluentValidation + domain invariants + DB constraints.
14. AutoMapper + DTOs.
15. Central exception handling + ProblemDetails.
16. ASP.NET Core Controllers.
17. SQL Server 2025.
18. Tenant isolation from day one.
19. JWT authentication.
20. Scoped server-side RBAC/capability authorization.
21. `rowversion`/explicit concurrency where appropriate.
22. ResourceScheduleGuards for R1 scheduling concurrency.
23. Object-storage abstraction.
24. SignalR where realtime is operationally useful.
25. OpenTelemetry-aligned observability.
26. AI provider abstraction with deterministic systems authoritative.
27. Performance-by-design + measure + optimize.
28. Pragmatic SOLID + KISS + Rule of Three.
29. Controlled migration review; no blind production auto-migrate.
30. Foundation first, then vertical slices.

------------------------------------------------------------------------

## 52. Final Architecture Position

Servexa backend should demonstrate enterprise-grade correctness without
enterprise-grade ceremony for its own sake.

``` text
Pragmatic DDD
      +
Clean Architecture
      +
Modular Monolith
      +
Logical CQRS / MediatR
      +
EF Core Code First
      +
Tenant Isolation
      +
Explicit Concurrency
      +
Transactional Outbox
      +
Hangfire
      +
Reliable Offline Command Semantics
      +
Measured Performance
      +
Strong Testing / Observability
      =
Servexa Backend
```

The architecture deliberately keeps deployment simple while protecting
boundaries that matter. If Servexa later needs service extraction, the
existing module ownership, contracts, outbox and integration boundaries
provide a controlled path without paying the operational cost of
microservices during R1.

This document is the **APPROVED BACKEND BASELINE**. Material deviations
require an explicit architecture decision/ADR rather than ad-hoc
implementation changes.
