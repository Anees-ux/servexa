# Servexa — OpenCode Agent Instructions

You are working on Servexa, an enterprise multi-tenant Field Service Management / Field Service Operations SaaS.

These instructions apply to all OpenCode work in this repository.

## 1. Canonical Source of Truth

The `/docs` directory contains the approved Servexa specifications.

Before reviewing or implementing a task:

1. Inspect the existing repository/code first.
2. Identify and read the canonical documents relevant to the task.
3. Do not rely only on previous session context or memory.
4. Do not invent missing business rules.
5. Preserve approved Servexa terminology and domain boundaries.
6. If code conflicts with an approved document, report the conflict.
7. If documents conflict, prefer the newer explicitly APPROVED BASELINE where it intentionally supersedes older guidance.
8. Never modify `/docs` unless explicitly instructed.

For EF Core/entities/database/migrations always inspect:

- `/docs/06-data/Servexa_Logical_Data_Model_v1.1_Approved_Baseline.md`
- `/docs/06-data/Servexa_Physical_Data_Model_v1.1_Approved_Baseline.md`
- `/docs/05-architecture/Servexa_Engineering_Architecture_and_Backend_Standards_v1.0_Approved_Baseline.md`

For backend/domain tasks inspect relevant documents under:

- `/docs/01-domain`
- `/docs/02-product`
- `/docs/04-domain-validation`
- `/docs/05-architecture`
- `/docs/06-data`

For frontend tasks always inspect:

- `/docs/03-ux/Servexa_UX_Information_Architecture_v1.0.md`
- `/docs/05-architecture/Servexa_Frontend_Architecture_and_Design_System_v1.0_Approved_Baseline.md`

plus relevant product/domain/data documents.

Approved newer baselines supersede conflicting older guidance.

Important reconciliations:
- JWT supersedes the older browser-session preference.
- Hangfire day-one supersedes conflicting older worker-only guidance.
- Transactional Outbox remains required; Hangfire does not replace it.

## 2. Default OpenCode Behavior

For review, investigation, explanation, or architecture-analysis requests:

- operate read-only;
- inspect repository and relevant docs;
- do NOT modify files unless explicitly asked;
- report only material findings;
- avoid speculative refactoring.

For implementation requests:

- modify files only when explicitly assigned implementation work;
- implement only the current bounded task;
- inspect relevant docs before editing;
- inspect existing implementation before creating new code;
- stop when the assigned task is complete.

Never automatically start the next Servexa task.

## 3. Scope Discipline

Do NOT:

- implement future modules/features;
- refactor unrelated code;
- generate all entities at once;
- introduce speculative abstractions;
- redesign approved architecture;
- update packages unnecessarily;
- modify unrelated files;
- perform cleanup outside current scope.

Prefer the smallest complete production-quality change satisfying the task.

## 4. Backend Architecture

Approved baseline:

- .NET 10 / C# 14
- ASP.NET Core Controllers
- SQL Server 2025
- EF Core Code First
- Clean Architecture physical solution
- Modular Monolith module ownership
- pragmatic DDD
- logical CQRS using the same primary DB
- MediatR
- aggregate-specific repositories
- DbContext as Unit of Work
- FluentValidation + domain invariants + DB constraints
- AutoMapper + explicit DTOs
- centralized ProblemDetails
- Domain Events + Transactional Outbox
- Hangfire
- SignalR where useful
- OpenTelemetry-aligned observability
- object-storage abstraction
- Redis only when measured and justified

Physical projects:

- Servexa.Domain
- Servexa.Application
- Servexa.Infrastructure
- Servexa.Api

Do not introduce microservices, Event Sourcing, separate CQRS databases, project-per-entity architecture, or generic IRepository<T> abstractions.

## 5. Domain Integrity

Never collapse these concepts:

Account != Contact
Site != Address
Asset != Equipment Model
Service Request != Work Order
Work Order != Booking
Booking != Resource Assignment
Operational Completion != Financial Closure
Resource != User Account

