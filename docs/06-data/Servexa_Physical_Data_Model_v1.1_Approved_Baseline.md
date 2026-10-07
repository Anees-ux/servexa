# Servexa Physical Data Model v1.1 --- Approved Baseline

**Status:** APPROVED BASELINE --- Ready for controlled EF Core
Code-First implementation\
**Target:** .NET 10 · EF Core · SQL Server 2025 · React client
(persistence-neutral)\
**Architecture:** Modular Monolith · one SQL Server database ·
module-owned schemas\
**Primary source of truth:** Servexa Logical Data Model v1.1 ---
Approved Baseline\
**Supporting authority:** Servexa ADRs v1.0 → Master PRD v1.1 → Event
Storming & Domain Validation v1.0\
**Scope rule:** This document translates approved logical decisions into
physical persistence. It does not reopen product/domain discovery.

------------------------------------------------------------------------

## 1. Executive Summary

Servexa uses one SQL Server database with module-owned schemas:

`platform`, `customers`, `assets`, `service`, `scheduling`, `field`,
`inventory`, `billing`, `payments`, `jobcost`.

`contracts` is a reserved later-release schema boundary and is **not
materialized in R1**.

The physical model preserves the strongest decisions from the candidate
design:

-   UUIDv7-compatible `uniqueidentifier` identifiers.
-   `TenantId` on every tenant-owned row.
-   tenant-safe composite alternate keys and foreign keys where
    relational integrity is valuable.
-   SQL Server `rowversion` for optimistic concurrency on mutable
    aggregate roots.
-   explicit append-only ledgers for audit, inventory movement and
    financial settlement facts.
-   transactional outbox/inbox/idempotency.
-   release-aware materialization: logical future boundaries do not
    automatically become tables.
-   relational columns by default; JSON only for genuinely
    document-shaped data.
-   immutable posted financial documents through application command
    boundaries plus physical safeguards/tests.
-   offline-created facts preserve client identity and device/server
    timestamps.

Corrections made during approval:

1.  Cross-module SQL foreign keys are **not blanket-prohibited**. Stable
    same-database identity relationships use tenant-safe FKs when
    lifecycle semantics permit. Module ownership remains an
    application/code boundary.
2.  Scheduling no-overlap is protected by an explicit R1 SQL-local
    serialization mechanism, but `ResourceScheduleGuard` is a
    **technical concurrency table**, not a domain aggregate.
3.  Offline inventory discrepancy no longer conflicts with a database
    `OnHand >= 0` rule.
4.  Settlement-position derived values are internally consistent and do
    not duplicate unsupported columns.
5.  Posted-invoice immutability has an explicit enforcement strategy.
6.  Stable hot-path statuses use compact numeric codes rather than
    universal string enums.
7.  UUIDv7 identity and clustered-index strategy are treated as separate
    decisions.
8.  Canonical instants use one standard: `datetime2(3)` UTC.
9.  SHA-256 hashes use `binary(32)` internally.
10. EF `ValueGeneratedOnAdd()` is not treated as immutability
    enforcement.
11. The R0/R1 physical table set is completed sufficiently for
    implementation sequencing.

**Final verdict:** READY FOR CONTROLLED CODE-FIRST IMPLEMENTATION,
module by module---not a one-shot generation of the entire database.

------------------------------------------------------------------------

## 2. Physical Modeling Principles

1.  **Relational by default.** Queryable, constrained business facts are
    columns/tables.
2.  **One authoritative home per fact.** Projections never replace
    ledgers.
3.  **Tenant safety is physical as well as application-level.**
4.  **Aggregate boundaries drive writes; SQL relationships drive
    integrity.**
5.  **Cross-module FK does not grant cross-module mutation.**
6.  **No cascade across aggregate/module boundaries by default.**
7.  **Append-only facts are corrected by compensation, not mutation.**
8.  **R1 materialization only.** R2/R3/R4 logical structures stay
    deferred unless an R1 FK genuinely requires a stable reference.
9.  **No database cleverness without a concrete invariant/access
    pattern.**
10. **Migrations are reviewed artifacts.** Generated SQL is inspected
    before promotion.

------------------------------------------------------------------------

## 3. SQL Server / EF Core Conventions

  -----------------------------------------------------------------------
  Concern                             Standard
  ----------------------------------- -----------------------------------
  Primary IDs                         `uniqueidentifier`, generated as
                                      UUIDv7 in application

  Tenant ID                           `uniqueidentifier NOT NULL`

  Canonical instant                   `datetime2(3)` UTC

  Local date                          `date`

  Local wall-clock time               `time(0)` where needed

  Timezone                            bounded IANA timezone identifier,
                                      `varchar(100)`

  Money                               `decimal(19,4)`

  Inventory quantity                  `decimal(18,4)`

  Percentage/rate                     `decimal(9,6)` unless domain needs
                                      more

  Coordinates                         `decimal(9,6)`

  SHA-256                             `binary(32)`

  Concurrency                         SQL Server `rowversion`

  JSON                                `nvarchar(max)` + `ISJSON` check
                                      where stored

  Free notes                          bounded `nvarchar(n)` unless
                                      genuinely unbounded

  Machine status                      `smallint` with application enum +
                                      DB check for critical states

  External/provider code              bounded `varchar(n)`

  Currency                            `char(3)` ISO 4217

  Country                             `char(2)` ISO 3166-1 alpha-2 where
                                      applicable
  -----------------------------------------------------------------------

### 3.1 Clustered index rule

UUIDv7 improves insertion locality but does not automatically decide
clustering. Default R1 rule:

-   normal aggregate tables: clustered PK on `Id` is acceptable
    initially because application-generated UUIDv7 is time-ordered
    enough for R1 scale;
-   very high-write append ledgers may use a dedicated clustered access
    path only when measurement proves useful;
-   do **not** add hidden BIGINT identity keys merely to optimize
    theoretical fragmentation;
-   monitor page splits/fragmentation and revise from evidence.

------------------------------------------------------------------------

## 4. Identifier and Business Number Strategy

All entity identities are UUIDv7-compatible GUIDs generated in the
application. Offline-capable entities generate the same identifier
format on the device.

Human numbers (`WO-...`, `INV-...`, etc.) are separate and
tenant-scoped.

`platform.NumberSeries` serializes number issuance. Invoice/Credit Note
numbers are assigned at posting/issue. Gapless numbering is **not
globally assumed**; a jurisdiction-specific numbering policy may later
require it.

------------------------------------------------------------------------

## 5. Multi-Tenant Physical Isolation Strategy

### 5.1 Mandatory rules

Every tenant-owned table contains `TenantId NOT NULL`.

For stable relational entities, define:

-   PK: `Id`
-   alternate key: `(TenantId, Id)`

Tenant-owned FKs use `(TenantId, ReferencedId)` → `(TenantId, Id)` where
the referenced table is materialized and the relationship is stable.

This prevents a row in Tenant A from physically referencing Tenant B.

EF Core global query filters remain defense-in-depth, not the only
isolation mechanism.

### 5.2 Cross-module FKs

Cross-schema FKs are allowed when all are true:

