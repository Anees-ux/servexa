---
trigger: always_on
---

# SERVEXA ENGINEERING RULES

You are implementing Servexa, an enterprise multi-tenant Field Service Management / Field Service Operations SaaS.

Implement ONLY the current bounded task. Do not redesign the approved product or architecture.

## 1. SOURCE OF TRUTH

`/docs` contains Servexa's canonical specifications.

Before coding:
- Identify and read documents relevant to the current task.
- Inspect existing implementation before editing.
- Do not rely only on memory/chat.
- Never invent missing business rules.
- Preserve approved terminology/domain distinctions.
- If code conflicts with docs, report it instead of silently deviating.
- If docs conflict, the newer explicitly APPROVED BASELINE supersedes older conflicting guidance.
- Never modify canonical docs unless explicitly instructed.

For EF/entities/migrations ALWAYS inspect:
- `/docs/06-data/Servexa_Logical_Data_Model_v1.1_Approved_Baseline.md`
- `/docs/06-data/Servexa_Physical_Data_Model_v1.1_Approved_Baseline.md`
- `/docs/05-architecture/Servexa_Engineering_Architecture_and_Backend_Standards_v1.0_Approved_Baseline.md`

For backend/domain work inspect relevant `/docs/01-domain`, `02-product`, `04-domain-validation`, `05-architecture`, `06-data`.

For frontend work ALWAYS inspect:
- `/docs/03-ux/Servexa_UX_Information_Architecture_v1.0.md`
- `/docs/05-architecture/Servexa_Frontend_Architecture_and_Design_System_v1.0_Approved_Baseline.md`
plus relevant feature/domain documents.

Approved baselines supersede conflicting old ADR guidance:
- JWT supersedes old browser-session preference.
- Hangfire day-one supersedes conflicting worker-only guidance.
- Transactional Outbox remains required; Hangfire does not replace it.

## 2. TASK SCOPE

Work ONLY on the assigned task.

Do not:
- implement future modules/features;
- refactor unrelated code;
- generate all entities at once;
- create speculative abstractions;
- change architecture without approval;
- update packages unnecessarily;
- modify unrelated files.

Choose the smallest complete production-quality change.

When complete, STOP. Never automatically start the next task.

## 3. BACKEND BASELINE

Use:
- .NET 10 / C# 14
- ASP.NET Core Controllers
- SQL Server 2025
- EF Core Code First
- Clean Architecture
- Modular Monolith
- Pragmatic DDD
- Logical CQRS, same DB
- MediatR
- aggregate-specific repositories
- DbContext as Unit of Work
- FluentValidation + domain invariants + DB constraints
- AutoMapper + DTOs
- ProblemDetails
- Domain Events + Transactional Outbox
- Hangfire
- SignalR where useful
- OpenTelemetry-aligned observability
- object-storage abstraction
- Redis only when measured/justified

Projects:
- Servexa.Domain
- Servexa.Application
- Servexa.Infrastructure
- Servexa.Api

Preserve module ownership within these layers.

Do NOT introduce microservices, Event Sourcing, separate CQRS DBs, project-per-entity architecture, or generic IRepository<T> wrappers around EF Core.

## 4. DOMAIN FIDELITY

Never collapse:

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

Implementation convenience must not corrupt the domain model.

## 5. CODE FIRST / DATABASE
Required workflow:
Approved Physical Model
→ C# entity/aggregate
→ IEntityTypeConfiguration<T>
→ EF migration
→ inspect migration/SQL
→ apply dev DB
→ verify constraints/indexes/FKs

Implement incrementally by assigned module/slice.

Follow approved physical model including where applicable:
- TenantId on tenant-owned rows
- tenant-safe FKs/relationships
- approved UUIDv7-compatible IDs
- datetime2(3)
- decimal(19,4) money
- decimal(18,4) quantity
- binary(32) hashes
- rowversion for concurrency-sensitive roots
- approved SQL schemas

Never manually change DB instead of proper migrations.
Never weaken constraints to make code pass.
Never blindly use Database.Migrate() for production.
Treat migrations as reviewed source artifacts.

## 6. TENANCY / SECURITY

Tenant isolation is a SECURITY boundary.

Never trust TenantId solely from client input.
Use validated authenticated server context.

Tenant isolation applies to commands, queries, repositories, FKs, unique constraints, jobs, Outbox handlers, caches, SignalR and storage.

Authentication is JWT-based:
- short-lived access token
- refresh lifecycle/rotation
- revocation/session tracking
- server-authoritative authorization
- future OIDC/SSO compatibility

Never commit secrets.

Frontend permission checks are UX only; backend authorization is authoritative.

## 7. DDD / CQRS / EF

Use pragmatic DDD.
Put real business invariants in appropriate domain behavior.

Avoid anemic models, giant aggregates, unnecessary Value Objects and speculative abstractions.

Prefer intention commands such as:
CompleteWorkOrder
RescheduleBooking
RecordPartUsage
PostInvoice

over generic UpdateEntity.

Commands mutate state; queries do not.
Queries may use optimized projections.
Use MediatR consistently.

Use aggregate-specific repositories.
DbContext is Unit of Work.

For reads:
- project required fields only;
- AsNoTracking where appropriate;
- paginate large results;
- avoid N+1;
- avoid unnecessary Include graphs;
- inspect important SQL;
- index real filter/order patterns.

Correctness → measure → optimize.
## 8. VALIDATION / CONCURRENCY / IDEMPOTENCY

Validation:
Request → FluentValidation
Business rules → Domain
Durable invariants → SQL constraints

Never rely on frontend validation.

Do not silently use last-write-wins.

