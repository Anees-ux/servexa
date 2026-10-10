using Microsoft.EntityFrameworkCore;
using Servexa.Application.Assets.Repositories;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Repositories;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Commands.CompleteWorkOrder;
using Servexa.Application.Service.Queries.EvaluateWorkOrderCompletion;
using Servexa.Application.Service.Repositories;
using Servexa.Application.Service.Services;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Customers.Enums;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Field.Enums;
using Servexa.Domain.Platform.Constants;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;
using Servexa.Infrastructure.Persistence;
using Servexa.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Servexa.ArchitectureTests;

public class WorkOrderCompletionEvaluationTests
{
    private static DbContextOptions<ServexaDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private sealed class TestTenantContext(Guid tenantId, Guid userId, params string[] permissions) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public Guid? UserId => userId;
        public string? UserEmail => "tester@servexa.local";
        public string? DisplayName => "Test User";
        public IReadOnlyList<string> Roles => ["Dispatcher"];
        public IReadOnlyList<string> Permissions => permissions.Length > 0 ? permissions : [Capabilities.WorkOrderView, Capabilities.WorkOrderComplete, Capabilities.WorkOrderCreate];
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => Permissions.Contains(permission);
    }

    private static (WorkOrder WO, Booking Bkg, ResourceAssignment Assign, ExecutionSession Session) SeedOperationalChain(
        ServexaDbContext db,
        Guid tenantId,
        Guid userId,
        bool completeBooking = true,
        bool completeSession = true,
        bool addScopeItem = false,
        bool fulfillScopeItem = false)
    {
        var account = new Account(tenantId, "ACC-01", "Acme", "Acme Corp", AccountType.Commercial, AccountStatus.Active, currencyCode: "EUR");
        db.Accounts.Add(account);

        var site = new Site(tenantId, "SIT-01", "HQ", Guid.NewGuid(), "Europe/Helsinki", "Main St", "Helsinki", "Uusimaa", "00100", "FI");
        db.Sites.Add(site);

        var wo = new WorkOrder(
            tenantId,
            $"WO-{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            account.Id,
            account.Id,
            site.Id,
            "MAINT",
            "Annual inspection",
            "{}",
            WorkOrderPriority.Standard);

        wo.Approve(userId);
        wo.MarkScheduled(userId);
        wo.StartProgress(userId);
        db.WorkOrders.Add(wo);

        if (addScopeItem)
        {
            var scopeItem = new WorkOrderScopeItem(
                tenantId,
                wo.Id,
                sequence: 1,
                scopeType: WorkOrderScopeType.Original,
                description: "Inspect hydraulic pressure valve",
                isRequiredForCompletion: true);

            if (fulfillScopeItem)
            {
                scopeItem.Fulfill(userId);
            }

            wo.AddScopeItem(scopeItem);
            db.WorkOrderScopeItems.Add(scopeItem);
        }

        var start = DateTime.UtcNow.AddHours(-2);
        var end = DateTime.UtcNow.AddHours(-1);
        var booking = new Booking(tenantId, $"BKG-{Guid.NewGuid():N}"[..8].ToUpperInvariant(), wo.Id, site.Id, start, end, "Europe/Helsinki", BookingStatus.Scheduled, 1, null, userId);

        var resource = new Resource(tenantId, "TECH-01", "Jane Doe", ResourceType.Technician);
        db.Resources.Add(resource);

        var assignment = new ResourceAssignment(tenantId, booking.Id, resource.Id, start, end, AssignmentRole.Lead);
        booking.AddAssignment(assignment);

        var session = new ExecutionSession(tenantId, assignment.Id, booking.Id, wo.Id, resource.Id, userId);
        session.StartTravel();
        session.ArriveOnSite();
        session.StartWork();

        if (completeSession)
        {
            session.EndSession("Completed full inspection. All systems operating within normal tolerances.");
        }

        if (completeBooking)
        {
            booking.Complete(userId);
        }

        db.Bookings.Add(booking);
        db.ResourceAssignments.Add(assignment);
        db.ExecutionSessions.Add(session);

        db.SaveChanges();

        return (wo, booking, assignment, session);
    }

    [Fact]
    public async Task BookingCompletion_DoesNotAutomaticallyComplete_WorkOrder()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, booking, _, session) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true);

        // Booking is Completed, session is Ended, but Work Order must remain InProgress
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(ExecutionSessionStatus.Ended, session.Status);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, wo.OperationalStatus);
        Assert.Null(wo.OperationallyCompletedAtUtc);
    }

    [Fact]
    public async Task IncompleteBooking_BlocksCompletion_EvaluationReportsUnmetGate()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: false, completeSession: true);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var readiness = await engine.EvaluateAsync(tenantId, wo.Id, userId);

        Assert.False(readiness.IsCompleteEligible);
        Assert.Equal(WorkOrderCompletionOutcome.FollowUpRequired.ToString(), readiness.RecommendedOutcome);
        var bookingGate = Assert.Single(readiness.Gates, g => g.GateName == "BookingCompletion");
        Assert.False(bookingGate.Passed);
        Assert.Contains("All scheduled visits must be completed", bookingGate.BlockingReason);
    }

    [Fact]
    public async Task MissingExecutionEvidence_BlocksCompletion()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: false);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var readiness = await engine.EvaluateAsync(tenantId, wo.Id, userId);

        Assert.False(readiness.IsCompleteEligible);
        var evidenceGate = Assert.Single(readiness.Gates, g => g.GateName == "ExecutionEvidence");
        Assert.False(evidenceGate.Passed);
        Assert.Contains("Operational execution evidence", evidenceGate.BlockingReason);
    }

    [Fact]
    public async Task MissingMandatoryScope_BlocksCompletion_PersistsBlockedEvaluation()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true, addScopeItem: true, fulfillScopeItem: false);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var handler = new CompleteWorkOrderCommandHandler(
            new WorkOrderRepository(db),
            new AccountRepository(db),
            new SiteRepository(db),
            new AssetRepository(db),
            engine,
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderComplete));

        var commandId = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CompleteWorkOrderCommand(wo.Id, commandId, "Attempting closure"), CancellationToken.None));

        Assert.Contains("Required scope item #1", ex.Message);

        // Verify Work Order remains InProgress and blocked evaluation was persisted
        var reloadedWo = await db.WorkOrders.Include(w => w.CompletionEvaluations).FirstAsync(w => w.Id == wo.Id);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, reloadedWo.OperationalStatus);
        Assert.Null(reloadedWo.OperationallyCompletedAtUtc);

        var evaluation = Assert.Single(reloadedWo.CompletionEvaluations);
        Assert.Equal(WorkOrderCompletionOutcome.FollowUpRequired, evaluation.Outcome);
        Assert.Contains("Required scope item #1", evaluation.Summary);
    }

    [Fact]
    public async Task UnauthorizedUser_CannotCompleteWorkOrder()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        // User without Capabilities.WorkOrderComplete
        var handler = new CompleteWorkOrderCommandHandler(
            new WorkOrderRepository(db),
            new AccountRepository(db),
            new SiteRepository(db),
            new AssetRepository(db),
            engine,
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderView));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new CompleteWorkOrderCommand(wo.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task CrossTenantAccess_IsRejected()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        // Handler configured with different tenant
        var handler = new CompleteWorkOrderCommandHandler(
            new WorkOrderRepository(db),
            new AccountRepository(db),
            new SiteRepository(db),
            new AssetRepository(db),
            engine,
            new UnitOfWork(db),
            new TestTenantContext(otherTenantId, userId, Capabilities.WorkOrderComplete));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CompleteWorkOrderCommand(wo.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task SuccessfulEvaluationAndCompletion_TransitionsStatus_AndRecordsEvaluationAndHistory()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true, addScopeItem: true, fulfillScopeItem: true);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var handler = new CompleteWorkOrderCommandHandler(
            new WorkOrderRepository(db),
            new AccountRepository(db),
            new SiteRepository(db),
            new AssetRepository(db),
            engine,
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderComplete));

        var commandId = Guid.NewGuid();
        var result = await handler.Handle(new CompleteWorkOrderCommand(wo.Id, commandId, "Manager final signoff"), CancellationToken.None);

        Assert.Equal(WorkOrderOperationalStatus.OperationallyComplete.ToString(), result.OperationalStatus);
        Assert.NotNull(result.OperationallyCompletedAtUtc);
        Assert.NotNull(result.LatestEvaluation);
        Assert.Equal(WorkOrderCompletionOutcome.OperationallyComplete.ToString(), result.LatestEvaluation!.Outcome);

        // Verify database persistence
        var reloadedWo = await db.WorkOrders
            .Include(w => w.StatusHistory)
            .Include(w => w.CompletionEvaluations)
            .FirstAsync(w => w.Id == wo.Id);

        Assert.Equal(WorkOrderOperationalStatus.OperationallyComplete, reloadedWo.OperationalStatus);
        Assert.NotNull(reloadedWo.OperationallyCompletedAtUtc);

        // Verify status history
        var lastHistory = reloadedWo.StatusHistory.OrderByDescending(h => h.ChangedAtUtc).First();
        Assert.Equal(WorkOrderOperationalStatus.OperationallyComplete, lastHistory.ToStatus);
        Assert.Equal(userId, lastHistory.ChangedByUserId);

        // Verify append-only evaluation record
        var evaluation = Assert.Single(reloadedWo.CompletionEvaluations);
        Assert.Equal(commandId, evaluation.CommandId);
        Assert.Equal(WorkOrderCompletionOutcome.OperationallyComplete, evaluation.Outcome);
        Assert.Equal(userId, evaluation.EvaluatedByUserId);
        Assert.Equal("Manager final signoff", evaluation.Notes);
    }

    [Fact]
    public async Task DuplicateOrAlreadyComplete_IsRejected()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var handler = new CompleteWorkOrderCommandHandler(
            new WorkOrderRepository(db),
            new AccountRepository(db),
            new SiteRepository(db),
            new AssetRepository(db),
            engine,
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderComplete));

        // First completion succeeds
        await handler.Handle(new CompleteWorkOrderCommand(wo.Id, Guid.NewGuid()), CancellationToken.None);

        // Second completion is rejected
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CompleteWorkOrderCommand(wo.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ReopenBehavior_PreservesEvaluationHistory_AccordingToApprovedBaseline()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var (wo, _, _, _) = SeedOperationalChain(db, tenantId, userId, completeBooking: true, completeSession: true);

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var handler = new CompleteWorkOrderCommandHandler(
            new WorkOrderRepository(db),
            new AccountRepository(db),
            new SiteRepository(db),
            new AssetRepository(db),
            engine,
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderComplete));

        // Complete the work order
        await handler.Handle(new CompleteWorkOrderCommand(wo.Id, Guid.NewGuid(), "Initial operational signoff"), CancellationToken.None);

        // Reopen the work order with explicit reason
        var reloadedWo = await db.WorkOrders
            .Include(w => w.CompletionEvaluations)
            .Include(w => w.StatusHistory)
            .FirstAsync(w => w.Id == wo.Id);

        reloadedWo.Reopen("Customer reported recurrent vibration on pump unit", userId);
        await db.SaveChangesAsync();

        // Work order is Approved again, but historical completion evaluation record remains intact
        Assert.Equal(WorkOrderOperationalStatus.Approved, reloadedWo.OperationalStatus);
        Assert.Null(reloadedWo.OperationallyCompletedAtUtc);
        Assert.Single(reloadedWo.CompletionEvaluations);
        Assert.Equal(WorkOrderCompletionOutcome.OperationallyComplete, reloadedWo.CompletionEvaluations.First().Outcome);

        // Status history contains reopen record
        var latestHistory = reloadedWo.StatusHistory.OrderByDescending(h => h.ChangedAtUtc).First();
        Assert.Equal(WorkOrderOperationalStatus.Approved, latestHistory.ToStatus);
        Assert.Contains("Customer reported recurrent vibration", latestHistory.Reason);
    }
}