-   both tables live in the same Servexa database;
-   target identity is stable;
-   relationship has clear lifecycle semantics;
-   FK does not imply cascade deletion;
-   it materially prevents corrupt references.

No direct cross-module table mutation is allowed in application code
merely because an FK exists.

Polymorphic audit/file/approval references remain logical references
because a single FK cannot target multiple tables safely.

------------------------------------------------------------------------

## 6. Schema Ownership

  -------------------------------------------------------------------------------------------
  Schema                  Module                  R1 responsibility
  ----------------------- ----------------------- -------------------------------------------
  `platform`              Platform                tenancy, identity membership,
                                                  authorization, policies, numbering, audit,
                                                  files, reliability

  `customers`             Customers               accounts, contacts, sites, customer
                                                  relationships

  `assets`                Assets                  models, installed assets,
                                                  placement/lifecycle

  `service`               Service                 requests, work orders, scope, operational
                                                  history/reports

  `scheduling`            Scheduling              resources, requirements, bookings,
                                                  assignments, commitments

  `field`                 FieldExecution          execution,
                                                  tasks/forms/readings/evidence/signatures,
                                                  offline sync, part usage

  `inventory`             Inventory               products, locations, items/lots, movement
                                                  ledger, balance projection,
                                                  reservations/reconciliation

  `billing`               Commercial              base pricebook, charges, invoices, credit
                                                  correction, financial closure

  `payments`              Payments                R2 by default; schema reserved

  `jobcost`               JobCosting              R2; schema reserved

  `contracts`             Contracts               R3; schema reserved, no R1 tables
  -------------------------------------------------------------------------------------------

------------------------------------------------------------------------

## 7. R0/R1 Materialization Matrix

  ------------------------------------------------------------------------------------------
  Logical area                                         Materialize now Release
  --------------------------------------- ---------------------------- ---------------------
  Tenant / Branch / Territory                                      Yes R0

  TenantUser / Role / Permission mapping                           Yes R0
  / scoped grants                                                      

  PolicySetting / NumberSeries /                                   Yes R0/R1
  FileObject / DeviceRegistration                                      

  Audit / Idempotency / Outbox / Inbox /                           Yes R0/R1
  DurableWork                                                          

  Account / Contact / Site +                                       Yes R1
  relationships                                                        

  EquipmentModel / Asset +                                         Yes R1
  placement/lifecycle                                                  

  ServiceRequest / WorkOrder /                                     Yes R1
  assets/scope/status/evaluation/report                                

  Resource / skills/certs / requirements                           Yes R1
  / booking / assignment / commitment                                  

  ResourceScheduleGuard                        Yes, **technical only** R1

  ExecutionSession/Intervals / Tasks /                             Yes R1
  Forms / Readings / Evidence / Signature                              
  / PartUsage                                                          

  SyncSession / SyncCommand /                                      Yes R1
  OfflineReconciliation                                                

  Product / Location / Movement / Balance                          Yes R1
  / Reservation / PartRequirement /                                    
  reconciliation                                                       

  Serialized InventoryItem / Lot               Materialize if tracking R1
                                                  policy enabled in R1 

  Base Pricebook / ChargeCalculation /                             Yes R1
  Invoice / CreditNote / FinancialClosure                              

  Payment/Allocation                                     No by default R2

  Job Costing valuation                                             No R2

  Agreements/Warranty/SLA/PM                                        No R3

  Portal/AI/RAG structures                                          No R4/R5
  ------------------------------------------------------------------------------------------

------------------------------------------------------------------------

# 8. Platform Physical Model

## 8.1 `platform.Tenants`

  Column                Type                 Null Purpose
  --------------------- ------------------ ------ --------------------
  Id                    uniqueidentifier       No Tenant identity
  TenantCode            varchar(50)            No Human/system alias
  LegalName             nvarchar(200)          No Legal name
  DisplayName           nvarchar(200)          No Display name
  Status                smallint               No Lifecycle
  DefaultTimeZoneId     varchar(100)           No IANA timezone
  DefaultCurrencyCode   char(3)                No ISO currency
  CreatedAtUtc          datetime2(3)           No Audit instant
  ModifiedAtUtc         datetime2(3)           No Audit instant
  Version               rowversion             No Concurrency

PK `Id`; unique `TenantCode`. No hard delete after activation.

## 8.2 `platform.Branches`

`Id`, `TenantId`, `Code`, `Name`, `TimeZoneId`, `Status`,
`CreatedAtUtc`, `ModifiedAtUtc`, `Version`.

Unique `(TenantId, Id)` and `(TenantId, Code)`. FK
`TenantId → Tenants.Id`.

## 8.3 `platform.Territories`

`Id`, `TenantId`, `Code`, `Name`, `ParentTerritoryId?`, `Status`,
timestamps, `Version`.

Tenant-safe self FK. Unique `(TenantId, Code)`.

## 8.4 `platform.TenantUsers`

`Id`, `TenantId`, `ExternalSubject`, `ExternalIssuer`, `DisplayName`,
`NormalizedEmail?`, `DefaultBranchId?`, `Status`, timestamps, `Version`.

Unique `(TenantId, ExternalIssuer, ExternalSubject)`. Optional
tenant-safe FK to Branch. `TenantUser` is not Resource and not Contact.

## 8.5 Authorization tables

### `platform.Roles`

`Id`, `TenantId`, `Name`, `NormalizedName`, `IsSystem`, `Status`,
`Version`.

Unique `(TenantId, NormalizedName)`.

### `platform.Permissions`

Global catalogue: `Id`, `Code`, `Description`, `Status`. Unique `Code`.

### `platform.RolePermissions`

`RoleId`, `PermissionId`; composite PK. Role delete restricted once
used.

### `platform.RoleAssignments`

`Id`, `TenantId`, `UserId`, `RoleId`, `EffectiveFromUtc`,
`EffectiveToUtc?`, `Version`.

### `platform.ScopeAssignments`

`Id`, `TenantId`, `RoleAssignmentId`, `ScopeType`, `ScopeId`.

Unique active logical grant enforced by application + supporting index.
Scope target is polymorphic and validated by authorization service; no
impossible multi-table FK.

## 8.6 `platform.PolicySettings`

`Id`, `TenantId`, `PolicyKey`, `ScopeType`, `ScopeId?`, `ValueJson`,
`EffectiveFromUtc`, `EffectiveToUtc?`, `VersionNumber`, `Version`.

`ISJSON(ValueJson)=1`. Unique
`(TenantId, PolicyKey, ScopeType, ScopeId, EffectiveFromUtc)` with
null-scope normalization handled by configuration/design. Published
history is not overwritten.

## 8.7 `platform.NumberSeries`

`Id`, `TenantId`, `SeriesKey`, `ScopeKey`, `PrefixPattern?`,
`NextValue bigint`, `Status`, `Version`.

Unique `(TenantId, SeriesKey, ScopeKey)`. Number issuance occurs in a
short transaction. `Version` protects optimistic retries; physical
implementation may additionally take an update lock on the row during
issuance.

## 8.8 `platform.FileObjects`

