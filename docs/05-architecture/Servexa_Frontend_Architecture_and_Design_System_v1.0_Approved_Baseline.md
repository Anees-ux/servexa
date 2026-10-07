# Servexa Frontend Architecture and Design System v1.0 --- Approved Baseline

**Product:** Servexa\
**Status:** APPROVED BASELINE\
**Frontend:** React + TypeScript + Vite SPA/PWA\
**Backend Contract:** ASP.NET Core OpenAPI\
**Scope:** R0 Foundation through R1 Core Service Spine, with controlled
seams for later releases

------------------------------------------------------------------------

## 1. Purpose and Quality Bar

This is the canonical frontend architecture baseline for Servexa, an
enterprise Field Service Management / Field Service Operations SaaS.

The frontend must feel like a purpose-built operational product, not a
generic admin dashboard. It must preserve Servexa's domain semantics,
multi-tenant security model, role-specific workflows, scheduling
complexity, technician offline execution, accessibility, realtime
behavior and long-term maintainability.

Implementation strategy:

**Foundation First → Controlled Vertical Slices**

Core principles:

-   Domain fidelity over generic CRUD abstractions.
-   Explicit architecture over clever abstractions.
-   Feature boundaries over technical dumping grounds.
-   Server, URL, form, local UI and offline state have distinct owners.
-   Frontend authorization is UX; backend authorization is
    authoritative.
-   Semantic design tokens are the visual contract.
-   Accessibility is foundational.
-   Offline is an explicit technician operating mode, not generic API
    caching.
-   SignalR updates/invalidate canonical server state; it does not
    create a second truth.
-   Measure before optimizing.
-   No hardcoded feature colors, `!important` theme hacks, or generic
    admin-template dependency.
-   Use pragmatic SOLID, KISS and Rule of Three; avoid speculative
    abstraction.

------------------------------------------------------------------------

## 2. Approved Technology Direction

  -----------------------------------------------------------------------
  Concern                             Approved Direction
  ----------------------------------- -----------------------------------
  Application                         React SPA / PWA

  Language                            TypeScript strict

  Build                               Vite

  React                               React 19.x; exact compatible
                                      versions verified at implementation

  React Compiler                      Use only after toolchain/dependency
                                      compatibility verification

  Routing                             TanStack Router

  Server state                        TanStack Query

  URL/filter state                    TanStack Router typed search
                                      parameters

  Local UI state                      React state first

  Cross-cutting client state          Zustand only where genuinely
                                      justified

  Forms                               React Hook Form + Zod

  Component foundation                shadcn-style source-owned
                                      components over accessible
                                      primitives

  Styling                             Tailwind CSS + semantic CSS Custom
                                      Properties

  Icons                               Lucide

  Tables                              TanStack Table

  Virtualization                      TanStack Virtual when measured need
                                      exists

  API client                          Generated from ASP.NET Core OpenAPI

  Authentication                      JWT-based authentication

  Offline                             PWA shell + Dexie/IndexedDB +
                                      explicit command outbox

  Realtime                            SignalR → TanStack Query cache

  Unit/component tests                Vitest + React Testing Library

  API mocking                         MSW

  E2E                                 Playwright

  Accessibility                       WCAG 2.2 AA + automated axe
                                      coverage on critical flows

  Component lab                       Storybook from foundation

  Visual regression                   Focused Playwright screenshots

  Motion                              CSS transitions first

  Charts                              Deferred until required

  Error tracking vendor               None in R1

  Localization                        English-first, i18n-ready; package
                                      deferred

  Themes                              Tenant-selectable curated
                                      multi-theme system

  Full white label                    Deferred; architecture remains
                                      ready
  -----------------------------------------------------------------------

Exact package versions are implementation-time decisions and must be
compatibility, maintenance and licensing checked before installation.

------------------------------------------------------------------------

## 3. Frontend Structure

``` text
src/
├── app/
│   ├── bootstrap/
│   ├── providers/
│   ├── router/
│   ├── auth/
│   └── error-handling/
├── features/
│   ├── customers/
│   ├── sites/
│   ├── assets/
│   ├── service-requests/
│   ├── work-orders/
│   ├── scheduling/
│   ├── field-execution/
│   ├── inventory/
│   ├── billing/
│   ├── notifications/
│   └── administration/
├── workspaces/
│   ├── support/
│   ├── dispatcher/
│   ├── technician/
│   ├── service-manager/
│   ├── warehouse/
│   ├── finance/
│   ├── commercial/
│   └── admin/
├── shared/
│   ├── api/
│   ├── auth/
│   ├── components/
│   ├── design-system/
│   ├── hooks/
│   ├── utilities/
│   ├── types/
│   └── validation/
└── styles/
    ├── tokens/
    ├── themes/
    └── globals/
```