For R1 scheduling concurrency follow approved ResourceScheduleGuards:
- deterministic ResourceId lock order;
- re-query overlap inside transaction;
- reject invalid overlap;
- persist after validation.

Retryable/duplicate-sensitive operations need durable idempotency, especially offline commands, part usage, webhooks and callbacks.

## 9. OUTBOX / HANGFIRE

Outbox = transactional guarantee that required post-commit work is durably recorded.
Hangfire = background execution/scheduling.

They are not interchangeable.

Never replace required Outbox persistence with direct Hangfire enqueue where a crash could lose work.

Handlers should tolerate duplicate execution where relevant.

## 10. INVENTORY / FINANCE

Reservation != Consumption.
InventoryMovement is an immutable ledger concept.

Offline physical inventory events must not be silently erased; use reconciliation.

Posted invoices are immutable.
Never edit a posted invoice.
Use approved correction/credit/rebill flow.

Correctness beats optimistic convenience.

## 11. API

Use thin ASP.NET Core Controllers.
Use DTOs; never expose EF/domain entities directly.

Prefer intention-revealing lifecycle endpoints over generic CRUD.

Use centralized ProblemDetails.
Never expose stack traces, SQL internals, secrets or sensitive internals.
Preserve trace/correlation IDs.

OpenAPI is the frontend contract source.

## 12. FRONTEND BASELINE

Use:
- React 19.x + strict TypeScript + Vite
- TanStack Router
- TanStack Query
- React local state first
- Zustand only when justified
- React Hook Form + Zod
- source-owned shadcn-style accessible primitives
- Tailwind + semantic CSS variables
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

Do not add nuqs initially.
Router owns URL state.
Do not duplicate Query data in Zustand.
SignalR updates/invalidates Query cache; no second truth store.

## 13. DESIGN / THEMES

Visual direction: Industrial precision + calm enterprise SaaS.

Avoid generic AI-dashboard styling, gratuitous gradients, glassmorphism-heavy UI, excessive rounded cards, random colors/shadows and decorative animation.

Architecture:
Accessible Primitive
→ Servexa Design-System Component
→ Feature Component

Use semantic design tokens.
No feature hardcoded theme HEX.
No `!important` as normal architecture.

Theme resolution:
Application Default → Tenant Default → User Override

Brand Theme and Light/Dark/System are separate.
Use one semantic CSS-variable contract.
No giant CSS swapping or reload for theme changes.
Operational status meaning/contrast must remain stable.

## 14. FRONTEND STATE / OFFLINE

State ownership:
Server → TanStack Query
URL → TanStack Router
Forms → React Hook Form
Local UI → React
Cross-cutting UI → Zustand only if justified
Offline → Dexie

Do not blindly optimistic-update financial posting, inventory consumption, scheduling, irreversible transitions or permission-sensitive commands.

Offline R1 = assignment-scoped technician execution, not full tenant replication.

Service Worker primarily caches app shell/static assets.
Do not broadly cache authenticated APIs.
Never store auth tokens in IndexedDB.

Offline commands require explicit IDs, idempotency, retry and conflict state.
Never silently overwrite conflicts.

## 15. ACCESSIBILITY / QUALITY

Target WCAG 2.2 AA.

Require semantic HTML, keyboard access, visible focus, labels/errors, dialog focus, touch targets, reduced-motion support, valid contrast and non-color-only statuses.

Use semantic tables normally; ARIA grid only for genuine interactive grids.

Write clear production code.
Use pragmatic SOLID + KISS + Rule of Three.

Avoid duplicate rules, magic values, dead code, swallowed exceptions, empty catches, god classes/components, circular dependencies, service locator and mutable global state.

Never solve problems by:
- disabling strict TypeScript;
- unnecessary `any`;
- suppressing warnings without cause;
- weakening nullability;
- bypassing validation/security;
- removing DB constraints;
- hardcoding tenant/user IDs;
- manually editing generated API code;
- manually altering DB;
- using CSS hacks.

Fix root causes.

## 16. PACKAGES / TESTS

Before adding a package verify need, maintenance, compatibility, license, duplication and cost.
Do not update packages unless required/approved.
Do not manually edit generated files.

Testing is proportional to risk.

Strongly test:
- tenant isolation
- authorization
- scheduling races
- concurrency/idempotency
- offline replay/conflicts
- duplicate stock consumption
- invoice immutability
- Outbox retry/crash
- duplicate webhooks

Never remove/weaken valid tests just to make code pass.

## 17. COMMAND / GIT SAFETY

Do not perform destructive actions without explicit approval:
- DB deletion/reset
- migration reset
- force push
- discarding unrelated changes
- production deployment
- cloud changes
- secret changes

Do not run broad expensive builds automatically unless current task requires them; prefer focused verification.

`main` is stable.
Never develop features directly on main.

Use short-lived:
- feature/*
- fix/*
- chore/*

No develop branch unless approved later.
No branch per entity.
Never commit secrets.
Keep commits focused.

## 18. AI COMPLETION PROTOCOL

Inspect actual files before making claims.
Do not guess.

Never claim build/test/migration/package/API success unless actually verified.

Do not overengineer or rewrite working unrelated code.

For large work, execute only the approved bounded stage.

If an unrelated issue is found, report it; fix only if it blocks/current scope requires it.

At the end of EVERY task report:
1. What changed
2. Files created/modified
3. DB/migration impact
4. Tests/verification actually performed
5. Warnings/unresolved issues
6. Suggested commit message
7. Is the assigned task fully complete?
Then STOP.
Never start the next task without explicit approval.
Goal: correct, secure, maintainable enterprise-quality Servexa — not maximum code generation