`Id`, `TenantId`, `OwnerType`, `OwnerId`, `OriginalName`, `ContentType`,
`SizeBytes bigint`, `ContentHash binary(32)?`, `StorageKey`,
`Classification`, `Visibility`, `UploadStatus`, `ScanStatus`,
`UploadedByUserId?`, `UploadedAtUtc`, `RetentionState`, `Version`.

Unique `(TenantId, StorageKey)`. Binary bytes remain in object storage.

## 8.9 `platform.AuditRecords`

Append-only:

`Id`, `TenantId`, `OccurredAtUtc`, `Stream`, `ActorType`,
`ActorUserId?`, `ActionCode`, `EntityType`, `EntityId`, `Reason?`,
`BeforeAfterJson?`, `CorrelationId?`, `CommandId?`, `DeviceId?`,
`Outcome`.

Indexes: - `(TenantId, EntityType, EntityId, OccurredAtUtc DESC)` -
`(TenantId, Stream, OccurredAtUtc DESC)`

No EF update/delete path. Security/financial retention is
compliance-controlled.

## 8.10 `platform.IdempotencyRecords`

`Id`, `TenantId`, `OperationType`, `CommandId`,
`PayloadHash binary(32)`, `Status`, `ResultType?`, `ResultId?`,
`FirstSeenAtUtc`, `CompletedAtUtc?`, `ExpiresAtUtc`.

Unique `(TenantId, OperationType, CommandId)`. Same key + different hash
is rejected.

## 8.11 `platform.OutboxMessages`

`Id`, `TenantId`, `EventType`, `EventVersion int`,
`SourceAggregateType`, `SourceAggregateId`, `PayloadJson`,
`OccurredAtUtc`, `Status`, `AttemptCount`, `NextAttemptAtUtc?`,
`LeaseOwner?`, `LeaseExpiresAtUtc?`, `DispatchedAtUtc?`,
`LastErrorCode?`.

`ISJSON(PayloadJson)=1`.

Worker index: `(Status, NextAttemptAtUtc, OccurredAtUtc)` INCLUDE
`(TenantId, EventType, AttemptCount)`.

Payload is immutable after insert; processing fields may change.

## 8.12 `platform.InboxMessages`

`Id`, `TenantId`, `Provider`, `ExternalEventId`, `ReceivedAtUtc`,
`SignatureVerified`, `PayloadHash`, `ProcessingStatus`,
`ResultCommandId?`, `ProcessedAtUtc?`.

Unique `(Provider, ExternalEventId)` when provider IDs are globally
unique; otherwise `(TenantId, Provider, ExternalEventId)`.

## 8.13 `platform.DurableWorkItems`

`Id`, `TenantId`, `WorkType`, `TargetType`, `TargetId?`, `PayloadJson?`,
`Status`, `AttemptCount`, `NextRunAtUtc`, `LeaseOwner?`,
`LeaseExpiresAtUtc?`, `DedupeKey?`, timestamps.

Filtered unique active dedupe where practical. Polling index
`(Status, NextRunAtUtc)`.

## 8.14 `platform.DeviceRegistrations`

`Id`, `TenantId`, `UserId`, `DeviceKey`, `Platform`, `AppVersion`,
`RegisteredAtUtc`, `LastSyncAtUtc?`, `OfflineCacheExpiresAtUtc?`,
`Status`, `RevokedAtUtc?`, `RevocationReason?`, `Version`.

Unique `(TenantId, DeviceKey)`.

------------------------------------------------------------------------

# 9. Customers Physical Model

## 9.1 `customers.Accounts`

`Id`, `TenantId`, `AccountNumber`, `LegalName`, `DisplayName`,
`AccountType`, `Status`, `DefaultBranchId?`, `PaymentTermsDays?`,
`CurrencyCode`, `IsCreditHold`, `CreditHoldReason?`, external refs,
timestamps, `Version`.

Unique `(TenantId, AccountNumber)`.

## 9.2 `customers.Contacts`

`Id`, `TenantId`, `FirstName`, `LastName`, `DisplayName`,
`PrimaryEmail?`, `PrimaryPhone?`, `PreferredLanguage?`, `Status`,
timestamps, `Version`.

Contact channel expansion may use child table if multiple channels are
required in first slice; do not JSON-store channels that require
search/uniqueness.

## 9.3 `customers.Sites`

`Id`, `TenantId`, `SiteNumber`, `Name`, `BranchId`, `TerritoryId?`,
`TimeZoneId`, flattened address columns, `Latitude?`, `Longitude?`,
`Status`, access/hazard summary fields, timestamps, `Version`.

Unique `(TenantId, SiteNumber)`. Tenant-safe FKs to Branch/Territory.

## 9.4 Relationship tables

-   `customers.AccountRelationships`
-   `customers.SiteAccountRelationships`
-   `customers.ContactRoleAssignments`

All include `TenantId`, effective dates and tenant-safe referenced IDs.
Historical relationships are end-dated, not deleted.

For parent-account hierarchy, cycle prevention is application/domain
validation; SQL FK alone cannot prevent graph cycles.

------------------------------------------------------------------------

# 10. Assets Physical Model

## 10.1 `assets.EquipmentModels`

`Id`, `TenantId`, `ManufacturerName`, `ModelCode`, `DisplayName`,
`CategoryCode`, `TrackingPolicy`, `Status`, timestamps, `Version`.

Unique tenant/model identity as appropriate.

## 10.2 `assets.Assets`

`Id`, `TenantId`, `AssetNumber`, `EquipmentModelId`, `SerialNumber?`,
`CurrentSiteId?`, `CurrentOwnerAccountId?`, `Status`, `InstalledAtUtc?`,
`DecommissionedAtUtc?`, timestamps, `Version`.

Unique `(TenantId, AssetNumber)`. Serial uniqueness is
**policy-dependent**; do not impose one universal unique serial rule
across all equipment categories.

## 10.3 History

-   `assets.AssetPlacements`: append/effective-dated site placement.
-   `assets.AssetOwnerships`: effective-dated account ownership.
-   `assets.AssetLifecycleEvents`: append-only lifecycle facts.
-   `assets.AssetComponentLinks`: effective-dated parent/component
    relationships where R1 requires components.

Current pointers on `Assets` and history records must be updated in one
transaction.

------------------------------------------------------------------------

# 11. Service Physical Model

## 11.1 `service.ServiceRequests`

`Id`, `TenantId`, `RequestNumber`, `AccountId`, `SiteId`, `ContactId?`,
`Priority`, `Status`, `Summary`, `Description?`, `ReportedAtUtc`,
`Channel`, timestamps, `Version`.

Unique `(TenantId, RequestNumber)`. Index
`(TenantId, Status, Priority, ReportedAtUtc)`.

## 11.2 `service.ServiceRequestAssets`

`Id`, `TenantId`, `ServiceRequestId`, `AssetId`; unique
`(TenantId, ServiceRequestId, AssetId)`.