### Boundary Rules

-   Features own domain-facing UI behavior.
-   Cross-feature use goes through intentional public exports.
-   Workspaces compose role layouts/navigation/routes; they do not
    duplicate business logic.
-   `shared/` contains genuinely reusable infrastructure only and must
    not become a dumping ground.
-   No deep imports into unrelated feature internals.
-   No generic CRUD engine as the default product architecture.

------------------------------------------------------------------------

## 4. State Ownership

  State                                 Owner
  ------------------------------------- -------------------------------
  API/server data                       TanStack Query
  Search/filter/sort/page/deep links    TanStack Router search params
  Component-local interaction           React
  Forms                                 React Hook Form
  Small cross-cutting client concerns   Zustand only if justified
  Offline technician data               Dexie repositories
  Pending offline mutations             Explicit command outbox

Decisions:

-   Do **not** install `nuqs` initially; TanStack Router owns typed URL
    state.
-   Do not duplicate Query data into Zustand.
-   Do not create a parallel SignalR business-state store.
-   React Context is not a general-purpose state database.

------------------------------------------------------------------------

## 5. Routing

Use TanStack Router with meaningful lazy-loaded workspace/feature
boundaries.

Representative routes:

``` text
/customers
/customers/$customerId
/sites/$siteId
/assets/$assetId
/service-requests/$requestId
/work-orders/$workOrderId
/dispatch
/technician/assignments
/inventory
/billing/invoices/$invoiceId
/admin/appearance
```

Shareable filters, date windows, paging and scheduling views should use
typed search parameters where appropriate.

Route permission metadata may improve UX, but backend authorization
remains authoritative.

------------------------------------------------------------------------

## 6. Authentication and Tenant Security

### Locked Decision: JWT

Servexa uses JWT-based authentication. This supersedes the earlier
browser-session preference where the two conflict.

Production requirements:

-   short-lived access token;
-   refresh-token lifecycle and rotation;
-   server-side session/revocation tracking;
-   logout/revocation support;
-   explicit expiry/refresh handling;
-   replay-risk mitigation;
-   centralized API authentication behavior;
-   permissions/scopes from trusted backend context.

The exact browser storage/transport mechanism for refresh credentials
requires a focused security design before implementation. Long-lived
sensitive credentials must not be casually persisted in
JavaScript-readable storage.

A client-supplied TenantId is never authority. The backend validates
tenant membership/context from authenticated identity and enforces
isolation on every protected operation.

Frontend permissions may hide/disable actions and protect navigation,
but are not security enforcement.

------------------------------------------------------------------------

## 7. API Contract and Errors

ASP.NET Core OpenAPI is the contract source.

``` text
ASP.NET Core
    ↓
OpenAPI
    ↓
Generated TypeScript client/types
    ↓
Feature API/query layer
    ↓
TanStack Query
    ↓
UI
```

Generated code lives in an obvious generated directory such as:

``` text
src/shared/api/_generated/
```

It is never manually edited. The exact generator, including
`@hey-api/openapi-ts` if selected, is verified at implementation.

The client must classify ASP.NET Core ProblemDetails/validation outcomes
including:

-   400 validation/request errors;
-   401 authentication;
-   403 authorization;
-   404 not found;
-   409 concurrency/business conflict;
-   422 domain validation where used;
-   network/offline failure;
-   unexpected 5xx.

Preserve backend correlation/trace identifiers for diagnostics.

------------------------------------------------------------------------

## 8. Error Handling and Observability

### Locked Decision: No dedicated frontend vendor in R1

R1 still includes:

-   application error boundary;
-   route-level recovery where useful;
-   consistent ProblemDetails UX;
-   correlation IDs;
-   safe diagnostic logging;
-   no sensitive data in browser logs;
-   integration seam for future Sentry/Application Insights/other
    tooling.

Backend observability remains aligned with Servexa's OpenTelemetry
direction.

------------------------------------------------------------------------

## 9. Forms and Validation

Use React Hook Form + Zod.

Validation remains layered:

``` text
Frontend UX validation
    ↓
API validation
    ↓
Domain invariants
    ↓
Database constraints
```

Requirements:

-   map server validation cleanly to forms/fields;
-   protect meaningful unsaved changes;
-   use purpose-built forms for complex workflows;
-   do not build a generic dynamic-form engine prematurely;
-   associate errors accessibly;
-   confirm destructive operations appropriately.

------------------------------------------------------------------------

## 10. Tables

Use TanStack Table for complex enterprise tables.

Support as required:

-   sorting;
-   filtering;
-   server pagination;
-   column visibility;
-   row actions;
-   selection;
-   loading/empty/error states;
-   responsive degradation;
-   keyboard accessibility.

Use TanStack Virtual only when scale/render measurement justifies it.

Prefer native semantic `<table>` for ordinary tables. ARIA `grid` is
reserved for genuinely interactive spreadsheet/grid widgets with the
required keyboard interaction model.

------------------------------------------------------------------------

## 11. Design System

Three layers:

``` text
Accessible Primitive
    ↓
Servexa Design-System Component
    ↓
Feature Component
```

Examples:

``` text
Primitive:
Dialog / Popover / Select

Servexa DS:
ConfirmDialog / StatusBadge / EntityHeader / DataTable / EmptyState / ErrorState / PageToolbar

Feature:
WorkOrderStatusBadge / AssetServiceHistory / DispatchBookingCard
```

shadcn-style components are source-owned foundations, **not Servexa's
visual identity**.

Do not install a generic admin dashboard and rebrand it.

### Visual Personality

**Industrial precision + calm enterprise SaaS**

Use:

-   controlled neutral surfaces;
-   strong hierarchy;
-   precise alignment;
-   excellent typography;
-   restrained shadows;
-   subtle borders;
-   small/medium radii;
-   dense operational screens where appropriate;
-   purposeful whitespace;
-   clear statuses;
-   minimal purposeful motion.

Avoid default decorative gradients, glassmorphism-heavy shells and
gimmicky dashboard visuals.

------------------------------------------------------------------------

## 12. Design Tokens

Token hierarchy:

``` text
Primitive Palette
    ↓
Semantic Tokens
    ↓
Component Tokens (only when justified)
    ↓
Components
```

Representative semantic contract:

``` css
--background
--surface
--surface-elevated
--foreground
--foreground-secondary
--foreground-muted
--border
--border-strong
--primary
--primary-hover
--primary-foreground
--accent
--accent-foreground
--focus-ring
--success
--success-foreground
--warning
--warning-foreground
--danger
--danger-foreground
--info
--info-foreground
```

Feature components consume semantic meaning, not arbitrary raw palette
colors.

Tokens may legitimately contain `px`, `rem` or other CSS values. The
rule is no arbitrary visual constants scattered through feature
code---not a literal ban on pixels.

------------------------------------------------------------------------

## 13. Premium Multi-Theme System

### Locked Decision

Servexa will support a tenant-selectable premium multi-theme system
inspired by the useful product concept in Ajeero, while rejecting
Ajeero's legacy runtime stylesheet-swapping implementation.

The Ajeero audit established that Ajeero used 14 predefined themes
backed largely by Metronic/ABP Zero, dynamically injected precompiled
CSS, tenant/user preference hierarchy and page reload on theme changes.
Servexa keeps the useful hierarchy and premium choice, but uses semantic
CSS Custom Properties.

### R1 Goals

-   premium Servexa default theme;
-   multiple curated enterprise themes;
-   architecture capable of approximately 10--14 curated themes;
-   tenant admin selects default theme;
-   optional user override;
-   brand theme separate from Light/Dark/System;
-   instant switching without page reload;
-   one semantic token contract;
-   WCAG-compatible contrast;
-   Storybook theme preview;
-   focused visual regression;
-   no hardcoded feature colors;
-   no theme-specific `!important` patches.

Start by perfecting a representative set such as:

-   Servexa Default;
-   Ocean;
-   Graphite;
-   Emerald.

Then add further curated themes against the same contract.
Names/palettes are design decisions, not architecture commitments.

### Resolution Precedence

``` text
Application Default
    ↓
Tenant Default
    ↓
User Override
```

A null user override means inherit the tenant default.

### Brand Theme vs Appearance Mode

