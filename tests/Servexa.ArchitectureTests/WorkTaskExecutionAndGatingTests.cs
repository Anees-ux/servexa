using Microsoft.EntityFrameworkCore;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Commands.CreateWorkTask;
using Servexa.Application.Field.Commands.UpdateWorkTaskStatus;
using Servexa.Application.Field.Queries.GetWorkTasks;
using Servexa.Application.Service.Services;
using Servexa.Domain.Assets.Entities;
using Servexa.Domain.Assets.Enums;
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

public class WorkTaskExecutionAndGatingTests
{
    private static DbContextOptions<ServexaDbContext> CreateInMemoryOptions() =>
        new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: $"Servexa_WorkTasks_{Guid.NewGuid()}")
            .Options;

    private static (WorkOrder WorkOrder, Asset PrimaryAsset, Asset SecondaryAsset, Booking Booking, ExecutionSession Session, Guid ModelId)
        SeedFullOperationalChain(ServexaDbContext db, Guid tenantId, Guid userId)
    {
        var account = new Account(tenantId, "ACC-TASK-01", "Task Test Corp", "Task Test Legal", AccountType.Commercial, AccountStatus.Active, currencyCode: "USD");
        var site = new Site(tenantId, "SIT-TASK-01", "Task Test Site", Guid.NewGuid(), "UTC", "100 Task Way", "TechCity", "State", "10001", "US");
        var modelId = Guid.NewGuid();
        var primaryAsset = new Asset(tenantId, "AST-TASK-01", modelId, "SN-001", site.Id, account.Id, AssetStatus.Active);
        var secondaryAsset = new Asset(tenantId, "AST-TASK-02", modelId, "SN-002", site.Id, account.Id, AssetStatus.Active);

        var wo = new WorkOrder(
            tenantId,
            $"WO-{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            account.Id,
            account.Id,
            site.Id,
            "MAINT",
            "Chiller & Pump Overhaul",
            "{}",
            WorkOrderPriority.Standard);

        wo.Approve(userId);
        wo.MarkScheduled(userId);
        wo.StartProgress(userId);
        wo.AttachAsset(primaryAsset.Id, site.Id, WorkOrderAssetRole.Primary);
        wo.AttachAsset(secondaryAsset.Id, site.Id, WorkOrderAssetRole.Included);

        var resource = new Resource(tenantId, "TECH-01", "Technician Alpha", ResourceType.Technician, userId: userId);
        var booking = new Booking(tenantId, $"BK-{Guid.NewGuid():N}"[..8].ToUpperInvariant(), wo.Id, site.Id, DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), "UTC", BookingStatus.Scheduled, 1, null, userId);
        var assignment = new ResourceAssignment(tenantId, booking.Id, resource.Id, DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), AssignmentRole.Lead);
        booking.AddAssignment(assignment);

        var session = new ExecutionSession(tenantId, assignment.Id, booking.Id, wo.Id, resource.Id, userId);
        session.StartTravel();
        session.ArriveOnSite();
        session.StartWork();
        session.EndSession("Completed all mechanical servicing and diagnostics.");
        booking.Complete(userId);

        db.Accounts.Add(account);
        db.Sites.Add(site);
        db.Assets.AddRange(primaryAsset, secondaryAsset);
        db.WorkOrders.Add(wo);
        db.Resources.Add(resource);
        db.Bookings.Add(booking);
        db.ResourceAssignments.Add(assignment);
        db.ExecutionSessions.Add(session);
        db.SaveChanges();

        return (wo, primaryAsset, secondaryAsset, booking, session, modelId);
    }

    [Fact]
    public async Task WorkTask_Create_WithSequenceAndAttribution_PersistsSuccessfully()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var command = new CreateWorkTaskCommand(
            wo.Id,
            "Verify condenser refrigerant pressure",
            "Check suction pressure within 65-75 psi",
            (short)WorkTaskType.Inspection,
            1,
            true,
            (short)WorkTaskGate.WorkOrderCompletion,
            primaryAsset.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Verify condenser refrigerant pressure", result.Title);
        Assert.Equal(WorkTaskType.Inspection.ToString(), result.TaskType);
        Assert.Equal(WorkTaskStatus.Pending.ToString(), result.Status);
        Assert.True(result.IsRequired);
        Assert.Equal(primaryAsset.Id, result.AssetId);

        // Verify query
        var queryHandler = new GetWorkTasksQueryHandler(
            new WorkTaskRepository(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderView));

        var tasks = await queryHandler.Handle(new GetWorkTasksQuery(wo.Id), CancellationToken.None);
        Assert.Single(tasks);
        Assert.Equal(result.Id, tasks[0].Id);
    }

    [Fact]
    public async Task WorkTask_MultiAssetAttribution_BelongsToWorkOrderScope_Succeeds()
    {
        // FIE-011: Multi-asset attribution test
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, secondaryAsset, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        // Create task for Primary Asset
        var task1 = await handler.Handle(new CreateWorkTaskCommand(
            wo.Id,
            "Chiller safety inspection",
            Sequence: 1,
            AssetId: primaryAsset.Id), CancellationToken.None);

        // Create task for Secondary Asset
        var task2 = await handler.Handle(new CreateWorkTaskCommand(
            wo.Id,
            "Pump motor lubrication",
            Sequence: 2,
            AssetId: secondaryAsset.Id), CancellationToken.None);

        Assert.Equal(primaryAsset.Id, task1.AssetId);
        Assert.Equal(secondaryAsset.Id, task2.AssetId);

        // Query filtered by secondary asset
        var queryHandler = new GetWorkTasksQueryHandler(
            new WorkTaskRepository(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderView));

        var pumpTasks = await queryHandler.Handle(new GetWorkTasksQuery(wo.Id, AssetId: secondaryAsset.Id), CancellationToken.None);
        Assert.Single(pumpTasks);
        Assert.Equal("Pump motor lubrication", pumpTasks[0].Title);
    }

    [Fact]
    public async Task WorkTask_MultiAssetAttribution_UnattachedAsset_RejectedWithConflict()
    {
        // FIE-011: Asset outside WO scope is rejected
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, _, _, _, _, modelId) = SeedFullOperationalChain(db, tenantId, userId);

        var unattachedAsset = new Asset(tenantId, "AST-UNATTACHED", modelId, "SN-999", Guid.NewGuid(), Guid.NewGuid(), AssetStatus.Active);
        db.Assets.Add(unattachedAsset);
        db.SaveChanges();

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var command = new CreateWorkTaskCommand(
            wo.Id,
            "Unauthorized asset task",
            AssetId: unattachedAsset.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("is not associated with Work Order", ex.Message);
    }

    [Fact]
    public async Task WorkTask_StatusTransition_StartAndComplete_RecordsActorAndTimestamp()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(tenantId, wo.Id, 1, "Inspect bearings", WorkTaskType.Standard, true, WorkTaskGate.WorkOrderCompletion, assetId: primaryAsset.Id);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.TechnicianExecute));

        // Start task -> InProgress
        var inProgressResult = await handler.Handle(new UpdateWorkTaskStatusCommand(
            wo.Id,
            task.Id,
            (short)WorkTaskStatus.InProgress), CancellationToken.None);

        Assert.Equal(WorkTaskStatus.InProgress.ToString(), inProgressResult.Status);

        // Complete task -> Completed
        var completedAt = DateTime.UtcNow;
        var completeResult = await handler.Handle(new UpdateWorkTaskStatusCommand(
            wo.Id,
            task.Id,
            (short)WorkTaskStatus.Completed,
            CompletedAtUtc: completedAt), CancellationToken.None);

        Assert.Equal(WorkTaskStatus.Completed.ToString(), completeResult.Status);
        Assert.Equal(userId, completeResult.CompletedByUserId);
        Assert.NotNull(completeResult.CompletedAtUtc);
    }

    [Fact]
    public async Task WorkTask_RequiredTask_SkippedWithoutReason_ThrowsException()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, _, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(tenantId, wo.Id, 1, "Mandatory safety check", WorkTaskType.SafetyCheck, true, WorkTaskGate.WorkOrderCompletion);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.TechnicianExecute));

        // Skip without reason on required task should throw
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new UpdateWorkTaskStatusCommand(
                wo.Id,
                task.Id,
                (short)WorkTaskStatus.Skipped,
                SkipReason: ""), CancellationToken.None));

        // Skip with reason succeeds
        var result = await handler.Handle(new UpdateWorkTaskStatusCommand(
            wo.Id,
            task.Id,
            (short)WorkTaskStatus.Skipped,
            SkipReason: "Unit powered down and locked out by customer facility safety team"), CancellationToken.None);

        Assert.Equal(WorkTaskStatus.Skipped.ToString(), result.Status);
        Assert.Equal("Unit powered down and locked out by customer facility safety team", result.SkipReason);
    }

    [Fact]
    public async Task WorkOrderCompletionEngine_PendingRequiredTask_BlocksCompletion()
    {
        // FIE-003: Required inspection can gate completion
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(
            tenantId,
            wo.Id,
            1,
            "Critical Pressure Relief Valve Inspection",
            WorkTaskType.Inspection,
            isRequired: true,
            gate: WorkTaskGate.WorkOrderCompletion,
            assetId: primaryAsset.Id);

        db.WorkTasks.Add(task);
        db.SaveChanges();

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var readiness = await engine.EvaluateAsync(tenantId, wo.Id, userId);

        Assert.False(readiness.IsCompleteEligible);
        Assert.Equal(WorkOrderCompletionOutcome.FollowUpRequired.ToString(), readiness.RecommendedOutcome);

        var taskGate = Assert.Single(readiness.Gates, g => g.GateName == "WorkTasks");
        Assert.False(taskGate.Passed);
        Assert.Contains("Required task #1 ('Critical Pressure Relief Valve Inspection') is in status 'Pending'", taskGate.BlockingReason);
        Assert.Contains("Mandatory tasks, checklists, and inspections compliance check (FIE-003)", taskGate.Description);
    }

    [Fact]
    public async Task WorkOrderCompletionEngine_CompletedRequiredTask_PassesGate()
    {
        // FIE-003: Fulfilling the required task satisfies the gate
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(
            tenantId,
            wo.Id,
            1,
            "Critical Pressure Relief Valve Inspection",
            WorkTaskType.Inspection,
            isRequired: true,
            gate: WorkTaskGate.WorkOrderCompletion,
            assetId: primaryAsset.Id);

        task.Complete(userId);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var readiness = await engine.EvaluateAsync(tenantId, wo.Id, userId);

        Assert.True(readiness.IsCompleteEligible);
        Assert.Equal(WorkOrderCompletionOutcome.OperationallyComplete.ToString(), readiness.RecommendedOutcome);

        var taskGate = Assert.Single(readiness.Gates, g => g.GateName == "WorkTasks");
        Assert.True(taskGate.Passed);
        Assert.Contains("All 1 required work tasks, checklists, and inspections are satisfied", taskGate.Description);
    }

    [Fact]
    public async Task WorkOrderCompletionEngine_SkippedRequiredTaskWithReason_PassesGate()
    {
        // FIE-003: An authorized skipped task also satisfies the gate
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, _, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(
            tenantId,
            wo.Id,
            1,
            "Pre-operational safety test",
            WorkTaskType.SafetyCheck,
            isRequired: true,
            gate: WorkTaskGate.WorkOrderCompletion);

        task.Skip("Facility power unavailable; waived by maintenance manager", userId);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        var engine = new WorkOrderCompletionEngine(
            new WorkOrderRepository(db),
            new BookingRepository(db),
            new ExecutionSessionRepository(db),
            new WorkTaskRepository(db));

        var readiness = await engine.EvaluateAsync(tenantId, wo.Id, userId);

        Assert.True(readiness.IsCompleteEligible);
        var taskGate = Assert.Single(readiness.Gates, g => g.GateName == "WorkTasks");
        Assert.True(taskGate.Passed);
    }

    [Fact]
    public async Task WorkTask_TenantIsolation_CrossTenantAccess_ReturnsNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (woA, _, _, _, _, _) = SeedFullOperationalChain(db, tenantA, userId);

        var taskA = new WorkTask(tenantA, woA.Id, 1, "Tenant A Task", WorkTaskType.Standard);
        db.WorkTasks.Add(taskA);
        db.SaveChanges();

        // User from Tenant B tries to query Tenant A tasks
        var queryHandler = new GetWorkTasksQueryHandler(
            new WorkTaskRepository(db),
            new TestTenantContext(tenantB, userId, Capabilities.WorkOrderView));

        var results = await queryHandler.Handle(new GetWorkTasksQuery(woA.Id), CancellationToken.None);
        Assert.Empty(results);

        // User from Tenant B tries to update Tenant A task
        var updateHandler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantB, userId, Capabilities.TechnicianExecute));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            updateHandler.Handle(new UpdateWorkTaskStatusCommand(
                woA.Id,
                taskA.Id,
                (short)WorkTaskStatus.Completed), CancellationToken.None));
    }

    [Fact]
    public async Task WorkTask_UpdateStatus_ViewOnlyUser_Forbidden()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(tenantId, wo.Id, 1, "Inspect bearings", WorkTaskType.Standard, true, WorkTaskGate.WorkOrderCompletion, assetId: primaryAsset.Id);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderView));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new UpdateWorkTaskStatusCommand(
                wo.Id,
                task.Id,
                (short)WorkTaskStatus.Completed), CancellationToken.None));

        Assert.Contains("User lacks permission to update task status", ex.Message);
    }

    [Fact]
    public async Task WorkTask_UpdateStatus_AuthorizedManager_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var managerUserId = Guid.NewGuid();
        var techUserId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, techUserId);

        var task = new WorkTask(tenantId, wo.Id, 1, "Manager override task", WorkTaskType.Standard, true, WorkTaskGate.WorkOrderCompletion, assetId: primaryAsset.Id);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        // Manager does not have a linked Resource, but has WorkOrderCreate capability
        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, managerUserId, Capabilities.WorkOrderCreate));

        var result = await handler.Handle(new UpdateWorkTaskStatusCommand(
            wo.Id,
            task.Id,
            (short)WorkTaskStatus.Completed), CancellationToken.None);

        Assert.Equal(WorkTaskStatus.Completed.ToString(), result.Status);
        Assert.Equal(managerUserId, result.CompletedByUserId);
    }

    [Fact]
    public async Task WorkTask_UpdateStatus_AssignedTechnician_Succeeds()
    {
        var tenantId = Guid.NewGuid();
        var techUserId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, booking, _, _) = SeedFullOperationalChain(db, tenantId, techUserId);

        var leadAssignment = booking.Assignments.First();
        var task = new WorkTask(
            tenantId,
            wo.Id,
            1,
            "Calibrate pressure transducer",
            WorkTaskType.Standard,
            isRequired: true,
            gate: WorkTaskGate.WorkOrderCompletion,
            assetId: primaryAsset.Id,
            assignmentId: leadAssignment.Id,
            bookingId: booking.Id);

        db.WorkTasks.Add(task);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, techUserId, Capabilities.TechnicianExecute));

        var result = await handler.Handle(new UpdateWorkTaskStatusCommand(
            wo.Id,
            task.Id,
            (short)WorkTaskStatus.InProgress), CancellationToken.None);

        Assert.Equal(WorkTaskStatus.InProgress.ToString(), result.Status);
    }

    [Fact]
    public async Task WorkTask_UpdateStatus_UnassignedTechnician_Forbidden()
    {
        var tenantId = Guid.NewGuid();
        var tech1UserId = Guid.NewGuid();
        var tech2UserId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, booking, _, _) = SeedFullOperationalChain(db, tenantId, tech1UserId);

        // Register tech2 as an active resource, but NOT assigned to this WorkOrder
        var unassignedResource = new Resource(tenantId, "TECH-02", "Technician Beta", ResourceType.Technician, userId: tech2UserId);
        db.Resources.Add(unassignedResource);
        db.SaveChanges();

        var task = new WorkTask(tenantId, wo.Id, 1, "Tech1 specific task", WorkTaskType.Standard, true, WorkTaskGate.WorkOrderCompletion, assetId: primaryAsset.Id);
        db.WorkTasks.Add(task);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, tech2UserId, Capabilities.TechnicianExecute));

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new UpdateWorkTaskStatusCommand(
                wo.Id,
                task.Id,
                (short)WorkTaskStatus.InProgress), CancellationToken.None));

        Assert.Contains("Technician is not assigned to this work order", ex.Message);

        // Now test where tech2 IS assigned to the work order on assignment 2, but the task is explicitly attributed to assignment 1 (tech1)
        var assignment2 = new ResourceAssignment(tenantId, booking.Id, unassignedResource.Id, DateTime.UtcNow.AddHours(-3), DateTime.UtcNow.AddHours(-1), AssignmentRole.Support);
        booking.AddAssignment(assignment2);
        db.ResourceAssignments.Add(assignment2);

        var assignment1 = booking.Assignments.First(a => a.ResourceId != unassignedResource.Id);
        var taskAttributedToTech1 = new WorkTask(
            tenantId,
            wo.Id,
            2,
            "Exclusive lead tech task",
            WorkTaskType.Standard,
            isRequired: true,
            gate: WorkTaskGate.WorkOrderCompletion,
            assignmentId: assignment1.Id);

        db.WorkTasks.Add(taskAttributedToTech1);
        db.SaveChanges();

        var exTaskMismatch = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            handler.Handle(new UpdateWorkTaskStatusCommand(
                wo.Id,
                taskAttributedToTech1.Id,
                (short)WorkTaskStatus.Completed), CancellationToken.None));

        Assert.Contains("Technician is not assigned to this task", exTaskMismatch.Message);
    }

    [Fact]
    public async Task WorkTask_UpdateStatus_CompletedWorkOrder_RejectedWithConflict()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(tenantId, wo.Id, 1, "Completed WO task", WorkTaskType.Standard, true, WorkTaskGate.WorkOrderCompletion, assetId: primaryAsset.Id);
        db.WorkTasks.Add(task);

        // Transition WorkOrder to OperationallyComplete
        wo.Complete(userId);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateWorkTaskStatusCommand(
                wo.Id,
                task.Id,
                (short)WorkTaskStatus.Completed), CancellationToken.None));

        Assert.Contains("OperationallyComplete", ex.Message);
    }

    [Fact]
    public async Task WorkTask_UpdateStatus_CancelledWorkOrder_RejectedWithConflict()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var task = new WorkTask(tenantId, wo.Id, 1, "Cancelled WO task", WorkTaskType.Standard, true, WorkTaskGate.WorkOrderCompletion, assetId: primaryAsset.Id);
        db.WorkTasks.Add(task);

        // Cancel WorkOrder
        wo.Cancel("Customer cancelled contract", userId);
        db.SaveChanges();

        var handler = new UpdateWorkTaskStatusCommandHandler(
            new WorkTaskRepository(db),
            new WorkOrderRepository(db),
            new ResourceRepository(db),
            new BookingRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new UpdateWorkTaskStatusCommand(
                wo.Id,
                task.Id,
                (short)WorkTaskStatus.Completed), CancellationToken.None));

        Assert.Contains("Cancelled", ex.Message);
    }

    [Fact]
    public async Task WorkTask_Create_AssetlessWorkOrder_ArbitraryAssetId_RejectedWithConflict()
    {
        // Blocker 2: Work Order has no assets attached, arbitrary AssetId must be rejected
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());

        var account = new Account(tenantId, "ACC-AL-01", "Assetless Corp", "Assetless Legal", AccountType.Commercial, AccountStatus.Active);
        var site = new Site(tenantId, "SIT-AL-01", "Assetless Site", Guid.NewGuid(), "UTC", "100 Void St", "City", "State", "00000", "US");
        var wo = new WorkOrder(tenantId, "WO-AL-001", account.Id, account.Id, site.Id, "GENERAL", "General Inspection", "{}", WorkOrderPriority.Standard);
        wo.Approve(userId);

        db.Accounts.Add(account);
        db.Sites.Add(site);
        db.WorkOrders.Add(wo);
        db.SaveChanges();

        Assert.Empty(wo.Assets);

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var arbitraryAssetId = Guid.NewGuid();
        var command = new CreateWorkTaskCommand(
            wo.Id,
            "Arbitrary Asset Task",
            AssetId: arbitraryAssetId);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("is not associated with Work Order", ex.Message);
    }

    [Fact]
    public async Task WorkTask_Create_WorkOrderWithAttachedAsset_Succeeds()
    {
        // Blocker 2: Attached in-scope asset succeeds
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, primaryAsset, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var command = new CreateWorkTaskCommand(
            wo.Id,
            "Attached Asset Task",
            AssetId: primaryAsset.Id);

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(primaryAsset.Id, result.AssetId);
    }

    [Fact]
    public async Task WorkTask_Create_WorkOrderWithUnattachedAsset_RejectedWithConflict()
    {
        // Blocker 2: Unattached asset in same tenant rejected
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, _, _, _, _, modelId) = SeedFullOperationalChain(db, tenantId, userId);

        var unattachedAsset = new Asset(tenantId, "AST-UNATT-02", modelId, "SN-UNATT", Guid.NewGuid(), Guid.NewGuid(), AssetStatus.Active);
        db.Assets.Add(unattachedAsset);
        db.SaveChanges();

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var command = new CreateWorkTaskCommand(
            wo.Id,
            "Unattached Asset Task",
            AssetId: unattachedAsset.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("is not associated with Work Order", ex.Message);
    }

    [Fact]
    public async Task WorkTask_Create_CrossTenantAssetId_RejectedWithConflict()
    {
        // Blocker 2: Cross-tenant asset rejected
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (woA, _, _, _, _, modelId) = SeedFullOperationalChain(db, tenantA, userId);

        var assetTenantB = new Asset(tenantB, "AST-TENANT-B", modelId, "SN-TENANT-B", Guid.NewGuid(), Guid.NewGuid(), AssetStatus.Active);
        db.Assets.Add(assetTenantB);
        db.SaveChanges();

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantA, userId, Capabilities.WorkOrderCreate));

        var command = new CreateWorkTaskCommand(
            woA.Id,
            "Cross-tenant Asset Task",
            AssetId: assetTenantB.Id);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("is not associated with Work Order", ex.Message);
    }

    [Fact]
    public async Task WorkTask_Create_NullAssetId_Succeeds()
    {
        // Blocker 2: Null AssetId succeeds
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = new ServexaDbContext(CreateInMemoryOptions());
        var (wo, _, _, _, _, _) = SeedFullOperationalChain(db, tenantId, userId);

        var handler = new CreateWorkTaskCommandHandler(
            new WorkOrderRepository(db),
            new WorkTaskRepository(db),
            new UnitOfWork(db),
            new TestTenantContext(tenantId, userId, Capabilities.WorkOrderCreate));

        var command = new CreateWorkTaskCommand(
            wo.Id,
            "General Checklist Task",
            AssetId: null);

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Null(result.AssetId);
    }

    private sealed class TestTenantContext(Guid tenantId, Guid? userId, params string[] capabilities) : ITenantContext
    {
        private readonly HashSet<string> _capabilities = new(capabilities, StringComparer.OrdinalIgnoreCase);

        public Guid TenantId => tenantId;
        public Guid? UserId => userId;
        public string? UserEmail => "tech@servexa.local";
        public string? DisplayName => "Test Tech";
        public IReadOnlyList<string> Roles => ["Technician"];
        public IReadOnlyList<string> Permissions => capabilities.ToList();
        public bool IsAuthenticated => true;
        public bool HasPermission(string capability) => _capabilities.Contains(capability);
    }
}