## 11.3 `service.WorkOrders`

  Column                        Type                 Null
  ----------------------------- ------------------ ------
  Id                            uniqueidentifier       No
  TenantId                      uniqueidentifier       No
  WorkOrderNumber               varchar(50)            No
  ServiceRequestId              uniqueidentifier      Yes
  ServiceAccountId              uniqueidentifier       No
  BillToAccountId               uniqueidentifier       No
  PrimarySiteId                 uniqueidentifier       No
  WorkTypeCode                  varchar(50)            No
  Priority                      smallint               No
  OperationalStatus             smallint               No
  Summary                       nvarchar(300)          No
  Description                   nvarchar(max)         Yes
  PauseReasonCode               varchar(50)           Yes
  BillToSnapshotJson            nvarchar(max)          No
  OperationallyCompletedAtUtc   datetime2(3)          Yes
  CreatedAtUtc                  datetime2(3)           No
  ModifiedAtUtc                 datetime2(3)           No
  Version                       rowversion             No

Unique `(TenantId, WorkOrderNumber)`. Tenant-safe FKs to Request
(optional), Accounts and Site. `ISJSON(BillToSnapshotJson)=1`.

Indexes: - `(TenantId, OperationalStatus, Priority, CreatedAtUtc)` -
`(TenantId, PrimarySiteId, OperationalStatus)` -
`(TenantId, ServiceAccountId, OperationalStatus)`

## 11.4 `service.WorkOrderAssets`

`Id`, `TenantId`, `WorkOrderId`, `AssetId`, `Role`. Unique
`(TenantId, WorkOrderId, AssetId)`. Cascade allowed from a
**draft/unreferenced** WorkOrder child relationship only; normal
operational deletion is not used.

## 11.5 `service.WorkOrderScopeItems`

`Id`, `TenantId`, `WorkOrderId`, `Sequence`, `ScopeType`, `Description`,
`Status`, `AssetId?`, timestamps.

## 11.6 `service.WorkOrderStatusHistory`

Append-only: `Id`, `TenantId`, `WorkOrderId`, `FromStatus`, `ToStatus`,
`ReasonCode?`, `ChangedByUserId`, `ChangedAtUtc`, `CommandId?`.

## 11.7 `service.WorkOrderCompletionEvaluations`

Append-only: `Id`, `TenantId`, `WorkOrderId`, `EvaluatedAtUtc`,
`Outcome`, `GateResultsJson`, `PolicySnapshotJson?`, `CommandId`.

Unique `(TenantId, CommandId)` for retry-sensitive evaluation.

## 11.8 `service.ScopeChangeRequests`

`Id`, `TenantId`, `WorkOrderId`, `RequestedByUserId`, `Description`,
`Status`, `ApprovalId?`, timestamps, `Version`.

## 11.9 `service.ServiceReports`

`Id`, `TenantId`, `WorkOrderId`, `VersionNumber`, `Status`,
`ContentJson`, `PublishedAtUtc?`, `Visibility`, `Version`.

Published report versions are immutable; new publication creates a new
version.

------------------------------------------------------------------------

# 12. Scheduling Physical Model

## 12.1 `scheduling.Resources`

`Id`, `TenantId`, `ResourceCode`, `ResourceType`, `DisplayName`,
`UserId?`, `HomeBranchId`, `ExclusiveCapacity bit`, `Status`,
timestamps, `Version`.

Unique `(TenantId, ResourceCode)`.

## 12.2 Qualification tables

-   `scheduling.Skills`
-   `scheduling.ResourceSkills`
-   `scheduling.Certifications`
-   `scheduling.ResourceCertifications`

Certification assignment includes `IssuedAt`, `ExpiresAt`, credential
reference and status. Eligibility queries index active skill/cert rows.

## 12.3 Availability

-   `scheduling.WorkingTimeProfiles`
-   `scheduling.WorkingTimeRules`
-   `scheduling.AvailabilityExceptions`

Store local wall-clock rules with timezone identity; actual
booking/commitment instants are UTC.

## 12.4 `scheduling.ResourceRequirements`

`Id`, `TenantId`, `WorkOrderId`, `RequirementType`, `Quantity`,
`DurationMinutes`, `TerritoryId?`, `Status`, `Version`.

Children for required skills/certifications when needed.

## 12.5 `scheduling.Bookings`

`Id`, `TenantId`, `BookingNumber`, `WorkOrderId`, `SiteId`,
`PlannedStartUtc`, `PlannedEndUtc`, `SiteTimeZoneId`, `Status`,
`DispatchStatus`, timestamps, `Version`.

Check `PlannedEndUtc > PlannedStartUtc`.

Indexes: - `(TenantId, WorkOrderId)` -
`(TenantId, Status, PlannedStartUtc)` -
`(TenantId, SiteId, PlannedStartUtc)`

## 12.6 `scheduling.ResourceAssignments`

`Id`, `TenantId`, `BookingId`, `ResourceId`, `AssignmentRole`,
`PlannedStartUtc`, `PlannedEndUtc`, `Status`, `SelectionRationale?`,
timestamps, `Version`.

Do **not** globally unique `(BookingId, ResourceId)` across history if
reassignment/replacement history needs multiple records. Use an
active-state filtered uniqueness rule or explicit supersession
relationship.

## 12.7 `scheduling.ResourceCommitments`

`Id`, `TenantId`, `ResourceId`, `ResourceAssignmentId`,
`CommitmentKind`, `StartUtc`, `EndUtc`, `Status`, `SupersededById?`,
`CreatedAtUtc`.

Check `EndUtc > StartUtc`.

Index `(TenantId, ResourceId, Status, StartUtc, EndUtc)`.

A normal B-tree index does not itself prevent interval overlap.

## 12.8 `scheduling.ResourceScheduleGuards` --- technical concurrency table

This is **not a domain aggregate**. It is an R1 SQL-local serialization
mechanism.

Columns: `TenantId`, `ResourceId`, `Version rowversion`, `TouchedAtUtc`.

PK/unique `(TenantId, ResourceId)`.

### R1 commit algorithm

Within one short SQL transaction:

1.  acquire/update-lock semantics on the guard row for every affected
    exclusive resource in deterministic ResourceId order;
2.  re-query active commitments that overlap `[StartUtc, EndUtc)`;
3.  reject if any conflict exists;
4.  insert/update assignment and commitment;
5.  commit.

No network call occurs inside this transaction.

The exact EF/SQL implementation may use a targeted SQL Server locking
query because pure optimistic `rowversion` with a late dummy update can
allow validation races if the validation is performed before
serialization. Concurrency integration tests must prove that two
competing commits cannot both succeed.

This technical table is the selected R1 physical mechanism; it does not
change the Logical Model.

## 12.9 Scheduling history

`BookingScheduleRevisions` and `SchedulingConflictLogs` are append-only
where required for dispatcher auditability.

------------------------------------------------------------------------

# 13. Field Execution Physical Model

## 13.1 `field.ExecutionSessions`

`Id`, `TenantId`, `AssignmentId`, `DeviceId?`, `Status`,
`StartedAtUtc?`, `CompletedAtUtc?`, `Version`.

## 13.2 `field.ExecutionIntervals`

Client-generated ID. `TenantId`, `ExecutionSessionId`, `IntervalType`
(Travel/Work/Pause), `StartedAtUtc`, `EndedAtUtc?`,
`DeviceObservedAtUtc`, `ServerReceivedAtUtc`, `SourceCommandId`.

