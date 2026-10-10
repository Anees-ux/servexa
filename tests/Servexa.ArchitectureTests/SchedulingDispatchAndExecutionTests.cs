using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Customers.Repositories;
using Servexa.Application.Exceptions;
using Servexa.Application.Field.Commands.CompleteTechnicianExecution;
using Servexa.Application.Field.Commands.MarkTechnicianArrived;
using Servexa.Application.Field.Commands.PauseTechnicianWork;
using Servexa.Application.Field.Commands.ResumeTechnicianWork;
using Servexa.Application.Field.Commands.StartTechnicianTravel;
using Servexa.Application.Field.Commands.StartTechnicianWork;
using Servexa.Application.Scheduling.Commands.AssignResourceToBooking;
using Servexa.Application.Scheduling.Commands.CancelBooking;
using Servexa.Application.Scheduling.Commands.CreateBooking;
using Servexa.Application.Scheduling.Commands.DispatchBooking;
using Servexa.Application.Scheduling.Commands.RescheduleBooking;
using Servexa.Application.Scheduling.Commands.UnassignResourceFromBooking;
using Servexa.Application.Scheduling.Dtos;
using Servexa.Application.Scheduling.Repositories;
using Servexa.Application.Service.Repositories;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Field.Entities;
using Servexa.Domain.Field.Enums;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;
using Servexa.Infrastructure.Persistence;
using Servexa.Infrastructure.Persistence.Repositories;

namespace Servexa.ArchitectureTests;

public class SchedulingDispatchAndExecutionTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid SiteA = Guid.NewGuid();
    private static readonly Guid AccountA = Guid.NewGuid();
    private static readonly Guid UserA = Guid.NewGuid();

    private static DbContextOptions<ServexaDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<ServexaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    #region Stage A: Scheduling Domain & Rules

    [Fact]
    public void Booking_DomainInvariants_ShouldEnforceRequiredFieldsAndWindowChronology()
    {
        var workOrderId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(2);
        var end = start.AddHours(2);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-0001",
            workOrderId: workOrderId,
            siteId: SiteA,
            plannedStartUtc: start,
            plannedEndUtc: end,
            siteTimeZoneId: "America/New_York",
            schedulingNotes: "Special access code #1234");

        Assert.Equal(TenantA, booking.TenantId);
        Assert.Equal(workOrderId, booking.WorkOrderId);
        Assert.Equal(SiteA, booking.SiteId);
        Assert.Equal("BKG-0001", booking.BookingNumber);
        Assert.Equal(start, booking.PlannedStartUtc);
        Assert.Equal(end, booking.PlannedEndUtc);
        Assert.Equal(BookingStatus.Scheduled, booking.Status);
        Assert.Equal(BookingDispatchStatus.Unassigned, booking.DispatchStatus);
        Assert.Single(booking.StatusHistory);
        Assert.Equal(BookingStatus.Scheduled, booking.StatusHistory.First().ToStatus);

        // Invalid chronology: End before Start
        Assert.Throws<ArgumentException>(() => new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-0002",
            workOrderId: workOrderId,
            siteId: SiteA,
            plannedStartUtc: end,
            plannedEndUtc: start));

        // Empty Tenant rejection
        Assert.Throws<ArgumentException>(() => new Booking(
            tenantId: Guid.Empty,
            bookingNumber: "BKG-0003",
            workOrderId: workOrderId,
            siteId: SiteA,
            plannedStartUtc: start,
            plannedEndUtc: end));
    }

    [Fact]
    public void CreateBookingValidator_ShouldEnforceChronologyAndRequiredFields()
    {
        var validator = new CreateBookingCommandValidator();

        var validCommand = new CreateBookingCommand(
            WorkOrderId: Guid.NewGuid(),
            PlannedStartUtc: DateTime.UtcNow.AddHours(1),
            PlannedEndUtc: DateTime.UtcNow.AddHours(3),
            SiteTimeZoneId: "UTC",
            SchedulingNotes: "Routine inspection");

        var validResult = validator.TestValidate(validCommand);
        validResult.ShouldNotHaveAnyValidationErrors();

        var invalidChronology = validCommand with
        {
            PlannedEndUtc = validCommand.PlannedStartUtc.AddHours(-1)
        };
        var invalidResult = validator.TestValidate(invalidChronology);
        invalidResult.ShouldHaveValidationErrorFor(c => c.PlannedEndUtc);

        var emptyWorkOrder = validCommand with { WorkOrderId = Guid.Empty };
        var emptyResult = validator.TestValidate(emptyWorkOrder);
        emptyResult.ShouldHaveValidationErrorFor(c => c.WorkOrderId);
    }

    [Fact]
    public async Task CreateBookingHandler_ShouldRejectDraftWorkOrder()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-DRAFT",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.Standard,
            summary: "Draft Work Order pending approval",
            billToSnapshotJson: "{}");

        await db.WorkOrders.AddAsync(workOrder);
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var siteRepo = new SiteRepository(db);
        var accountRepo = new AccountRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var numberSeriesService = new FakeNumberSeriesService();
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA);

        var handler = new CreateBookingCommandHandler(
            bookingRepo,
            workOrderRepo,
            siteRepo,
            accountRepo,
            resourceRepo,
            commitmentRepo,
            numberSeriesService,
            unitOfWork,
            tenantContext);

        var command = new CreateBookingCommand(
            WorkOrderId: workOrder.Id,
            PlannedStartUtc: DateTime.UtcNow.AddDays(1),
            PlannedEndUtc: DateTime.UtcNow.AddDays(1).AddHours(2),
            SiteTimeZoneId: "UTC");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("Approved first", ex.Message);
    }

    [Fact]
    public async Task Booking_Reschedule_ShouldEnforceRevisionLedger()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var originalStart = DateTime.UtcNow.AddDays(1);
        var originalEnd = originalStart.AddHours(3);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-RESCHED",
            workOrderId: Guid.NewGuid(),
            siteId: SiteA,
            plannedStartUtc: originalStart,
            plannedEndUtc: originalEnd);

        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var siteRepo = new SiteRepository(db);
        var accountRepo = new AccountRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA);

        var handler = new RescheduleBookingCommandHandler(
            bookingRepo,
            workOrderRepo,
            siteRepo,
            accountRepo,
            resourceRepo,
            commitmentRepo,
            unitOfWork,
            tenantContext);

        var newStart = originalStart.AddDays(1);
        var newEnd = originalEnd.AddDays(1);

        var command = new RescheduleBookingCommand(
            BookingId: booking.Id,
            NewStartUtc: newStart,
            NewEndUtc: newEnd,
            Reason: "Customer requested morning slot instead",
            Initiator: "Customer");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(newStart, result.PlannedStartUtc);
        Assert.Equal(newEnd, result.PlannedEndUtc);
        Assert.Single(result.Revisions);
        var rev = result.Revisions.First();
        Assert.Equal(originalStart, rev.PreviousStartUtc);
        Assert.Equal(originalEnd, rev.PreviousEndUtc);
        Assert.Equal(newStart, rev.NewStartUtc);
        Assert.Equal(newEnd, rev.NewEndUtc);
        Assert.Equal("Customer requested morning slot instead", rev.Reason);
        Assert.Equal("Customer", rev.Initiator);
    }

    [Fact]
    public async Task Booking_Cancel_ShouldRequireReasonAndReleaseCommitments()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-CANCEL-TEST",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "PREVENTIVE",
            priority: WorkOrderPriority.Low,
            summary: "Annual check",
            billToSnapshotJson: "{}");
        workOrder.Approve(UserA);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-CANCEL",
            workOrderId: workOrder.Id,
            siteId: SiteA,
            plannedStartUtc: DateTime.UtcNow.AddDays(2),
            plannedEndUtc: DateTime.UtcNow.AddDays(2).AddHours(2));

        var resource = new Resource(
            tenantId: TenantA,
            resourceCode: "TECH-01",
            displayName: "John Field Tech",
            resourceType: ResourceType.Technician,
            userId: UserA);

        await db.WorkOrders.AddAsync(workOrder);
        await db.Resources.AddAsync(resource);
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var assignment = new ResourceAssignment(
            tenantId: TenantA,
            bookingId: booking.Id,
            resourceId: resource.Id,
            plannedStartUtc: booking.PlannedStartUtc,
            plannedEndUtc: booking.PlannedEndUtc,
            assignmentRole: AssignmentRole.Lead,
            selectionRationale: "Assigned for cancellation test");
        booking.AddAssignment(assignment);

        var commitment = new ResourceCommitment(
            tenantId: TenantA,
            resourceId: resource.Id,
            resourceAssignmentId: assignment.Id,
            startUtc: booking.PlannedStartUtc,
            endUtc: booking.PlannedEndUtc);

        await db.ResourceCommitments.AddAsync(commitment);
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var siteRepo = new SiteRepository(db);
        var accountRepo = new AccountRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA);

        var handler = new CancelBookingCommandHandler(
            bookingRepo,
            workOrderRepo,
            siteRepo,
            accountRepo,
            resourceRepo,
            commitmentRepo,
            unitOfWork,
            tenantContext);

        var command = new CancelBookingCommand(booking.Id, "Site unreachable due to flooding");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled.ToString(), result.Status);
        Assert.Equal(BookingDispatchStatus.Cancelled.ToString(), result.DispatchStatus);

        // Commitments released
        var remainingCommitments = await db.ResourceCommitments
            .Where(c => c.ResourceAssignmentId == assignment.Id && c.ReleasedAtUtc == null)
            .ToListAsync();
        Assert.Empty(remainingCommitments);
    }

    #endregion

    #region Stage B: Dispatch & Resource Assignment

    [Fact]
    public async Task AssignResource_ShouldEnforceConflictGuardsAndPreventDoubleBooking()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var resource = new Resource(
            tenantId: TenantA,
            resourceCode: "TECH-ELITE",
            displayName: "Sarah Specialist",
            resourceType: ResourceType.Technician,
            userId: UserA);
        await db.Resources.AddAsync(resource);

        var startSlot = DateTime.UtcNow.AddDays(1).AddHours(9);
        var endSlot = startSlot.AddHours(2);

        // Booking 1
        var booking1 = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-SLOT-1",
            workOrderId: Guid.NewGuid(),
            siteId: SiteA,
            plannedStartUtc: startSlot,
            plannedEndUtc: endSlot);
        await db.Bookings.AddAsync(booking1);

        // Booking 2 overlapping (9:30 - 11:30)
        var booking2 = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-SLOT-2",
            workOrderId: Guid.NewGuid(),
            siteId: SiteA,
            plannedStartUtc: startSlot.AddMinutes(30),
            plannedEndUtc: endSlot.AddMinutes(30));
        await db.Bookings.AddAsync(booking2);

        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var siteRepo = new SiteRepository(db);
        var accountRepo = new AccountRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA);

        var handler = new AssignResourceToBookingCommandHandler(
            bookingRepo,
            resourceRepo,
            commitmentRepo,
            workOrderRepo,
            siteRepo,
            accountRepo,
            unitOfWork,
            tenantContext);

        // Assign Booking 1 -> Succeeds
        var assignCmd1 = new AssignResourceToBookingCommand(
            BookingId: booking1.Id,
            ResourceId: resource.Id,
            AssignmentRole: AssignmentRole.Lead,
            SelectionRationale: "Primary dispatch");

        var res1 = await handler.Handle(assignCmd1, CancellationToken.None);
        Assert.NotNull(res1);
        Assert.Single(res1.Assignments);
        Assert.Equal(BookingDispatchStatus.Assigned.ToString(), res1.DispatchStatus);

        // Assign Booking 2 -> Must fail with conflict guard
        var assignCmd2 = new AssignResourceToBookingCommand(
            BookingId: booking2.Id,
            ResourceId: resource.Id,
            AssignmentRole: AssignmentRole.Lead,
            SelectionRationale: "Double booking attempt");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(assignCmd2, CancellationToken.None));
        Assert.Contains("overlapping commitment", ex.Message);
    }

    [Fact]
    public async Task DispatchBooking_ShouldEnforceAssignedResourcePrerequisite()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-DISP-PRE",
            workOrderId: Guid.NewGuid(),
            siteId: SiteA,
            plannedStartUtc: DateTime.UtcNow.AddDays(1),
            plannedEndUtc: DateTime.UtcNow.AddDays(1).AddHours(2));
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var siteRepo = new SiteRepository(db);
        var accountRepo = new AccountRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA);

        var handler = new DispatchBookingCommandHandler(
            bookingRepo,
            resourceRepo,
            workOrderRepo,
            siteRepo,
            accountRepo,
            unitOfWork,
            tenantContext);

        // Cannot dispatch booking with no assigned resource
        var command = new DispatchBookingCommand(booking.Id);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Contains("no active assigned resources", ex.Message);

        // Now assign resource and dispatch successfully
        var resource = new Resource(
            tenantId: TenantA,
            resourceCode: "TECH-02",
            displayName: "Mike Dispatch",
            resourceType: ResourceType.Technician);
        await db.Resources.AddAsync(resource);

        var assignment = new ResourceAssignment(
            tenantId: TenantA,
            bookingId: booking.Id,
            resourceId: resource.Id,
            plannedStartUtc: booking.PlannedStartUtc,
            plannedEndUtc: booking.PlannedEndUtc,
            assignmentRole: AssignmentRole.Lead);
        booking.AddAssignment(assignment);
        await db.SaveChangesAsync();

        var dispatched = await handler.Handle(command, CancellationToken.None);
        Assert.Equal(BookingDispatchStatus.Dispatched.ToString(), dispatched.DispatchStatus);
    }

    #endregion

    #region Stage C: Technician Execution & Operational Completion Gates

    [Fact]
    public async Task TechnicianExecution_ShouldFollowTravelArriveStartPauseResumeComplete()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-EXEC-TEST",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "Compressor trip",
            billToSnapshotJson: "{}");
        workOrder.Approve(UserA);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-EXEC-01",
            workOrderId: workOrder.Id,
            siteId: SiteA,
            plannedStartUtc: DateTime.UtcNow.AddHours(1),
            plannedEndUtc: DateTime.UtcNow.AddHours(3));

        var resource = new Resource(
            tenantId: TenantA,
            resourceCode: "TECH-EXEC",
            displayName: "Field Tech 01",
            resourceType: ResourceType.Technician,
            userId: UserA);

        await db.WorkOrders.AddAsync(workOrder);
        await db.Resources.AddAsync(resource);
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var assignment = new ResourceAssignment(
            tenantId: TenantA,
            bookingId: booking.Id,
            resourceId: resource.Id,
            plannedStartUtc: booking.PlannedStartUtc,
            plannedEndUtc: booking.PlannedEndUtc,
            assignmentRole: AssignmentRole.Lead);
        booking.AddAssignment(assignment);
        booking.Dispatch();
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var sessionRepo = new ExecutionSessionRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA, userId: UserA);

        // 1. Start Travel
        var travelHandler = new StartTechnicianTravelCommandHandler(
            resourceRepo, bookingRepo, sessionRepo, unitOfWork, tenantContext);
        var travelResult = await travelHandler.Handle(
            new StartTechnicianTravelCommand(assignment.Id), CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Traveling.ToString(), travelResult.Status);
        Assert.Single(travelResult.Intervals);
        Assert.Equal(ExecutionIntervalType.Travel.ToString(), travelResult.Intervals.First().IntervalType);

        // 2. Mark Arrived
        var arriveHandler = new MarkTechnicianArrivedCommandHandler(
            resourceRepo, bookingRepo, sessionRepo, unitOfWork, tenantContext);
        var arriveResult = await arriveHandler.Handle(
            new MarkTechnicianArrivedCommand(assignment.Id), CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Arrived.ToString(), arriveResult.Status);
        Assert.Equal(ExecutionIntervalType.Travel.ToString(), arriveResult.Intervals.First().IntervalType);
        Assert.NotNull(arriveResult.Intervals.First().EndedAtUtc);

        // 3. Start Work
        var startWorkHandler = new StartTechnicianWorkCommandHandler(
            resourceRepo, bookingRepo, sessionRepo, workOrderRepo, unitOfWork, tenantContext);
        var workResult = await startWorkHandler.Handle(
            new StartTechnicianWorkCommand(assignment.Id), CancellationToken.None);

        Assert.Equal(3, workResult.Intervals.Count);
        var workInterval = workResult.Intervals.Last();
        Assert.Equal(ExecutionIntervalType.Work.ToString(), workInterval.IntervalType);
        Assert.Null(workInterval.EndedAtUtc);

        // Work Order is now InProgress
        var refreshedWo = await workOrderRepo.GetByIdAsync(TenantA, workOrder.Id);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, refreshedWo!.OperationalStatus);

        // 4. Pause Work
        var pauseHandler = new PauseTechnicianWorkCommandHandler(
            resourceRepo, sessionRepo, unitOfWork, tenantContext);
        var pauseResult = await pauseHandler.Handle(
            new PauseTechnicianWorkCommand(assignment.Id, "Waiting for replacement valve"), CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Paused.ToString(), pauseResult.Status);
        Assert.Equal(4, pauseResult.Intervals.Count);
        var pauseInterval = pauseResult.Intervals.Last();
        Assert.Equal(ExecutionIntervalType.Pause.ToString(), pauseInterval.IntervalType);
        Assert.Equal("Waiting for replacement valve", pauseInterval.Notes);

        // 5. Resume Work
        var resumeHandler = new ResumeTechnicianWorkCommandHandler(
            resourceRepo, sessionRepo, unitOfWork, tenantContext);
        var resumeResult = await resumeHandler.Handle(
            new ResumeTechnicianWorkCommand(assignment.Id), CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Working.ToString(), resumeResult.Status);
        Assert.Equal(5, resumeResult.Intervals.Count);
        Assert.Equal(ExecutionIntervalType.Work.ToString(), resumeResult.Intervals.Last().IntervalType);

        // 6. Complete Execution
        var completeHandler = new CompleteTechnicianExecutionCommandHandler(
            resourceRepo, bookingRepo, sessionRepo, commitmentRepo, unitOfWork, tenantContext);
        var completeResult = await completeHandler.Handle(
            new CompleteTechnicianExecutionCommand(assignment.Id, "Replaced thermal expansion valve, pressure verified normal"),
            CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Ended.ToString(), completeResult.Status);
        Assert.NotNull(completeResult.CompletedAtUtc);
        Assert.Equal("Replaced thermal expansion valve, pressure verified normal", completeResult.WorkSummary);

        // Booking is Completed
        var refreshedBooking = await bookingRepo.GetByIdAsync(TenantA, booking.Id);
        Assert.Equal(BookingStatus.Completed, refreshedBooking!.Status);

        // B-1: Work Order must NOT be automatically completed by technician execution.
        // It remains InProgress awaiting formal WorkOrderCompletionEvaluation.
        refreshedWo = await workOrderRepo.GetByIdAsync(TenantA, workOrder.Id);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, refreshedWo!.OperationalStatus);
        Assert.NotEqual(WorkOrderOperationalStatus.OperationallyComplete, refreshedWo.OperationalStatus);
    }

    [Fact]
    public async Task B1_Regression_TechnicianCompletion_CannotAutomaticallyCompleteWorkOrder()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-REGRESS-B1",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "CORRECTIVE",
            priority: WorkOrderPriority.High,
            summary: "HVAC motor repair",
            billToSnapshotJson: "{}");
        workOrder.Approve(UserA);
        workOrder.StartProgress(UserA);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-B1-01",
            workOrderId: workOrder.Id,
            siteId: SiteA,
            plannedStartUtc: DateTime.UtcNow.AddHours(1),
            plannedEndUtc: DateTime.UtcNow.AddHours(3));

        var resource = new Resource(
            tenantId: TenantA,
            resourceCode: "TECH-B1",
            displayName: "Field Tech B1",
            resourceType: ResourceType.Technician,
            userId: UserA);

        await db.WorkOrders.AddAsync(workOrder);
        await db.Resources.AddAsync(resource);
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var assignment = new ResourceAssignment(
            tenantId: TenantA,
            bookingId: booking.Id,
            resourceId: resource.Id,
            plannedStartUtc: booking.PlannedStartUtc,
            plannedEndUtc: booking.PlannedEndUtc,
            assignmentRole: AssignmentRole.Lead);
        booking.AddAssignment(assignment);
        booking.Dispatch();

        var session = new ExecutionSession(TenantA, assignment.Id, booking.Id, booking.WorkOrderId, resource.Id, UserA);
        session.StartWork();
        await db.ExecutionSessions.AddAsync(session);
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var sessionRepo = new ExecutionSessionRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA, userId: UserA);

        var completeHandler = new CompleteTechnicianExecutionCommandHandler(
            resourceRepo, bookingRepo, sessionRepo, commitmentRepo, unitOfWork, tenantContext);

        var completeResult = await completeHandler.Handle(
            new CompleteTechnicianExecutionCommand(assignment.Id, "Finished technical diagnosis and replaced capacitor"),
            CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Ended.ToString(), completeResult.Status);

        // Booking is marked Completed
        var refreshedBooking = await bookingRepo.GetByIdAsync(TenantA, booking.Id);
        Assert.Equal(BookingStatus.Completed, refreshedBooking!.Status);

        // Invariant: Work Order MUST NOT be completed by technician execution!
        // It must remain in InProgress awaiting approved WorkOrderCompletionEvaluation.
        var refreshedWo = await workOrderRepo.GetByIdAsync(TenantA, workOrder.Id);
        Assert.NotNull(refreshedWo);
        Assert.Equal(WorkOrderOperationalStatus.InProgress, refreshedWo.OperationalStatus);
        Assert.NotEqual(WorkOrderOperationalStatus.OperationallyComplete, refreshedWo.OperationalStatus);
    }

    [Fact]
    public async Task B1_Regression_TechnicianCompletion_WhenWorkOrderIsScheduled_SucceedsWithoutChangingWorkOrderStatus()
    {
        var options = CreateInMemoryOptions();
        await using var db = new ServexaDbContext(options);

        var workOrder = new WorkOrder(
            tenantId: TenantA,
            workOrderNumber: "WO-REGRESS-SCHED",
            serviceAccountId: AccountA,
            billToAccountId: AccountA,
            primarySiteId: SiteA,
            workTypeCode: "PREVENTIVE",
            priority: WorkOrderPriority.Standard,
            summary: "Filter change",
            billToSnapshotJson: "{}");
        workOrder.Approve(UserA);
        workOrder.MarkScheduled(UserA);

        var booking = new Booking(
            tenantId: TenantA,
            bookingNumber: "BKG-SCHED-01",
            workOrderId: workOrder.Id,
            siteId: SiteA,
            plannedStartUtc: DateTime.UtcNow.AddHours(2),
            plannedEndUtc: DateTime.UtcNow.AddHours(4));

        var resource = new Resource(
            tenantId: TenantA,
            resourceCode: "TECH-SCHED",
            displayName: "Tech Scheduled",
            resourceType: ResourceType.Technician,
            userId: UserA);

        await db.WorkOrders.AddAsync(workOrder);
        await db.Resources.AddAsync(resource);
        await db.Bookings.AddAsync(booking);
        await db.SaveChangesAsync();

        var assignment = new ResourceAssignment(
            tenantId: TenantA,
            bookingId: booking.Id,
            resourceId: resource.Id,
            plannedStartUtc: booking.PlannedStartUtc,
            plannedEndUtc: booking.PlannedEndUtc,
            assignmentRole: AssignmentRole.Lead);
        booking.AddAssignment(assignment);
        booking.Dispatch();

        var session = new ExecutionSession(TenantA, assignment.Id, booking.Id, booking.WorkOrderId, resource.Id, UserA);
        session.StartWork();
        await db.ExecutionSessions.AddAsync(session);
        await db.SaveChangesAsync();

        var bookingRepo = new BookingRepository(db);
        var workOrderRepo = new WorkOrderRepository(db);
        var resourceRepo = new ResourceRepository(db);
        var sessionRepo = new ExecutionSessionRepository(db);
        var commitmentRepo = new ResourceCommitmentRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var tenantContext = new FakeTenantContext(TenantA, userId: UserA);

        var completeHandler = new CompleteTechnicianExecutionCommandHandler(
            resourceRepo, bookingRepo, sessionRepo, commitmentRepo, unitOfWork, tenantContext);

        var completeResult = await completeHandler.Handle(
            new CompleteTechnicianExecutionCommand(assignment.Id, "Completed visit"),
            CancellationToken.None);

        Assert.Equal(ExecutionSessionStatus.Ended.ToString(), completeResult.Status);

        // Work Order remains Scheduled and does not fail
        var refreshedWo = await workOrderRepo.GetByIdAsync(TenantA, workOrder.Id);
        Assert.NotNull(refreshedWo);
        Assert.Equal(WorkOrderOperationalStatus.Scheduled, refreshedWo.OperationalStatus);
    }

    #endregion

    #region Supporting Test Fakes

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

    private sealed class FakeNumberSeriesService : INumberSeriesService
    {
        private int _counter;

        public Task<string> AllocateNextNumberAsync(
            Guid tenantId,
            string seriesKey,
            string defaultPrefix,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"{defaultPrefix}{Interlocked.Increment(ref _counter):D4}");
        }
    }

    #endregion
}