``` text
Brand Theme
├── Servexa
├── Ocean
├── Graphite
├── Emerald
└── ...

Appearance Mode
├── Light
├── Dark
└── System
```

This allows combinations such as `Ocean + Dark` without separate
monolithic CSS bundles.

### Persistence

Persist preference identifiers, not copies of every predefined token
dictionary.

Conceptual model:

``` text
TenantAppearanceSettings
- TenantId
- DefaultThemeKey
- DefaultColorMode

UserAppearancePreference
- UserId
- ThemeKey?       // null = inherit tenant
- ColorMode?      // null = inherit
```

Approved theme definitions remain version-controlled
frontend/design-system assets.

### Future White Label

Full arbitrary tenant white-label editing is deferred, but the engine
must later support:

-   tenant logo;
-   constrained brand seed/accent;
-   validated custom branding;
-   versioned approved overrides.

Do not expose unrestricted editing of every status/semantic token.

------------------------------------------------------------------------

## 14. Theme Boot and Flash Prevention

A non-sensitive resolved theme/mode identifier may be cached locally for
fast visual boot.

A minimal synchronous boot step may apply the last known safe appearance
before React mounts, preventing a flash of the wrong theme.

After authentication/bootstrap:

1.  retrieve authoritative tenant/user preferences;
2.  resolve precedence;
3.  update document theme;
4.  update the safe local appearance cache.

Appearance cache is not authentication/authorization state.

------------------------------------------------------------------------

## 15. Operational Status Colors

Do not repeat Ajeero-style hardcoded TypeScript HEX dictionaries.

Use semantic domain tokens:

``` css
--status-success-bg
--status-success-fg
--status-warning-bg
--status-warning-fg
--status-critical-bg
--status-critical-fg
--booking-confirmed-bg
--booking-confirmed-fg
--booking-conflict-bg
--booking-conflict-fg
--work-completed-bg
--work-completed-fg
--offline-bg
--offline-fg
```

Themes may harmonize these values but must preserve meaning and
contrast. Status must never depend on color alone.

------------------------------------------------------------------------

## 16. Storybook and Visual Regression

### Storybook: Foundation

Use Storybook for:

-   design-system components;
-   variants/interactions;
-   relevant loading/empty/error/permission states;
-   Light/Dark;
-   representative themes;
-   responsive states;
-   accessibility inspection.

Do not duplicate every feature screen in Storybook.

### Visual Regression: Yes

Use focused Playwright screenshots initially.

High-value surfaces:

-   application shell;
-   navigation;
-   core DS components;
-   forms;
-   data tables;
-   critical entity patterns;
-   Dispatch Board;
-   technician mobile shell;
-   Default Light/Dark;
-   selected visually divergent themes.

Do not multiply every screenshot by every theme. Use representative
matrices plus token/contrast validation.

------------------------------------------------------------------------

## 17. Dispatch / Scheduling Board

### Locked R1 Scope

R1 contains a professional Dispatch Board with:

-   day/week operational views;
-   technician/resource rows;
-   bookings;
-   unassigned-work queue;
-   filters;
-   availability;
-   conflict indicators;
-   booking create/edit;
-   explicit reschedule/reassign actions;
-   realtime updates;
-   adaptive fallback for smaller screens;
-   accessible non-drag operations.

### Advanced Drag & Drop: Deferred

Later stage:

-   drag to reassign;
-   drag to reschedule;
-   resize where domain-valid;
-   robust collision/conflict feedback;
-   accessible keyboard/non-pointer equivalents;
-   advanced virtualization if required.

Drag operations never bypass backend scheduling validation or
concurrency controls.

Do not assume keyboard drag emulation is always the best accessible
solution; explicit move/reschedule commands can provide a clearer
equivalent.

------------------------------------------------------------------------

## 18. Realtime

Use one intentional SignalR integration layer:

``` text
Backend event
    ↓
SignalR
    ↓
Frontend event adapter
    ↓
Invalidate/update relevant TanStack Query keys
    ↓
UI renders canonical cache
```

No parallel SignalR truth store.

Examples:

-   booking changed;
-   assignment changed;
-   work-order state changed;
-   dispatch conflict changed;
-   notification created.

Backend controls tenant/user event scope.

------------------------------------------------------------------------

## 19. Offline Technician PWA

Offline R1 is deliberately narrow:

> Technician execution for already-authorized, assignment-scoped work.

Possible local data:

-   current/near-term assignments;
-   permitted work-order/site/asset summary;
-   tasks/checklists;
-   permitted reference data;
-   draft notes;
-   observations;
-   pending part-usage commands;
-   evidence upload queue/metadata.

Do not build a full offline replica of CRM, billing, organization-wide
inventory or all customer history.

### Service Worker

Primarily cache:

-   application shell;
-   static assets;
-   controlled application updates.

Do **not** broadly cache authenticated API responses with generic
`NetworkFirst` rules.

Correctness/sensitive offline data belongs in explicit application/Dexie
repositories.

### Command Outbox

Offline writes carry:

-   client command ID;
-   idempotency key;
-   entity identifiers;
-   command type;
-   payload;
-   device/client timestamp;
-   retry state;
-   server outcome;
-   conflict state.

Server conflicts such as `409` become explicit reconciliation UX, never
silent overwrite.

### Local Security

-   no access/refresh tokens in IndexedDB;
-   assignment-scoped minimal data;
-   purge on logout/revocation according to policy;
-   explicit device/browser threat model.

IndexedDB is not assumed to provide native encryption. If
application-level encryption is required, design it deliberately.

------------------------------------------------------------------------

## 20. Evidence / Media Integrity

Field evidence needs a defined artifact policy.

If client-side compression/transformation occurs, distinguish:

-   original artifact if preservation is required;
-   transformed upload artifact;
-   metadata/provenance;
-   server hash/seal used for tamper evidence.

Image processing must not silently break evidence-integrity guarantees.

------------------------------------------------------------------------

## 21. Optimistic UI Policy

Optimistic UI is selective.

Suitable candidates may include low-risk reversible preferences.

Do not automatically optimistic-update:

-   financial posting;
-   inventory consumption;
-   scheduling changes with overlap/concurrency implications;
-   irreversible work-order lifecycle transitions;
-   permission-sensitive commands.

Use explicit pending states and update canonical Query state after
server acceptance for high-risk commands.

------------------------------------------------------------------------

## 22. Accessibility

Target **WCAG 2.2 AA**.

Requirements:

-   semantic HTML first;
-   full keyboard access;
-   visible focus;
-   correct accessible names;
-   labels/error associations;
-   contrast-safe themes;
-   status not color-only;
-   reduced-motion support;
-   correct dialog/popover focus;
-   field-friendly touch targets;
-   automated axe coverage for critical journeys;
-   manual keyboard review.

------------------------------------------------------------------------

## 23. Responsive / Role-Adaptive UX

Servexa is not desktop UI squeezed onto mobile.

### Dispatcher

Desktop-first, dense scheduling, queues, filters, keyboard/pointer
efficiency.

### Technician

Mobile-first, large touch targets, assignment focus, offline visibility,
evidence capture, explicit sync state.

### Service Manager

Desktop/tablet operational monitoring, exceptions, KPI summaries and
drill-down.

### Warehouse / Finance / Commercial / Admin

Role-specific density, actions and navigation.

On smaller screens, scheduling should provide an agenda/list alternative
rather than compressing the desktop board into unusability.

------------------------------------------------------------------------

## 24. Localization and Timezones

### Locked Decision

R1 is English-first and i18n-ready. Do not add an i18n package until a
real second-language requirement exists.

Prepare boundaries for:

-   dates;
-   times;
-   numbers;
-   currencies;
-   units;
-   locale-sensitive formatting.

Timezone behavior follows domain context, not one universal branch
timezone:

-   SLA/business calendar → policy/calendar timezone;
-   site service windows → site timezone;
-   resource shifts → resource/calendar timezone;
-   timestamp presentation → explicit product context/rule.

------------------------------------------------------------------------

## 25. Performance

Use:

-   route/feature splitting;
-   Query caching;
-   localized state;
-   server filtering/pagination;
-   virtualization when measured;
-   stable component boundaries;
-   React Compiler only after compatibility verification;
-   controlled bundle analysis;
-   media optimization;
-   profiling for Dispatch/large grids.

Avoid:

-   memoization everywhere;
-   global state as a performance workaround;
-   duplicated server state;
-   premature virtualization;
-   large admin/template frameworks.

Performance budgets should come from measured baseline builds.

------------------------------------------------------------------------

## 26. Testing

### Unit / Logic --- Vitest

Theme resolution, utilities, permissions, formatting, offline/outbox
logic and frontend transformations.

### Component --- React Testing Library