Unique `(TenantId, SourceCommandId)` where one command creates one
interval fact.

## 13.3 `field.WorkTasks`

`Id`, `TenantId`, `WorkOrderId`, `AssignmentId?`, `AssetId?`,
`Sequence`, `TaskType`, `Title`, `Status`, `CompletedAtUtc?`, `Version`.

## 13.4 Forms

### `field.FormTemplates`

Template identity.

### `field.FormTemplateVersions`

Published immutable version with `DefinitionJson` + `ISJSON`.

### `field.FormResponses`

`Id` client-generated, `TenantId`, `TemplateVersionId`, `WorkOrderId`,
`AssignmentId`, `AssetId?`, `Status`, `ResponseJson`,
`DeviceObservedAtUtc`, `ServerReceivedAtUtc`, `SourceCommandId`,
`Version`.

Unique `(TenantId, SourceCommandId)` for creation command.

## 13.5 `field.Readings`

Append-oriented field fact: client ID, tenant, WO, assignment, asset,
reading code, numeric/text value, UOM, observed/server times, source
command.

## 13.6 `field.EvidenceItems`

`Id` client-generated, `TenantId`, `WorkOrderId`, `AssignmentId`,
`AssetId?`, `FileId`, `EvidenceType`, `Visibility`, `CapturedAtUtc`,
`ServerReceivedAtUtc`, `ContentHash`, `SourceCommandId`.

## 13.7 `field.CustomerSignatures`

`Id` client-generated, `TenantId`, `WorkOrderId`, `AssignmentId`,
`SignerName`, `SignerContactId?`, `SignedAtUtc`, `FileId`,
`ContentHash`, `SourceCommandId`.

Signature core is immutable after accepted sync.

## 13.8 `field.DiagnosisRecords`

`Id`, `TenantId`, `WorkOrderId`, `AssignmentId`, `AssetId?`,
`DiagnosisText`, `RootCauseCode?`, `ResolutionText?`, timestamps,
`Version`.

## 13.9 `field.PartUsages`

`Id` client-generated, `TenantId`, `WorkOrderId`, `AssignmentId`,
`AssetId?`, `ProductId`, `InventoryItemId?`, `SourceLocationId`,
`Quantity`, `UsageType`, `DeviceObservedAtUtc`, `ServerReceivedAtUtc`,
`SourceCommandId`, `ReconciliationStatus`.

Check `Quantity > 0`.

Unique `(TenantId, SourceCommandId)` for one usage per command; if a
command can contain multiple lines, use
`(TenantId, SourceCommandId, CommandLineKey)` instead.

PartUsage is the operational fact; it is not the inventory ledger row.

## 13.10 Offline sync

### `field.SyncSessions`

`Id`, `TenantId`, `DeviceId`, `UserId`, `StartedAtUtc`,
`CompletedAtUtc?`, `Status`, counters, `Version`.

### `field.SyncCommandRecords`

Composite identity-safe design: `Id`, `TenantId`, `CommandId`,
`DeviceId`, `OperationType`, `PayloadHash`, `PayloadJson?`,
`ExpectedVersion?`, `DeviceObservedAtUtc`, `ServerReceivedAtUtc`,
`Status`, `ResultType?`, `ResultId?`, `ConflictId?`, error metadata.

Unique `(TenantId, CommandId, OperationType)`.

### `field.OfflineReconciliationItems`

`Id`, `TenantId`, `ConflictType`, `TargetType`, `TargetId`,
`SourceCommandId`, `Status`, `DetailsJson`, `CreatedAtUtc`,
`ResolvedAtUtc?`, `ResolvedByUserId?`, `ResolutionCode?`, `Version`.

------------------------------------------------------------------------

# 14. Inventory Physical Model

## 14.1 `inventory.Products`

`Id`, `TenantId`, `ProductCode`, `Name`, `BaseUomCode`,
`TrackingPolicy`, `Status`, timestamps, `Version`.

Unique `(TenantId, ProductCode)`.

## 14.2 `inventory.InventoryLocations`

`Id`, `TenantId`, `LocationCode`, `LocationType`, `BranchId?`,
`ResourceId?`, `Name`, `Status`, timestamps, `Version`.

Location types may represent warehouse, truck, onsite, transit,
quarantine, reconciliation/system location.

## 14.3 Serialized / lot tracking

### `inventory.InventoryItems`

For serialized products: `Id`, `TenantId`, `ProductId`, `SerialNumber`,
`CurrentLocationId?`, `Status`, `DisputeStatus`, timestamps, `Version`.

Filtered unique `(TenantId, ProductId, SerialNumber)` where tracking
policy requires tenant/product uniqueness.

### `inventory.InventoryLots`

For lot-tracked products: `Id`, `TenantId`, `ProductId`, `LotNumber`,
`ExpiryDate?`, `Status`, `Version`.

## 14.4 `inventory.InventoryMovements` --- authoritative physical ledger

  Column             Type                 Null
  ------------------ ------------------ ------
  Id                 uniqueidentifier       No
  TenantId           uniqueidentifier       No
  ProductId          uniqueidentifier       No
  InventoryItemId    uniqueidentifier      Yes
  LotId              uniqueidentifier      Yes
  FromLocationId     uniqueidentifier       No
  ToLocationId       uniqueidentifier       No
  Quantity           decimal(18,4)          No
  MovementType       smallint               No
  CausationType      varchar(50)            No
  CausationId        uniqueidentifier       No
  CausationLineKey   varchar(100)           No
  OccurredAtUtc      datetime2(3)           No
  RecordedAtUtc      datetime2(3)           No
  IsDisputed         bit                    No
  SourceCommandId    uniqueidentifier      Yes

Check `Quantity > 0`, `FromLocationId <> ToLocationId`.

Unique `(TenantId, CausationType, CausationId, CausationLineKey)`
prevents duplicate physical effects.

This is an immutable location-to-location movement ledger---not an
accounting "double-entry" ledger.

## 14.5 `inventory.StockBalances` --- rebuildable projection

`Id`, `TenantId`, `ProductId`, `LocationId`, `LotId?`, `StockState`,
`OnHand`, `Reserved`, `Version`.

Unique normalized balance key.

`Available` is computed as `OnHand - Reserved` in query/application
projection rather than independently persisted unless profiling proves a
need.

### Negative/offline policy

Do **not** use a universal `CHECK (OnHand >= 0)` if approved offline
physical-reality reconciliation can temporarily reveal
shortage/discrepancy.

R1 rule:

-   normal online commands reject insufficient available stock according
    to policy;
-   offline PartUsage is preserved;
-   corresponding movement may post into a disputed/reconciliation state
    using an explicit system/reconciliation location or discrepancy
    record;
-   `InventoryReconciliationException` is created;
-   serialized items involved become unavailable/disputed until
    resolved.

The balance projection remains mathematically derived from accepted
movement facts; policy exceptions are explicit records, not invisible
CHECK bypasses.

Check `Reserved >= 0`; reservation logic prevents normal reserved
quantity from exceeding allocatable stock.

## 14.6 `inventory.Reservations`

