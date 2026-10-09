using Microsoft.EntityFrameworkCore;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Service.Commands.TransitionWorkOrderStatus;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Assets.Entities;
using Servexa.Domain.Assets.Enums;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;
using Servexa.Infrastructure.Persistence;

namespace Servexa.ArchitectureTests;

public class AssetAndWorkOrderVerticalSliceTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static readonly Guid SiteA = Guid.NewGuid();
    private static readonly Guid SiteB = Guid.NewGuid();
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid AccountB = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();

    [Fact]
    public void EquipmentModel_DomainInvariants_ShouldEnforceRequiredFields()
    {
        var model = new EquipmentModel(
            tenantId: TenantA,
            manufacturerName: "Trane",
            modelCode: "CVHE-500",
            displayName: "Centrifugal Chiller 500T",
            categoryCode: "HVAC",
            trackingPolicy: EquipmentTrackingPolicy.Serialized);

        Assert.Equal(TenantA, model.TenantId);
        Assert.Equal("Trane", model.ManufacturerName);
        Assert.Equal("CVHE-500", model.ModelCode);
        Assert.Equal(EquipmentModelStatus.Active, model.Status);
        Assert.Equal(EquipmentTrackingPolicy.Serialized, model.TrackingPolicy);

        // Retiring model
        model.Retire();
        Assert.Equal(EquipmentModelStatus.Retired, model.Status);

        // Reactivating model
        model.Reactivate();
        Assert.Equal(EquipmentModelStatus.Active, model.Status);

        // Empty tenant ID rejection
        Assert.Throws<ArgumentException>(() => new EquipmentModel(
            tenantId: Guid.Empty,
            manufacturerName: "Trane",
            modelCode: "CVHE-500",
            displayName: "Centrifugal Chiller 500T",
            categoryCode: "HVAC"));
    }

    [Fact]
    public void Asset_DomainLifecycleTransitions_ShouldFollowApprovedStateMachine()
    {
        var modelId = Guid.NewGuid();
        var asset = new Asset(
            tenantId: TenantA,
            assetNumber: "AST-0001",
            equipmentModelId: modelId,
            serialNumber: "SN-998877",
            currentSiteId: SiteA,
            currentOwnerAccountId: AccountA,
            status: AssetStatus.PreInstallation);

        Assert.Equal(AssetStatus.PreInstallation, asset.Status);
        Assert.Null(asset.InstalledAtUtc);

        // Commissioning
        asset.Commission();
        Assert.Equal(AssetStatus.Active, asset.Status);
        Assert.NotNull(asset.InstalledAtUtc);

        // Degrade
        asset.MarkDegraded("Compressor bearing vibration elevated");
        Assert.Equal(AssetStatus.Degraded, asset.Status);

        // Restore to Active
        asset.RestoreActive();
        Assert.Equal(AssetStatus.Active, asset.Status);

        // Decommission
        asset.Decommission();
        Assert.Equal(AssetStatus.Decommissioned, asset.Status);
        Assert.NotNull(asset.DecommissionedAtUtc);

        // Cannot degrade while decommissioned
        Assert.Throws<InvalidOperationException>(() => asset.MarkDegraded());

        // Reactivate
        asset.Reactivate();
        Assert.Equal(AssetStatus.Active, asset.Status);
        Assert.Null(asset.DecommissionedAtUtc);

        // Decommission and then Replace
        asset.Decommission();
        asset.Replace();
        Assert.Equal(AssetStatus.Replaced, asset.Status);

        // Terminal replaced state cannot reactivate or commission
        Assert.Throws<InvalidOperationException>(() => asset.Reactivate());
        Assert.Throws<InvalidOperationException>(() => asset.Commission());
    }

    [Fact]
    public void WorkOrder_OperationalLifecycle_ShouldFollowApprovedStateMachineStrictly()
    {
        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-0001",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "Emergency chiller shutdown on circuit 2",
            billToSnapshotJson: "{\"Account\":\"Acme\"}");

        Assert.Equal(TenantA, workOrder.TenantId);
        Assert.Equal(WorkOrderOperationalStatus.Draft, workOrder.OperationalStatus);
        Assert.Single(workOrder.StatusHistory);
        Assert.Equal(WorkOrderOperationalStatus.Draft, workOrder.StatusHistory.First().FromStatus);
        Assert.Equal(WorkOrderOperationalStatus.Draft, workOrder.StatusHistory.First().ToStatus);

        // Invalid direct transition to InProgress from Draft
        Assert.Throws<InvalidOperationException>(() => workOrder.StartProgress(UserA));

        // Draft -> Approve
        workOrder.Approve(UserA);
        Assert.Equal(WorkOrderOperationalStatus.Approved, workOrder.OperationalStatus);
        Assert.Equal(2, workOrder.StatusHistory.Count);

        // Approved -> Scheduled
        workOrder.MarkScheduled(UserA);
        Assert.Equal(WorkOrderOperationalStatus.Scheduled, workOrder.OperationalStatus);

        // Scheduled -> InProgress
        workOrder.StartProgress(UserA);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, workOrder.OperationalStatus);

        // InProgress -> Paused
        workOrder.Pause("WAIT_PARTS", "Waiting on OEM relief valve", UserA);
        Assert.Equal(WorkOrderOperationalStatus.Paused, workOrder.OperationalStatus);
        Assert.Equal("WAIT_PARTS", workOrder.PauseReasonCode);
        Assert.Equal("Waiting on OEM relief valve", workOrder.PauseNote);

        // Paused -> InProgress
        workOrder.StartProgress(UserA);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, workOrder.OperationalStatus);
        Assert.Null(workOrder.PauseReasonCode);
        Assert.Null(workOrder.PauseNote);

        // InProgress -> OperationallyComplete
        workOrder.Complete(UserA);
        Assert.Equal(WorkOrderOperationalStatus.OperationallyComplete, workOrder.OperationalStatus);
        Assert.NotNull(workOrder.OperationallyCompletedAtUtc);

        // OperationallyComplete cannot be cancelled directly
        Assert.Throws<InvalidOperationException>(() => workOrder.Cancel("Mistake", UserA));

        // OperationallyComplete -> Reopen -> Approved
        workOrder.Reopen("Leak recurred during pressure test", UserA);
        Assert.Equal(WorkOrderOperationalStatus.Approved, workOrder.OperationalStatus);
        Assert.Null(workOrder.OperationallyCompletedAtUtc);

        // Approved -> Cancel
        workOrder.Cancel("Customer cancelled contract", UserA);
        Assert.Equal(WorkOrderOperationalStatus.Cancelled, workOrder.OperationalStatus);

        // History count matches all lifecycle stages (1 initial draft + 8 transitions)
        Assert.Equal(9, workOrder.StatusHistory.Count);
    }

    [Fact]
    public void WorkOrder_AssetAttachment_ShouldAttachAndRejectDuplicateInScope()
    {
        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-0002",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "PREVENTIVE",
            priority: WorkOrderPriority.Standard,
            summary: "Annual chiller PM",
            billToSnapshotJson: "{}");

        var assetId = Guid.NewGuid();
        workOrder.AttachAsset(assetId, SiteA, WorkOrderAssetRole.Primary);

        Assert.Single(workOrder.Assets);
        var attached = workOrder.Assets.First();
        Assert.Equal(assetId, attached.AssetId);
        Assert.Equal(WorkOrderAssetRole.Primary, attached.Role);
        Assert.Equal(WorkOrderAssetStatus.InScope, attached.Status);

        // Cannot attach duplicate in-scope asset
        Assert.Throws<InvalidOperationException>(() => workOrder.AttachAsset(assetId, SiteA, WorkOrderAssetRole.Included));
    }

    [Fact]
    public void AssetAndWorkOrder_TenantIsolation_EntitiesMustPreserveTenantBoundaries()
    {
        var assetA = new Asset(
            tenantId: TenantA,
            assetNumber: "AST-A001",
            equipmentModelId: Guid.NewGuid(),
            status: AssetStatus.Active);

        var assetB = new Asset(
            tenantId: TenantB,
            assetNumber: "AST-B001",
            equipmentModelId: Guid.NewGuid(),
            status: AssetStatus.Active);

        Assert.Equal(TenantA, assetA.TenantId);
        Assert.Equal(TenantB, assetB.TenantId);
        Assert.NotEqual(assetA.TenantId, assetB.TenantId);
    }

    #region B-2 Regression Tests: Illegal Draft -> Scheduled Transition

    [Fact]
    public void WorkOrder_MarkScheduled_FromDraft_MustThrowInvalidOperationException()
    {
        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-B2-001",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "Testing illegal transition",
            billToSnapshotJson: "{}");

        Assert.Equal(WorkOrderOperationalStatus.Draft, workOrder.OperationalStatus);

        // Direct transition from Draft to Scheduled must be rejected at domain layer
        var ex = Assert.Throws<InvalidOperationException>(() => workOrder.MarkScheduled(UserA));
        Assert.Contains("Approved", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Draft -> Approved -> Scheduled is the approved flow
        workOrder.Approve(UserA);
        Assert.Equal(WorkOrderOperationalStatus.Approved, workOrder.OperationalStatus);

        workOrder.MarkScheduled(UserA);
        Assert.Equal(WorkOrderOperationalStatus.Scheduled, workOrder.OperationalStatus);
    }

    #endregion

    #region B-4 Regression Tests: Reopen Reason Enforced

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WorkOrder_Reopen_WithoutValidReason_MustThrowArgumentException(string? invalidReason)
    {
        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-B4-001",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "Testing reopen reason requirement",
            billToSnapshotJson: "{}");

        workOrder.Approve(UserA);
        workOrder.MarkScheduled(UserA);
        workOrder.StartProgress(UserA);
        workOrder.Complete(UserA);

        Assert.Equal(WorkOrderOperationalStatus.OperationallyComplete, workOrder.OperationalStatus);

        // Reopening with null, empty, or whitespace reason must be rejected
        Assert.ThrowsAny<ArgumentException>(() => workOrder.Reopen(invalidReason!, UserA));

        // Reopening with a valid non-empty reason succeeds and records the reason in the status ledger
        const string validReason = "Customer reported recurring vibration after completion";
        workOrder.Reopen(validReason, UserA);

        Assert.Equal(WorkOrderOperationalStatus.Approved, workOrder.OperationalStatus);
        var latestHistory = workOrder.StatusHistory.Last();
        Assert.Equal(WorkOrderOperationalStatus.OperationallyComplete, latestHistory.FromStatus);
        Assert.Equal(WorkOrderOperationalStatus.Approved, latestHistory.ToStatus);
        Assert.Equal(validReason, latestHistory.Reason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TransitionWorkOrderStatusCommandHandler_ReopenWithoutReason_MustThrowValidationException(string? invalidReason)
    {
        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-B4-002",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "Testing handler reopen validation",
            billToSnapshotJson: "{}");

        workOrder.Approve(UserA);
        workOrder.MarkScheduled(UserA);
        workOrder.StartProgress(UserA);
        workOrder.Complete(UserA);

        var repo = new FakeWorkOrderRepository(workOrder);
        var accountRepo = new FakeAccountRepository();
        var siteRepo = new FakeSiteRepository();
        var assetRepo = new FakeAssetRepository();
        var uow = new FakeUnitOfWork();
        var tenantContext = new FakeTenantContext(TenantA, UserA);

        var handler = new TransitionWorkOrderStatusCommandHandler(repo, accountRepo, siteRepo, assetRepo, uow, tenantContext);

        var command = new TransitionWorkOrderStatusCommand(
            workOrder.Id,
            WorkOrderOperationalStatus.Approved,
            null,
            null,
            invalidReason);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("reopening", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TransitionWorkOrderStatusCommandHandler_CancelWithoutReason_MustThrowValidationException(string? invalidReason)
    {
        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-B1-001",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "Testing handler cancel validation",
            billToSnapshotJson: "{}");

        var repo = new FakeWorkOrderRepository(workOrder);
        var accountRepo = new FakeAccountRepository();
        var siteRepo = new FakeSiteRepository();
        var assetRepo = new FakeAssetRepository();
        var uow = new FakeUnitOfWork();
        var tenantContext = new FakeTenantContext(TenantA, UserA);

        var handler = new TransitionWorkOrderStatusCommandHandler(repo, accountRepo, siteRepo, assetRepo, uow, tenantContext);

        var command = new TransitionWorkOrderStatusCommand(
            workOrder.Id,
            WorkOrderOperationalStatus.Cancelled,
            null,
            null,
            invalidReason);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("cancelling", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region B-3 Regression Tests: Asset Lifecycle Event Append-Only Ledger

    [Fact]
    public void Asset_LifecycleEvents_ShouldAppendAndPreserveAuditReasonsAndActors()
    {
        var modelId = Guid.NewGuid();
        var asset = new Asset(
            tenantId: TenantA,
            assetNumber: "AST-B3-001",
            equipmentModelId: modelId,
            serialNumber: "SN-112233",
            currentSiteId: SiteA,
            currentOwnerAccountId: AccountA,
            status: AssetStatus.PreInstallation,
            actorUserId: UserA);

        // Initial registration creates Registered event
        Assert.Single(asset.LifecycleEvents);
        var regEvent = asset.LifecycleEvents.First();
        Assert.Equal(AssetLifecycleEventType.Registered, regEvent.EventType);
        Assert.Null(regEvent.PreviousStatus);
        Assert.Equal(AssetStatus.PreInstallation, regEvent.NewStatus);
        Assert.Equal(UserA, regEvent.ActorUserId);

        // Commission
        const string commissionReason = "Site acceptance test passed";
        asset.Commission(reason: commissionReason, actorUserId: UserA);
        Assert.Equal(2, asset.LifecycleEvents.Count);
        var commEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Commissioned, commEvent.EventType);
        Assert.Equal(AssetStatus.PreInstallation, commEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Active, commEvent.NewStatus);
        Assert.Equal(commissionReason, commEvent.Reason);
        Assert.Equal(UserA, commEvent.ActorUserId);

        // Degrade
        const string degradeReason = "Bearing high temp alarm";
        asset.MarkDegraded(degradeReason, UserA);
        Assert.Equal(3, asset.LifecycleEvents.Count);
        var degEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Degraded, degEvent.EventType);
        Assert.Equal(AssetStatus.Active, degEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Degraded, degEvent.NewStatus);
        Assert.Equal(degradeReason, degEvent.Reason);

        // Restore Active
        const string restoreReason = "Lubricant replaced and recalibrated";
        asset.RestoreActive(restoreReason, UserA);
        Assert.Equal(4, asset.LifecycleEvents.Count);
        var restEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Restored, restEvent.EventType);
        Assert.Equal(AssetStatus.Degraded, restEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Active, restEvent.NewStatus);
        Assert.Equal(restoreReason, restEvent.Reason);

        // Move to Site
        const string moveReason = "Facility consolidation";
        asset.MoveToSite(SiteB, moveReason, UserA);
        Assert.Equal(5, asset.LifecycleEvents.Count);
        var moveEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Moved, moveEvent.EventType);
        Assert.Equal(SiteA, moveEvent.FromSiteId);
        Assert.Equal(SiteB, moveEvent.ToSiteId);
        Assert.Equal(moveReason, moveEvent.Reason);

        // Assign Owner Account
        const string ownerReason = "Asset leased to subsidiary";
        asset.AssignOwnerAccount(AccountB, ownerReason, UserA);
        Assert.Equal(6, asset.LifecycleEvents.Count);
        var ownEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.OwnershipChanged, ownEvent.EventType);
        Assert.Equal(AccountA, ownEvent.FromOwnerAccountId);
        Assert.Equal(AccountB, ownEvent.ToOwnerAccountId);
        Assert.Equal(ownerReason, ownEvent.Reason);

        // Decommission
        const string decommReason = "Replaced with higher efficiency unit";
        asset.Decommission(decommReason, UserA);
        Assert.Equal(7, asset.LifecycleEvents.Count);
        var decommEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Decommissioned, decommEvent.EventType);
        Assert.Equal(AssetStatus.Active, decommEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Decommissioned, decommEvent.NewStatus);
        Assert.Equal(decommReason, decommEvent.Reason);

        // Reactivate
        const string reactivateReason = "Emergency backup deployment";
        asset.Reactivate(reactivateReason, UserA);
        Assert.Equal(8, asset.LifecycleEvents.Count);
        var reactEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Reactivated, reactEvent.EventType);
        Assert.Equal(AssetStatus.Decommissioned, reactEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Active, reactEvent.NewStatus);
        Assert.Equal(reactivateReason, reactEvent.Reason);

        // Decommission and Replace
        asset.Decommission("Decommissioned before scrap", UserA);
        const string replaceReason = "End of life scrappage";
        asset.Replace(replaceReason, UserA);
        Assert.Equal(10, asset.LifecycleEvents.Count);
        var repEvent = asset.LifecycleEvents.Last();
        Assert.Equal(AssetLifecycleEventType.Replaced, repEvent.EventType);
        Assert.Equal(AssetStatus.Decommissioned, repEvent.PreviousStatus);
        Assert.Equal(AssetStatus.Replaced, repEvent.NewStatus);
        Assert.Equal(replaceReason, repEvent.Reason);
    }

    [Fact]
    public async Task Asset_LifecycleEvents_Persistence_ShouldAtomicallySaveEventsToLedger()
    {
        var options = new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ServexaDbContext(options);

        var model = new EquipmentModel(
            tenantId: TenantA,
            manufacturerName: "Carrier",
            modelCode: "19XR",
            displayName: "Centrifugal Chiller 600T",
            categoryCode: "HVAC");
        await dbContext.EquipmentModels.AddAsync(model);

        var asset = new Asset(
            tenantId: TenantA,
            assetNumber: "AST-B3-PERSIST",
            equipmentModelId: model.Id,
            serialNumber: "SN-99999",
            currentSiteId: SiteA,
            currentOwnerAccountId: AccountA,
            status: AssetStatus.PreInstallation,
            actorUserId: UserA);

        asset.Commission(reason: "Commissioning on roof plant", actorUserId: UserA);
        asset.MarkDegraded("Oil leak detected", UserA);

        await dbContext.Assets.AddAsync(asset);
        await dbContext.SaveChangesAsync();

        // Query back from fresh context to verify persistence
        await using var verifyContext = new ServexaDbContext(options);
        var retrieved = await verifyContext.Assets
            .Include(a => a.LifecycleEvents)
            .FirstOrDefaultAsync(a => a.TenantId == TenantA && a.AssetNumber == "AST-B3-PERSIST");

        Assert.NotNull(retrieved);
        Assert.Equal(AssetStatus.Degraded, retrieved.Status);
        Assert.Equal(3, retrieved.LifecycleEvents.Count);
        Assert.Contains(retrieved.LifecycleEvents, e => e.EventType == AssetLifecycleEventType.Registered);
        Assert.Contains(retrieved.LifecycleEvents, e => e.EventType == AssetLifecycleEventType.Commissioned && e.Reason == "Commissioning on roof plant");
        Assert.Contains(retrieved.LifecycleEvents, e => e.EventType == AssetLifecycleEventType.Degraded && e.Reason == "Oil leak detected");
    }

    #endregion

    #region Supporting Test Fakes

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeTenantContext(
        Guid tenantId,
        Guid? userId = null,
        string? email = null,
        string? displayName = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? permissions = null) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public Guid? UserId => userId;
        public string? UserEmail => email;
        public string? DisplayName => displayName;
        public IReadOnlyList<string> Roles => roles ?? [];
        public IReadOnlyList<string> Permissions => permissions ?? [];
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class FakeWorkOrderRepository : IWorkOrderRepository
    {
        private readonly List<WorkOrder> _items = [];

        public FakeWorkOrderRepository(params WorkOrder[] items)
        {
            _items.AddRange(items);
        }

        public Task<WorkOrder?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(w => w.TenantId == tenantId && w.Id == id));

        public Task<WorkOrder?> GetByNumberAsync(Guid tenantId, string workOrderNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.FirstOrDefault(w => w.TenantId == tenantId && w.WorkOrderNumber.Equals(workOrderNumber, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<WorkOrder>> GetWorkOrdersAsync(Guid tenantId, Guid? serviceAccountId, Guid? primarySiteId, short? operationalStatus, short? priority, string? search, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WorkOrder>>(_items.Where(w => w.TenantId == tenantId).Skip(skip).Take(take).ToList());

        public Task<int> GetCountAsync(Guid tenantId, Guid? serviceAccountId, Guid? primarySiteId, short? operationalStatus, short? priority, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Count(w => w.TenantId == tenantId));

        public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
        {
            _items.Add(workOrder);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid tenantId, string workOrderNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Any(w => w.TenantId == tenantId && w.WorkOrderNumber.Equals(workOrderNumber, StringComparison.OrdinalIgnoreCase)));
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public Task<Account?> GetByIdAsync(Guid tenantId, Guid accountId, CancellationToken cancellationToken = default)
            => Task.FromResult<Account?>(null);
        public Task<Account?> GetByAccountNumberAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default)
            => Task.FromResult<Account?>(null);
        public Task<IReadOnlyList<Account>> GetAccountsAsync(Guid tenantId, string? search, int skip, int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Account>>([]);
        public Task<int> GetCountAsync(Guid tenantId, string? search, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
        public Task AddAsync(Account account, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid tenantId, string accountNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class FakeSiteRepository : ISiteRepository
    {
        public Task<Site?> GetByIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default) => Task.FromResult<Site?>(null);
        public Task<Site?> GetBySiteNumberAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default) => Task.FromResult<Site?>(null);
        public Task<IReadOnlyList<Site>> GetSitesAsync(Guid tenantId, Guid? branchId, Guid? accountId, string? search, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Site>>([]);
        public Task<int> GetCountAsync(Guid tenantId, Guid? branchId, Guid? accountId, string? search, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task AddAsync(Site site, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddRelationshipAsync(SiteAccountRelationship relationship, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid tenantId, string siteNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<SiteAccountRelationship>> GetRelationshipsBySiteIdAsync(Guid tenantId, Guid siteId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<SiteAccountRelationship>>([]);
    }

    private sealed class FakeAssetRepository : IAssetRepository
    {
        public Task<Asset?> GetByIdAsync(Guid tenantId, Guid id, bool asNoTracking = true, CancellationToken cancellationToken = default) => Task.FromResult<Asset?>(null);
        public Task<Asset?> GetByAssetNumberAsync(Guid tenantId, string assetNumber, CancellationToken cancellationToken = default) => Task.FromResult<Asset?>(null);
        public Task<IReadOnlyList<Asset>> GetAssetsAsync(Guid tenantId, Guid? siteId, Guid? ownerAccountId, Guid? equipmentModelId, string? search, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Asset>>([]);
        public Task<int> GetCountAsync(Guid tenantId, Guid? siteId, Guid? ownerAccountId, Guid? equipmentModelId, string? search, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task AddAsync(Asset asset, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(Guid tenantId, string assetNumber, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    #endregion
}