Forms, validation, permission states, error states, design-system
interactions and user-visible behavior.

### API --- MSW

Controlled API scenarios for tests/development.

### E2E --- Playwright

Critical lifecycle:

``` text
Login
→ Customer
→ Site
→ Asset
→ Service Request
→ Work Order
→ Schedule
→ Technician execution
→ Part usage
→ Completion
→ Invoice
```

High-risk additional coverage:

-   permissions;
-   concurrent booking conflict UX;
-   offline replay/conflict;
-   theme persistence;
-   JWT expiry/refresh;
-   posted invoice read-only behavior;
-   responsive technician workflow.

------------------------------------------------------------------------

## 27. Security Rules

-   No secrets in frontend source.
-   Never trust frontend permission checks.
-   Never trust TenantId solely because the browser sent it.
-   No unsanitized user HTML.
-   No sensitive data in browser logs.
-   No authentication tokens in offline operational databases.
-   CSP/security headers configured at deployment/backend edge.
-   Review dependencies before introduction.
-   Generated API code is transport code, not a security boundary.

------------------------------------------------------------------------

## 28. Package Governance

Before adding a dependency:

1.  Can React/platform/CSS already solve it?
2.  Is the problem recurring and material?
3.  Is the package maintained?
4.  Is licensing acceptable?
5.  Is toolchain compatibility verified?
6.  What is bundle/runtime cost?
7.  Does it introduce a competing state owner?
8.  Can it be replaced later?

Initial exclusions:

-   no `nuqs` beside TanStack Router without demonstrated need;
-   no motion library for basic transitions;
-   no paid visual-regression dependency initially;
-   no chart library until charts are required.

------------------------------------------------------------------------

## 29. Frontend Foundation Milestone

Create **Servexa Frontend Foundation** before broad feature work:

1.  Vite + React + strict TypeScript.
2.  Lint/format/repository conventions.
3.  TanStack Router.
4.  TanStack Query.
5.  JWT authentication infrastructure.
6.  OpenAPI client generation.
7.  ProblemDetails handling.
8.  Error boundaries.
9.  Semantic design tokens.
10. Initial curated themes.
11. Light/Dark/System.
12. Source-owned primitives.
13. Servexa design-system components.
14. Role-aware application shell.
15. Storybook.
16. Vitest + RTL.
17. MSW.
18. Playwright baseline.
19. Accessibility automation.
20. Visual-regression baseline.
21. PWA shell.
22. Dexie repository skeleton.
23. Offline command-outbox skeleton.
24. SignalR integration seam.
25. CI quality gates.
26. Architecture/developer README.

Foundation establishes necessary contracts; it does not mean prebuilding
every future abstraction.

------------------------------------------------------------------------

## 30. Vertical Slice Delivery

After foundation:

``` text
Customers
  ↓
Sites
  ↓
Assets
  ↓
Service Requests
  ↓
Work Orders
  ↓
Scheduling / Dispatch
  ↓
Field Execution
  ↓
Inventory
  ↓
Billing
  ↓
Notifications / Administration
```

Each slice:

``` text
Domain/API contract
    ↓
Backend implementation
    ↓
OpenAPI generation
    ↓
Frontend feature
    ↓
Tests
    ↓
Accessibility/responsive review
    ↓
Self-review
    ↓
PR + CI
    ↓
Merge
```

Never ask an AI agent to generate the entire application at once.

------------------------------------------------------------------------

## 31. Git / PR Discipline

Use short-lived branches, for example:

``` text
feature/r0-frontend-foundation
feature/r1-customer-spine
feature/r1-work-orders
feature/r1-dispatch-board
feature/r1-technician-offline
fix/dispatch-conflict-state
```

PRs should describe:

-   problem;
-   approach;
-   architectural impact;
-   screenshots for UI changes;
-   tests;
-   accessibility;
-   responsive behavior;
-   API/migration dependencies;
-   known follow-ups.

No direct commits to `main`. Do not commit generated build output unless
repository policy explicitly requires it.

------------------------------------------------------------------------

## 32. Rejected / Deferred Approaches

### Rejected Initially

-   generic admin dashboard template;
-   Metronic-style stylesheet swapping;
-   reload required for theme changes;
-   hardcoded feature HEX colors;
-   `!important` as theming architecture;
-   duplicate Query state in Zustand;
-   separate SignalR truth store;
-   `nuqs` without demonstrated need;
-   broad authenticated API caching by service worker;
-   casually storing long-lived JWT credentials in JavaScript-readable
    storage;