`Id`, `TenantId`, `ProductId`, `LocationId`, `WorkOrderId`, `Quantity`,
`Status`, `ExpiresAtUtc?`, timestamps, `Version`.

Reservation ≠ consumption.

## 14.7 `inventory.PartRequirements`

`Id`, `TenantId`, `WorkOrderId`, `ProductId`, `RequiredQuantity`,
`Status`, preferred location?, timestamps, `Version`.

## 14.8 Transfers / adjustments / reconciliation

R1 physical support where required by warehouse workflows:

-   `inventory.Transfers`
-   `inventory.TransferLines`
-   `inventory.InventoryAdjustments`
-   `inventory.InventoryAdjustmentLines`
-   `inventory.InventoryReconciliationExceptions`

Adjustment approval creates compensating movement facts; it does not
rewrite movement history.

RMA remains R2 unless activated by release scope.

------------------------------------------------------------------------

# 15. Commercial / Billing Physical Model

## 15.1 Base Pricebook

### `billing.Pricebooks`

`Id`, `TenantId`, `Code`, `Name`, `CurrencyCode`, `Status`, timestamps,
`Version`.

### `billing.PricebookVersions`

`Id`, `TenantId`, `PricebookId`, `VersionNumber`, `EffectiveFromUtc`,
`EffectiveToUtc?`, `Status`, `PublishedAtUtc?`.

Published versions immutable.

### `billing.PricebookItems`

`Id`, `TenantId`, `PricebookVersionId`, `ItemType`, `ProductId?`,
`ServiceCode?`, `UnitPrice decimal(19,4)`, tax category/code,
minimum/rounding fields only if approved.

Do not use a generic `TargetId` without type integrity if separate
nullable typed references are practical.

## 15.2 `billing.ChargeCalculations`

`Id`, `TenantId`, `WorkOrderId`, `CalculationVersion`, `Status`,
`CalculatedAtUtc`, `CurrencyCode`, `Version`.

### `billing.ChargeLines`

`Id`, `TenantId`, `ChargeCalculationId`, `SourceType`, `SourceId?`,
`Description`, `Quantity`, `UnitPrice`, `NetAmount`, `TaxAmount`,
`GrossAmount`, `PriceSourceSnapshotJson`, `AssetId?`.

Finalized calculation versions are immutable.

## 15.3 `billing.Invoices`

`Id`, `TenantId`, `InvoiceNumber?`, `AccountId`, `WorkOrderId?`,
`Status`, `CurrencyCode`, bill-to snapshot columns/JSON, `NetTotal`,
`TaxTotal`, `GrossTotal`, `PostedAtUtc?`, `PostingSeal binary(32)?`,
timestamps, `Version`.

Filtered unique `(TenantId, InvoiceNumber)` where number is not null.

Check: - Posted → InvoiceNumber and PostedAtUtc and PostingSeal
required. - totals non-negative subject to document type rules.

### `billing.InvoiceLines`

`Id`, `TenantId`, `InvoiceId`, `LineNumber`, `SourceChargeLineId?`,
`Description`, `Quantity`, `UnitPrice`, `NetAmount`, `TaxAmount`,
`GrossAmount`, `TaxSnapshotJson?`.

Unique `(TenantId, InvoiceId, LineNumber)`.

### Posted immutability strategy

SQL CHECK constraints alone cannot prevent later UPDATEs.

R1 guarantee is layered:

1.  domain/application commands reject mutation when Invoice status is
    Posted;
2.  EF write services expose no normal update path for posted
    Invoice/InvoiceLine values;
3.  posting occurs transactionally and computes `PostingSeal` over
    canonical financially material header/line values;
4.  integration tests attempt forbidden post-posting modifications;
5.  production DB principal used by the application receives only the
    permissions required by the application deployment model; if
    operationally practical, later hardening may separate posting/ledger
    write permissions;
6.  corrections use Credit Note/rebill, never edit.

A DB trigger is **not** required in R1 unless later threat/compliance
analysis demands database-enforced immutability independent of the
application.

## 15.4 Credit correction

### `billing.CreditNotes`

Header mirroring financial snapshot semantics. Number assigned at issue.
Issued credit note immutable.

### `billing.CreditNoteLines`

Compensating amounts and original invoice-line reference where
applicable.

### `billing.InvoiceCorrectionLinks`

`OriginalInvoiceId`, `CreditNoteId`, `RebillInvoiceId?`, reason,
timestamps.

## 15.5 `billing.FinancialClosures`

`Id`, `TenantId`, `WorkOrderId`, `Status`, `EvaluatedAtUtc`,
`ClosedAtUtc?`, `GateResultsJson`, `Version`.

Financial Closure is separate from WorkOrder operational completion.

------------------------------------------------------------------------

# 16. Payments Physical Model

Payments are **deferred to R2 by default**. No R1 tables are required
merely to support invoice posting.

When activated:

-   `payments.Payments`
-   `payments.PaymentAllocations`
-   `payments.CreditApplications`
-   `payments.Refunds`
-   optional `payments.PaymentIntents`

Settlement authority is the append-only allocation/application ledger.

If a cached `InvoiceSettlementPosition` is materialized, it is a
projection with:

`InvoiceGross`, `AllocatedAmount`, `CreditedAmount`,
`RefundedOrReversedAmount` where applicable, `Outstanding`, `Version`.

Its invariant must reference actual columns. Prefer
recalculation/projection rebuild over treating this cache as financial
authority.

------------------------------------------------------------------------

# 17. Job Costing

R2. No R1 valuation tables.

`CostEntry` will be append-only and may reference PartUsage,
ExecutionInterval or other cost causation. Inventory valuation method
remains the one intentionally deferred physical decision from the
approved LDM.

------------------------------------------------------------------------

# 18. Contracts / SLA / PM

R3 boundary only. Do not create empty `contracts.*` tables in R1.

Do not add R1 foreign keys to nonexistent Entitlement/SLA tables. If R1
needs to preserve a future-origin reference, use nullable
external/reference metadata only when an actual R1 use case exists.

------------------------------------------------------------------------

# 19. Concurrency Design

Use `rowversion` on mutable aggregate roots.

Required R1 examples:

-   Tenant
-   TenantUser
-   Role
-   Account
-   Contact
-   Site
-   Asset
-   ServiceRequest
-   WorkOrder
-   Booking
-   ResourceAssignment
-   Resource
-   ExecutionSession
-   mutable FormResponse
-   Product
-   InventoryItem
-   Reservation
-   StockBalance projection
-   Pricebook draft/version
-   Invoice draft
-   FinancialClosure
-   NumberSeries

Append-only rows do not need `rowversion` unless processing metadata
itself is mutable.

`rowversion` is not a substitute for multi-row invariant serialization.

------------------------------------------------------------------------

# 20. Scheduling No-Overlap Physical Strategy

**Selected R1 approach:** per-resource technical guard row + short
SQL-local protected transaction.

Why selected:

-   single SQL Server database;
-   explicit and understandable;
-   serializes only contenders for the same resource;
-   does not require Redis/distributed locks;
-   works with EF Core while allowing a narrowly targeted SQL locking
    query;
-   easy to concurrency-test.