Agreement != Service Plan != Entitlement != Warranty != SLA != Maintenance Plan

Product != Inventory Item != Part Usage != Asset/Component

Physical Stock != Operational Usage != Customer Charge != Job Cost

Estimate/Quote != Work Order

Task != Checklist != Inspection != Form Template != Form Response

Implementation convenience must never corrupt the approved domain model.

## 6. EF Core / Database

Required Code-First workflow:

Approved Physical Model
→ C# entity/aggregate
→ IEntityTypeConfiguration<T>
→ migration
→ inspect migration/SQL
→ apply development DB
→ verify constraints/indexes/FKs

Implement incrementally.

Follow approved physical model, including where applicable:

- TenantId on tenant-owned rows
- tenant-safe relationships/FKs
- approved UUIDv7-compatible IDs
- datetime2(3)
- decimal(19,4) money
- decimal(18,4) quantity
- binary(32) hashes
- rowversion where approved
- approved SQL schemas

Never:

- manually change the DB instead of using a proper migration;
- weaken constraints just to make code pass;
- silently rewrite shared migrations;
- use Database.Migrate() blindly as production deployment strategy.

Migrations are reviewed source artifacts.

## 7. Security / Multi-Tenancy

Tenant isolation is a security boundary.

Never trust TenantId merely because it came from client input.

Tenant context must come from validated authenticated server context.

Tenant isolation applies to relevant:

- commands and queries
- repositories
- FKs and unique constraints
- jobs
- Outbox handlers
- caches
- SignalR
- object storage

Authentication is JWT-based with:

- short-lived access tokens
- refresh lifecycle/rotation
- revocation/session tracking
- server-authoritative authorization
- future OIDC/SSO compatibility

Never commit secrets.

Frontend authorization is UX only.
Backend authorization is authoritative.

## 8. DDD / CQRS / EF Quality

Use pragmatic DDD.

Business invariants belong in appropriate domain behavior.

Avoid:

- anemic domain models
- giant aggregates
- unnecessary Value Objects
- speculative abstractions

Prefer intention-revealing commands such as:

- CompleteWorkOrder
- RescheduleBooking
- RecordPartUsage
- PostInvoice

over generic UpdateEntity commands.

Commands mutate state.
Queries do not.

Use optimized projections for reads where appropriate.

Use aggregate-specific repositories.
DbContext is Unit of Work.

For reads:

- project required fields only;
- use AsNoTracking where appropriate;
- paginate large results;
- avoid N+1;
- avoid unnecessary Include graphs;
- inspect important SQL.

Correctness first. Measure before optimizing.

## 9. Validation / Concurrency / Idempotency

Validation layers:

Request/application → FluentValidation
Business invariants → Domain
Durable relational invariants → SQL

Never rely on frontend validation.

Do not silently use last-write-wins.

For approved R1 scheduling concurrency use ResourceScheduleGuards and follow the canonical physical model.

Duplicate/retry-sensitive operations require durable idempotency, especially:

- offline commands
- part usage
- webhooks
- external callbacks

Do not use in-memory duplicate prevention as the durable guarantee.

## 10. Outbox / Hangfire

Transactional Outbox provides the durable transactional guarantee.

Hangfire provides background execution/scheduling.

They are not interchangeable.

Never replace required Outbox persistence with direct Hangfire enqueue where a crash could lose required work.

Handlers should tolerate duplicate execution where applicable.

## 11. Inventory / Financial Integrity

Reservation != Consumption.

InventoryMovement is an immutable ledger concept.

Offline physical inventory events must not be silently discarded because server stock differs; use approved reconciliation behavior.

Posted invoices are immutable.

Never implement direct editing of a posted invoice.

Use approved correction/credit/rebill flows.

## 12. Frontend Architecture

Use:

- React 19.x
- strict TypeScript
- Vite
- TanStack Router
- TanStack Query
- React local state first
- Zustand only when justified
- React Hook Form + Zod
- source-owned accessible shadcn-style primitives
- Tailwind + semantic CSS Custom Properties
- Lucide
- TanStack Table
- TanStack Virtual only when measured
- generated OpenAPI client
- SignalR
- PWA + Dexie for approved offline scope
- Storybook
- Vitest + React Testing Library
- MSW
- Playwright

State ownership:

Server → TanStack Query
URL → TanStack Router
Forms → React Hook Form
Local UI → React
Cross-cutting UI → Zustand only when justified
Offline operational data → Dexie

Do not duplicate Query server state in Zustand.

## 13. Design System / Accessibility

Visual direction:

Industrial precision + calm enterprise SaaS.

Avoid generic AI-dashboard styling, excessive gradients, glassmorphism, random colors/shadows, excessive rounded cards and decorative animation.

Use:

Accessible Primitive
→ Servexa Design-System Component
→ Feature Component

Use semantic design tokens.

No hardcoded theme colors in feature logic.
Do not use !important as normal styling architecture.

Target WCAG 2.2 AA.

Require keyboard accessibility, visible focus, labels/errors, valid dialog focus, sufficient contrast and statuses that do not depend only on color.

## 14. Offline

R1 offline scope is assignment-scoped technician execution, not full tenant replication.

Do not broadly cache authenticated APIs.

Do not store authentication tokens in IndexedDB.

Offline commands require explicit identity, idempotency, retry and conflict state.

Never silently overwrite conflicts.

## 15. Engineering Quality

Use pragmatic SOLID + KISS + Rule of Three.

Avoid:

- duplicate business logic
- magic values
- dead code
- swallowed exceptions
- empty catch blocks
- god services/components
- circular dependencies
- service locator
- mutable global state
- unnecessary reflection

Never fix problems by:

- disabling TypeScript strictness
- unnecessary `any`
- suppressing warnings without cause
- weakening nullability
- bypassing validation/authorization
- removing valid DB constraints
- hardcoding tenant/user IDs
- manually editing generated API code
- manually altering the database
- CSS hacks

Fix root causes.

## 16. Packages / Testing

Do not add or update packages without a justified task requirement.

Before adding a dependency verify need, maintenance, compatibility, license, duplication and cost.

Never manually edit generated files.

Testing is proportional to risk.

Strongly test high-risk behavior including:

- tenant isolation
- authorization
- scheduling races
- concurrency/idempotency
- offline replay/conflicts
- duplicate stock consumption
- posted invoice immutability
- Outbox retry/crash behavior
- duplicate webhooks

Never weaken valid tests just to make code pass.

## 17. Git / Command Safety

`main` is stable.

Never perform feature development directly on main.

Use short-lived:

- feature/*
- fix/*
- chore/*

No develop branch unless explicitly approved.
No branch per entity.
Keep commits focused.
Never commit secrets.

Never perform destructive operations without explicit approval, including:

- DB deletion/reset
- migration reset
- force push
- discarding unrelated changes
- production deployment
- cloud-resource modification
- secret modification

Do not run broad expensive builds automatically unless required by the assigned task.
Prefer focused verification.

## 18. OpenCode Review Policy

When asked to review work produced by another agent:

- default to read-only;
- read relevant canonical docs;
- inspect actual diff/code;
- identify material correctness, architecture, security, tenancy, data-integrity, concurrency and test issues;
- distinguish blockers from optional improvements;
- do not modify code during review;
- do not recommend stylistic rewrites without engineering value;
- do not create endless review loops.

If no material issue exists, say so clearly.

## 19. Completion Protocol

Never claim something was verified unless it was actually verified.

At the end of an implementation task report:

1. What changed
2. Files created/modified
3. Database/migration impact
4. Tests/verification actually performed
5. Warnings/unresolved issues
6. Suggested commit message
7. Whether the assigned task is fully complete

Then STOP.

Do not start the next task automatically.

Do not declare COMPLETE if required work remains, verification failed, a required migration was not inspected, or an architectural uncertainty remains.

The objective is correct, secure, maintainable enterprise-quality Servexa — not maximum code generation.