-   generic CRUD engine as product architecture;
-   memoization everywhere;
-   mandatory virtualization.

### Deferred

-   arbitrary custom tenant white-label editor;
-   unrestricted tenant palette editor;
-   advanced Dispatch drag/drop/resize;
-   dedicated frontend error-tracking vendor;
-   full i18n framework;
-   charts;
-   additional global-state tooling;
-   paid visual-regression service.

------------------------------------------------------------------------

## 33. R1 Frontend Quality Gates

Relevant features must satisfy:

-   strict TypeScript;
-   API contract integration;
-   permission-aware UX;
-   loading state where data-bearing;
-   empty state where data-bearing;
-   error state;
-   conflict state where applicable;
-   responsive behavior;
-   keyboard accessibility;
-   theme compatibility;
-   Light/Dark compatibility;
-   no arbitrary hardcoded colors;
-   risk-proportionate tests;
-   no unexplained console errors;
-   no duplicate state ownership;
-   safe tenant context;
-   preserved domain terminology.

Offline features additionally require:

-   offline indicator;
-   queued-command state;
-   retry behavior;
-   conflict handling;
-   idempotent server contract;
-   logout/revocation cleanup.

------------------------------------------------------------------------

## 34. Locked Frontend Decisions

Unless deliberately superseded by a later ADR:

1.  React + TypeScript + Vite SPA/PWA.
2.  Feature-oriented architecture.
3.  TanStack Router.
4.  TanStack Query for server state.
5.  React local state first.
6.  Zustand only when justified.
7.  No `nuqs` initially.
8.  React Hook Form + Zod.
9.  TanStack Table; TanStack Virtual when justified.
10. Source-owned shadcn-style component foundation.
11. Tailwind + semantic CSS Custom Properties.
12. JWT-based authentication.
13. Generated OpenAPI client.
14. ProblemDetails-aware error model.
15. SignalR feeds Query cache.
16. Assignment-scoped technician offline model.
17. Dexie + explicit command outbox.
18. Storybook from foundation.
19. Focused Playwright visual regression.
20. WCAG 2.2 AA target.
21. English-first, i18n-ready R1.
22. No dedicated frontend observability vendor in R1.
23. Dispatch Board in R1.
24. Advanced Dispatch drag/drop deferred.
25. Premium tenant-selectable multi-theme system.
26. Brand theme separate from Light/Dark/System.
27. Tenant default + optional user override.
28. Full custom white-label editor deferred.
29. No generic admin-template dependency.
30. Foundation first, then vertical slices.

------------------------------------------------------------------------

## 35. Domain Alignment

Frontend convenience must never collapse Servexa's approved
distinctions:

-   Account ≠ Contact.
-   Site ≠ Address.
-   Asset ≠ Equipment Model.
-   Service Request ≠ Work Order.
-   Work Order ≠ Booking.
-   Booking ≠ Resource Assignment.
-   Operational Completion ≠ Financial Closure.
-   Resource ≠ User.
-   physical stock ≠ operational usage ≠ customer charge ≠ job cost.

The frontend inherits backend/domain guarantees around:

-   tenant isolation;
-   scoped RBAC;
-   auditability;
-   concurrency;
-   idempotency;
-   outbox reliability;
-   immutable posted invoices;
-   booking overlap protection;
-   offline conflict reconciliation;
-   evidence provenance;
-   authoritative deterministic business rules.

------------------------------------------------------------------------

## 36. Final Architecture Position

Servexa should demonstrate controlled enterprise engineering rather than
maximum framework complexity.

``` text
Strong Domain Model
        +
Generated API Contract
        +
Feature Boundaries
        +
Explicit State Ownership
        +
Semantic Design System
        +
Premium Multi-Theme Engine
        +
Accessible Role-Specific UX
        +
Reliable Offline Field Execution
        +
Realtime Operational Updates
        +
Risk-Based Testing
        =
Servexa Frontend
```

Ajeero's tenant/user theme hierarchy and premium theme-selection
experience are retained as useful product inspiration. Its precompiled
stylesheet swapping, reload requirement, template coupling and hardcoded
scheduling colors are not copied.

This document is the **APPROVED FRONTEND BASELINE**. Material deviations
require an explicit architecture decision/ADR rather than ad-hoc
implementation changes.