Important correction: validation must happen **after serialization is
acquired**, not "validate first then dummy-update later."

For multi-resource/crew commits, acquire guards in deterministic
ResourceId order to reduce deadlock risk.

Required concurrency test: two transactions attempt overlapping
commitments for the same exclusive Resource; exactly one succeeds.

------------------------------------------------------------------------

# 21. Idempotency & Offline Persistence

Three layers are distinct:

1.  `platform.IdempotencyRecords` --- server operation replay
    protection.
2.  `field.SyncCommandRecords` --- offline intake/result/conflict
    history.
3.  domain-level unique causation keys --- prevent duplicate
    physical/financial effects even if higher-level retry logic fails.

Retry-sensitive facts must carry `SourceCommandId` or equivalent
causation key.

Examples:

-   PartUsage creation: unique command/line key.
-   InventoryMovement: unique causation tuple.
-   Signature: unique command/line key.
-   FormResponse submission: unique command/version key.
-   Webhook: Inbox unique provider event ID.
-   Outbox handler effects: consumer idempotency.

------------------------------------------------------------------------

# 22. Outbox / Inbox / Durable Work

Outbox message is inserted in the same DB transaction as the
authoritative business change.

Workers claim work in bounded batches. Lease/attempt fields are
processing metadata, not event payload mutation.

No network call occurs inside the source business transaction.

Dead-lettered/failed items remain operationally visible.

------------------------------------------------------------------------

# 23. Audit, History and Immutability

Do not collapse these into one table:

  Need                            Mechanism
  ------------------------------- --------------------------------
  Current mutable state           aggregate table
  Business transition history     explicit domain history table
  Who/why/security trace          `platform.AuditRecords`
  Physical stock truth            `inventory.InventoryMovements`
  Financial settlement truth      allocations/applications
  Effective-dated configuration   version/effective rows
  Read performance                rebuildable projection

SQL Server temporal tables are not an R1 default because they do not
capture business intent/causation. They may later be introduced for a
specific operational need.

Append-only is enforced by write-path design, permissions where
practical, and tests---not by misusing EF value-generation metadata.

------------------------------------------------------------------------

# 24. Effective Dating and Versioning

Use half-open effective intervals conceptually:
`[EffectiveFrom, EffectiveTo)`.

SQL Server does not provide a simple CHECK constraint that prevents
arbitrary overlapping ranges across rows. Therefore:

-   uniqueness handles identical version starts;
-   application/domain transaction validates overlap;
-   high-contention effective-dated records may use a targeted
    serialization strategy if evidence requires it.

Published versions are immutable.

------------------------------------------------------------------------

# 25. Index Catalogue --- R1 Critical

  ----------------------------------------------------------------------------------------------
  Table                 Purpose            Key columns                         Notes
  --------------------- ------------------ ----------------------------------- -----------------
  WorkOrders            Ops queue          TenantId, OperationalStatus,        tenant/status
                                           Priority, CreatedAtUtc              first

  WorkOrders            Site history/open  TenantId, PrimarySiteId,            
                        work               OperationalStatus                   

  ServiceRequests       request queue      TenantId, Status, Priority,         
                                           ReportedAtUtc                       

  Assets                site assets        TenantId, CurrentSiteId, Status     

  Assets                serial search      TenantId, EquipmentModelId,         filtered if
                                           SerialNumber                        serial non-null

  Bookings              dispatch timeline  TenantId, Status, PlannedStartUtc   

  Bookings              WO visits          TenantId, WorkOrderId,              
                                           PlannedStartUtc                     

  ResourceAssignments   booking crew       TenantId, BookingId, Status         

  ResourceCommitments   overlap lookup     TenantId, ResourceId, Status,       supports overlap
                                           StartUtc, EndUtc                    query; not an
                                                                               overlap
                                                                               constraint

  PartUsages            WO parts           TenantId, WorkOrderId,              
                                           ServerReceivedAtUtc                 

  InventoryMovements    product/location   TenantId, ProductId, OccurredAtUtc  location may be
                        history                                                separate access
                                                                               path

  StockBalances         current stock      unique normalized                   
                                           tenant/product/location/lot/state   

  Reservations          active reservation TenantId, LocationId, Status,       filtered active
                        queue              ExpiresAtUtc                        where useful

  Invoices              invoice number     TenantId, InvoiceNumber             unique filtered
                                                                               non-null

  Invoices              billing queue      TenantId, Status, CreatedAtUtc      

  OutboxMessages        worker polling     Status, NextAttemptAtUtc,           global worker
                                           OccurredAtUtc                       path

  DurableWorkItems      worker polling     Status, NextRunAtUtc                

  AuditRecords          entity timeline    TenantId, EntityType, EntityId,     
                                           OccurredAtUtc                       

  SyncCommandRecords    sync exception     TenantId, Status,                   
                        queue              ServerReceivedAtUtc                 
  ----------------------------------------------------------------------------------------------

Do not add INCLUDE columns until representative queries/profiling
justify them.

------------------------------------------------------------------------

# 26. Constraint Catalogue

Critical examples:

-   all tenant-owned rows: `TenantId NOT NULL`.
-   business numbers unique inside tenant/series scope.
-   Booking/Assignment/Commitment intervals: end \> start.
-   PartUsage quantity \> 0.
-   InventoryMovement quantity \> 0 and from ≠ to.
-   JSON fields that are intentionally JSON: `ISJSON(...) = 1`.
-   Posted Invoice requires number, posted instant and seal.
-   issued Credit Note requires number/issue instant.
-   currency code fixed length.
-   tenant-safe composite FKs where materialized target exists.
-   duplicate causation keys prohibited on retry-sensitive physical
    effects.

Do not encode policy that legitimately varies by tenant as an
unconditional CHECK constraint.

------------------------------------------------------------------------

# 27. Foreign Key / Delete Behavior Matrix

Default: `NO ACTION/RESTRICT`.

Cascade only for strict, draft-owned children where physical deletion is
allowed before the aggregate becomes operational.

Never cascade-delete:

-   WorkOrder history
-   Booking/Assignment history
-   field evidence/signatures
-   PartUsage
-   InventoryMovement
-   AuditRecord
-   posted Invoice/InvoiceLine
-   CreditNote
-   settlement ledger
-   idempotency/inbox/outbox evidence needed for retention

Cross-module stable identity FKs use `(TenantId, Id)` and `NO ACTION`.

------------------------------------------------------------------------

# 28. Precision / Type Standards

Final R1 standards:

-   GUID: `uniqueidentifier`
-   UTC instant: `datetime2(3)`
-   local date: `date`
-   timezone: `varchar(100)`
-   money: `decimal(19,4)`
-   quantity: `decimal(18,4)`
-   percentage/rate: `decimal(9,6)` default
-   coordinate: `decimal(9,6)`
-   SHA-256: `binary(32)`
-   JSON: `nvarchar(max)` + ISJSON
-   short machine code: bounded `varchar`
-   human text: `nvarchar`
-   concurrency: `rowversion`
-   status enum: `smallint` for stable internal states unless a concrete
    reason requires otherwise

------------------------------------------------------------------------

# 29. EF Core Mapping Guidance

1.  Use module-specific `DbContext` boundaries for substantial modules
    as defined by ADR-001.
2.  Each entity uses `IEntityTypeConfiguration<T>`.
3.  Configure schema explicitly.
4.  Configure `(TenantId, Id)` alternate keys for tenant-safe FK
    targets.
5.  Apply global tenant filters to tenant-owned entities.
6.  Use `.IsRowVersion()` for concurrency columns.
7.  Flatten small stable value objects (Address, Money components where
    appropriate).
8.  Use separate tables for searchable multi-valued business data.
9.  JSON only for document-shaped snapshots/definitions/payloads.
10. Stable internal statuses map to compact numeric values; keep
    conversion centralized and tested.
11. Do not expose cross-module navigation collections that encourage
    aggregate traversal/mutation.
12. Append-only tables are written through dedicated insert services; do
    not rely on `ValueGeneratedOnAdd()` for immutability.
13. Restrict delete behavior explicitly; do not accept EF cascade
    defaults accidentally.

------------------------------------------------------------------------

# 30. Migration Strategy

## 30.1 Code-first ownership

EF Core Code First remains the implementation approach.

The PDM is the approved design contract; migrations are the executable
schema history.

## 30.2 Migration sequencing

Do **not** generate one giant `001_Initial_Servexa_R1` containing every
module.

Preferred implementation sequence:

1.  `platform` foundation
2.  customers + assets
3.  service
4.  scheduling
5.  field execution
6.  inventory
7.  billing
8.  reliability/read-model refinements

Each feature branch contains the entity/configuration changes and its
corresponding migration(s).

## 30.3 Review

Every migration review checks:

-   unexpected cascade deletes
-   wrong nullable columns
-   missing tenant-safe keys/FKs
-   accidental table/column rename interpreted as drop/create
-   index width/order
-   default constraints
-   destructive operations
-   generated SQL

## 30.4 Production

Generate reviewed idempotent deployment scripts/bundles appropriate to
the CI/CD environment. Production schema changes are forward-fix
oriented. Destructive rollback is not the normal recovery strategy.

Never call `Database.Migrate()` blindly as the production deployment
mechanism.

------------------------------------------------------------------------

# 31. Physical Design Risks / Deferred Questions

No R1 blocker remains in this baseline.

Controlled deferred items:

1.  Inventory valuation method --- R2 Job Costing.
2.  Jurisdiction-specific invoice numbering/e-invoicing ---
    localization/compliance profile.
3.  Advanced partitioning/compression --- only after measured scale.
4.  SQL Server RLS --- optional defense-in-depth after core tenant-safe
    design.
5.  Specialized clustered indexes for very high-volume ledgers ---
    performance evidence.
6.  Payment gateway persistence --- R2/integration-specific.

------------------------------------------------------------------------

# 32. Validation Against Approved Logical Model / ADRs

### Domain separations preserved

-   Account ≠ Contact ≠ Site.
-   Asset ≠ EquipmentModel.
-   ServiceRequest ≠ WorkOrder.
-   WorkOrder ≠ Booking.
-   Booking ≠ ResourceAssignment.
-   Assignment completion ≠ Booking completion ≠ WorkOrder operational
    completion.
-   Operational completion ≠ Financial closure.
-   Resource ≠ TenantUser.
-   PartUsage ≠ InventoryMovement ≠ ChargeLine ≠ CostEntry.
-   Reservation ≠ consumption.
-   posted Invoice correction = Credit Note/rebill, not edit.
-   offline physical evidence survives server-state conflicts.

### Architecture preserved

-   modular monolith.
-   one SQL Server initially.
-   module ownership.
-   shared-table multi-tenancy with physical tenant safety.
-   optimistic concurrency.
-   SQL-local scheduling invariant.
-   transactional outbox.
-   idempotency.
-   no Redis correctness dependency.
-   no broker requirement R1.
-   no event sourcing.
-   no generic workflow engine.
-   no premature future tables.

### R1 integrity tests required before merge

1.  cross-tenant FK/reference attempts fail.
2.  global tenant query filter prevents accidental reads.
3.  two overlapping resource commits: only one succeeds.
4.  replayed offline PartUsage does not double-consume stock.
5.  duplicate webhook/inbox event produces one effect.
6.  outbox crash/retry does not duplicate downstream effect.
7.  posted invoice mutation is rejected.
8.  credit correction preserves original invoice.
9.  inventory ledger rebuild matches StockBalance projection.
10. offline serial conflict preserves evidence and raises
    reconciliation.
11. stale `rowversion` update returns concurrency conflict.
12. forbidden cascade paths do not delete historical/financial evidence.

------------------------------------------------------------------------

# 33. Implementation Handoff

An implementation agent may now build **one bounded module/slice at a
time**.

It may:

-   create solution/module structure;
-   create domain/persistence classes for the approved slice;
-   create EF Core configurations;
-   create module DbContext;
-   create tests for physical invariants;
-   generate the migration for that slice;
-   inspect generated migration/SQL.

It may **not**:

-   invent new entities/modules;
-   change package versions without approval;
-   replace UUIDv7/tenant/concurrency strategy;
-   introduce microservices/brokers/Redis correctness;
-   add future R2/R3/R4 tables;
-   weaken tenant-safe relationships;
-   auto-run production migrations;
-   silently change the approved PDM.

Recommended first implementation branch:

`feature/r0-platform-foundation`

Suggested commit progression:

1.  `chore: bootstrap Servexa solution and module boundaries`
2.  `feat(platform): add tenancy and identity persistence model`
3.  `feat(platform): add authorization and policy persistence`
4.  `feat(platform): add audit idempotency and outbox persistence`
5.  `chore(db): add platform foundation migration`
6.  `test(platform): verify tenant isolation and persistence invariants`

Then self-review/CI/PR → `main`.

------------------------------------------------------------------------

# 34. Final Verdict

## READY FOR CONTROLLED CODE-FIRST IMPLEMENTATION

Servexa's Physical Data Model is approved as the persistence baseline
for R0/R1.

The model is intentionally conservative: proven SQL Server relational
design, explicit tenant safety, small consistency boundaries,
append-only provenance, SQL-local concurrency for hard invariants, and
release-aware materialization.

The earlier candidate's strong decisions---UUIDv7, module schemas,
`TenantId`, `rowversion`, outbox/idempotency, relational-first modeling,
offline evidence preservation, inventory ledger, financial compensation
and Code First---are retained.

The candidate's implementation-risk issues have been corrected:

-   cross-module FK policy is consistent;
-   scheduling serialization is race-safe and explicitly technical;
-   offline inventory discrepancy is physically representable;
-   settlement calculations are internally consistent;
-   invoice immutability has a real enforcement model;
-   enum/timestamp/hash standards are precise;
-   append-only semantics are not confused with EF value generation;
-   R1 physical coverage and migration sequencing are complete enough to
    begin implementation.

**Next action:** establish the GitHub/engineering workflow baseline and
implement `feature/r0-platform-foundation`. Do not reopen PRD, Event
Storming, ADRs or the Logical Data Model unless implementation reveals a
genuine contradiction.
