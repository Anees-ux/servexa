using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Servexa.Api.Infrastructure;
using Servexa.Application.Common.Interfaces;
using Servexa.Application.Exceptions;
using Servexa.Application.Scheduling.Commands.AssignResourceToBooking;
using Servexa.Application.Scheduling.Commands.RescheduleBooking;
using Servexa.Domain.Customers.Entities;
using Servexa.Domain.Customers.Enums;
using Servexa.Domain.Platform.Entities;
using Servexa.Domain.Scheduling.Entities;
using Servexa.Domain.Scheduling.Enums;
using Servexa.Domain.Service.Entities;
using Servexa.Domain.Service.Enums;
using Servexa.Infrastructure.Persistence;
using Servexa.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Servexa.ArchitectureTests;

/// <summary>
/// Proves real SQL Server concurrency, serialization, and isolation behaviors (H3 & Physical Model §12.8).
/// Uses genuinely competing DbContext instances against real SQL Server (LocalDB) to demonstrate:
/// 1. At most one conflicting assignment commits between competing concurrent transactions.
/// 2. The losing transaction leaves zero partial assignments or commitments.
/// 3. Resource schedule guard rowversion changes upon commit.
/// 4. Concurrent first-time guard creation safely resolves without duplicate guard records.
/// 5. Overlapping reschedule conflicts are rejected.
/// 6. Non-overlapping bookings remain concurrently assignable.
/// 7. Tenant isolation is strictly preserved across concurrent operations.
/// 8. Concurrency exceptions map cleanly to HTTP 409 Conflict ProblemDetails.
/// </summary>
public class RealSqlServerConcurrencyIntegrationTests
{
    private const string SqlServerConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=Servexa_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    private static DbContextOptions<ServexaDbContext> CreateSqlServerOptions()
    {
        return new DbContextOptionsBuilder<ServexaDbContext>()
            .UseSqlServer(SqlServerConnectionString)
            .Options;
    }

    private sealed class TestTenantContext(Guid tenantId, Guid userId) : ITenantContext
    {
        public Guid TenantId => tenantId;
        public Guid? UserId => userId;
        public string? UserEmail => "concurrency.test@servexa.local";
        public string? DisplayName => "Concurrency Tester";
        public IReadOnlyList<string> Roles => ["Dispatcher", "Admin"];
        public IReadOnlyList<string> Permissions => ["Scheduling.Manage", "Dispatch.Manage"];
        public bool IsAuthenticated => true;
        public bool HasPermission(string permission) => true;
    }

    private static async Task<(Guid AccountId, Guid SiteId, Guid WorkOrderId, Guid ResourceId)> SeedBaseEntitiesAsync(
        ServexaDbContext db,
        Guid tenantId,
        Guid userId)
    {
        var tenant = new Tenant(
            tenantCode: $"T-{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            legalName: "Concurrency Test Legal",
            displayName: "Concurrency Tenant",
            defaultTimeZoneId: "Europe/Helsinki",
            defaultCurrencyCode: "EUR",
            id: tenantId);
        db.Tenants.Add(tenant);

        var branch = new Branch(
            tenantId: tenantId,
            code: $"BR-{Guid.NewGuid():N}"[..6].ToUpperInvariant(),
            name: "Helsinki HQ",
            timeZoneId: "Europe/Helsinki");
        db.Branches.Add(branch);

        var account = new Account(
            tenantId: tenantId,
            accountNumber: $"ACC-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            legalName: "Concurrency Legal Corp",
            displayName: "Concurrency Account",
            accountType: AccountType.Commercial,
            currencyCode: "EUR");
        db.Accounts.Add(account);

        var site = new Site(
            tenantId: tenantId,
            siteNumber: $"SIT-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            name: "Concurrency Test Site",
            branchId: branch.Id,
            timeZoneId: "Europe/Helsinki",
            addressLine1: "Test St 1",
            city: "Helsinki",
            stateProvince: "Uusimaa",
            postalCode: "00100",
            countryCode: "FI");
        db.Sites.Add(site);

        var wo = new WorkOrder(
            tenantId: tenantId,
            workOrderNumber: $"WO-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            serviceAccountId: account.Id,
            billToAccountId: account.Id,
            primarySiteId: site.Id,
            workTypeCode: "PREVENTIVE",
            priority: WorkOrderPriority.Standard,
            summary: "Concurrency Test WO",
            billToSnapshotJson: "{\"Account\":\"Concurrency Legal\"}");
        wo.Approve(userId);
        db.WorkOrders.Add(wo);

        var resource = new Resource(
            tenantId: tenantId,
            resourceCode: $"TECH-{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            resourceType: ResourceType.Technician,
            displayName: "Concurrency Technician",
            homeBranchId: branch.Id,
            exclusiveCapacity: true,
            status: ResourceStatus.Active);
        db.Resources.Add(resource);

        await db.SaveChangesAsync();

        return (account.Id, site.Id, wo.Id, resource.Id);
    }

    [Fact]
    public async Task TwoSimultaneousAssignments_ForSameResourceAndOverlappingWindow_AtMostOneCommits()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Guid woId, siteId, resourceId;
        using (var setupDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            (_, siteId, woId, resourceId) = await SeedBaseEntitiesAsync(setupDb, tenantId, userId);

            // Create two distinct bookings with overlapping windows
            var start = DateTime.UtcNow.AddHours(2);
            var end = start.AddHours(2);

            var booking1 = new Booking(tenantId, $"BKG-C1-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start, end, "Europe/Helsinki", BookingStatus.Scheduled, 1, null, userId);
            var booking2 = new Booking(tenantId, $"BKG-C2-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start.AddMinutes(30), end.AddMinutes(30), "Europe/Helsinki", BookingStatus.Scheduled, 1, null, userId);

            setupDb.Bookings.AddRange(booking1, booking2);
            await setupDb.SaveChangesAsync();
        }

        Guid bkg1Id, bkg2Id;
        using (var readDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            var b1 = await readDb.Bookings.FirstAsync(b => b.TenantId == tenantId && b.BookingNumber.StartsWith("BKG-C1"));
            var b2 = await readDb.Bookings.FirstAsync(b => b.TenantId == tenantId && b.BookingNumber.StartsWith("BKG-C2"));
            bkg1Id = b1.Id;
            bkg2Id = b2.Id;
        }

        // Run concurrent assignment using two distinct DbContexts
        using var db1 = new ServexaDbContext(CreateSqlServerOptions());
        using var db2 = new ServexaDbContext(CreateSqlServerOptions());

        var tenantContext1 = new TestTenantContext(tenantId, userId);
        var tenantContext2 = new TestTenantContext(tenantId, userId);

        var handler1 = new AssignResourceToBookingCommandHandler(
            new BookingRepository(db1),
            new ResourceRepository(db1),
            new ResourceCommitmentRepository(db1),
            new WorkOrderRepository(db1),
            new SiteRepository(db1),
            new AccountRepository(db1),
            new UnitOfWork(db1),
            tenantContext1);

        var handler2 = new AssignResourceToBookingCommandHandler(
            new BookingRepository(db2),
            new ResourceRepository(db2),
            new ResourceCommitmentRepository(db2),
            new WorkOrderRepository(db2),
            new SiteRepository(db2),
            new AccountRepository(db2),
            new UnitOfWork(db2),
            tenantContext2);

        var command1 = new AssignResourceToBookingCommand(bkg1Id, resourceId, AssignmentRole.Lead, "Tx1 assignment");
        var command2 = new AssignResourceToBookingCommand(bkg2Id, resourceId, AssignmentRole.Lead, "Tx2 assignment");

        int successCount = 0;
        int conflictCount = 0;

        var task1 = Task.Run(async () =>
        {
            try
            {
                await handler1.Handle(command1, CancellationToken.None);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex) when (ex is ConflictException || ex is DbUpdateConcurrencyException || ex is DbUpdateException)
            {
                Interlocked.Increment(ref conflictCount);
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                await handler2.Handle(command2, CancellationToken.None);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex) when (ex is ConflictException || ex is DbUpdateConcurrencyException || ex is DbUpdateException)
            {
                Interlocked.Increment(ref conflictCount);
            }
        });

        await Task.WhenAll(task1, task2);

        // Crucial invariant: Exactly one commits, exactly one is rejected
        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);

        // Verification in fresh DbContext: exactly one assignment and one commitment exist in SQL Server
        using var verifyDb = new ServexaDbContext(CreateSqlServerOptions());
        var persistedAssignments = await verifyDb.ResourceAssignments
            .Where(a => a.TenantId == tenantId && a.ResourceId == resourceId && a.Status == AssignmentStatus.Assigned)
            .ToListAsync();
        var persistedCommitments = await verifyDb.ResourceCommitments
            .Where(c => c.TenantId == tenantId && c.ResourceId == resourceId && c.Status == CommitmentStatus.Active)
            .ToListAsync();

        Assert.Single(persistedAssignments);
        Assert.Single(persistedCommitments);

        // The single committed commitment matches the successful booking interval
        Assert.Equal(persistedAssignments[0].Id, persistedCommitments[0].ResourceAssignmentId);
    }

    [Fact]
    public async Task ResourceScheduleGuard_RowVersion_ChangesUponSuccessfulCommit()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Guid woId, siteId, resourceId, bookingId;
        using (var setupDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            (_, siteId, woId, resourceId) = await SeedBaseEntitiesAsync(setupDb, tenantId, userId);

            var start = DateTime.UtcNow.AddHours(4);
            var end = start.AddHours(2);
            var booking = new Booking(tenantId, $"BKG-RV-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start, end, "Europe/Helsinki", BookingStatus.Scheduled, 1, null, userId);
            setupDb.Bookings.Add(booking);

            // Pre-seed a guard
            var guard = new ResourceScheduleGuard(tenantId, resourceId);
            setupDb.ResourceScheduleGuards.Add(guard);
            await setupDb.SaveChangesAsync();
            bookingId = booking.Id;
        }

        byte[] initialRowVersion;
        using (var readDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            var g = await readDb.ResourceScheduleGuards.FirstAsync(x => x.TenantId == tenantId && x.ResourceId == resourceId);
            initialRowVersion = (byte[])g.RowVersion.Clone();
        }

        // Assign resource to booking
        using var assignDb = new ServexaDbContext(CreateSqlServerOptions());
        var tenantContext = new TestTenantContext(tenantId, userId);
        var handler = new AssignResourceToBookingCommandHandler(
            new BookingRepository(assignDb),
            new ResourceRepository(assignDb),
            new ResourceCommitmentRepository(assignDb),
            new WorkOrderRepository(assignDb),
            new SiteRepository(assignDb),
            new AccountRepository(assignDb),
            new UnitOfWork(assignDb),
            tenantContext);

        await handler.Handle(new AssignResourceToBookingCommand(bookingId, resourceId, AssignmentRole.Lead), CancellationToken.None);

        // Verify guard rowversion changed in database
        using var verifyDb = new ServexaDbContext(CreateSqlServerOptions());
        var updatedGuard = await verifyDb.ResourceScheduleGuards.FirstAsync(x => x.TenantId == tenantId && x.ResourceId == resourceId);
        Assert.False(initialRowVersion.SequenceEqual(updatedGuard.RowVersion), "RowVersion must change after commit on SQL Server.");
    }

    [Fact]
    public async Task ConcurrentFirstTimeGuardCreation_DoesNotCorruptDatabase()
    {
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        // Two distinct DbContexts call EnsureScheduleGuardAsync simultaneously for a brand new resource
        using var db1 = new ServexaDbContext(CreateSqlServerOptions());
        using var db2 = new ServexaDbContext(CreateSqlServerOptions());

        var repo1 = new ResourceCommitmentRepository(db1);
        var repo2 = new ResourceCommitmentRepository(db2);

        var task1 = Task.Run(async () =>
        {
            try
            {
                await repo1.EnsureScheduleGuardAsync(tenantId, resourceId);
                await db1.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is DbUpdateException)
            {
                // Unique constraint expected if both try to insert
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                await repo2.EnsureScheduleGuardAsync(tenantId, resourceId);
                await db2.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is DbUpdateException)
            {
                // Unique constraint expected if both try to insert
            }
        });

        await Task.WhenAll(task1, task2);

        // Verify database state: exactly 1 guard row was created
        using var verifyDb = new ServexaDbContext(CreateSqlServerOptions());
        var guards = await verifyDb.ResourceScheduleGuards
            .Where(g => g.TenantId == tenantId && g.ResourceId == resourceId)
            .ToListAsync();

        Assert.Single(guards);
    }

    [Fact]
    public async Task NonOverlappingBookings_RemainAssignable()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Guid woId, siteId, resourceId;
        Guid bkg1Id, bkg2Id;
        using (var setupDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            (_, siteId, woId, resourceId) = await SeedBaseEntitiesAsync(setupDb, tenantId, userId);

            // Morning booking: 09:00 - 11:00
            var start1 = DateTime.UtcNow.Date.AddDays(2).AddHours(9);
            var end1 = start1.AddHours(2);
            var booking1 = new Booking(tenantId, $"BKG-NO1-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start1, end1, "Europe/Helsinki", BookingStatus.Scheduled, 1, null, userId);

            // Afternoon booking: 13:00 - 15:00 (non-overlapping)
            var start2 = DateTime.UtcNow.Date.AddDays(2).AddHours(13);
            var end2 = start2.AddHours(2);
            var booking2 = new Booking(tenantId, $"BKG-NO2-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start2, end2, "Europe/Helsinki", BookingStatus.Scheduled, 1, null, userId);

            setupDb.Bookings.AddRange(booking1, booking2);
            await setupDb.SaveChangesAsync();

            bkg1Id = booking1.Id;
            bkg2Id = booking2.Id;
        }

        // Assign first booking with db1
        using (var db1 = new ServexaDbContext(CreateSqlServerOptions()))
        {
            var handler1 = new AssignResourceToBookingCommandHandler(
                new BookingRepository(db1), new ResourceRepository(db1), new ResourceCommitmentRepository(db1),
                new WorkOrderRepository(db1), new SiteRepository(db1), new AccountRepository(db1),
                new UnitOfWork(db1), new TestTenantContext(tenantId, userId));
            await handler1.Handle(new AssignResourceToBookingCommand(bkg1Id, resourceId, AssignmentRole.Lead), CancellationToken.None);
        }

        // Assign second non-overlapping booking with db2
        using (var db2 = new ServexaDbContext(CreateSqlServerOptions()))
        {
            var handler2 = new AssignResourceToBookingCommandHandler(
                new BookingRepository(db2), new ResourceRepository(db2), new ResourceCommitmentRepository(db2),
                new WorkOrderRepository(db2), new SiteRepository(db2), new AccountRepository(db2),
                new UnitOfWork(db2), new TestTenantContext(tenantId, userId));
            await handler2.Handle(new AssignResourceToBookingCommand(bkg2Id, resourceId, AssignmentRole.Support), CancellationToken.None);
        }

        // Both succeeded
        using var verifyDb = new ServexaDbContext(CreateSqlServerOptions());
        var commitments = await verifyDb.ResourceCommitments
            .Where(c => c.TenantId == tenantId && c.ResourceId == resourceId && c.Status == CommitmentStatus.Active)
            .ToListAsync();

        Assert.Equal(2, commitments.Count);
    }

    [Fact]
    public async Task TenantIsolation_ConcurrentAssignmentsInDifferentTenants_DoNotConflict()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Guid woA, siteA, resA, bkgAId;
        Guid woB, siteB, resB, bkgBId;

        using (var setupDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            (_, siteA, woA, resA) = await SeedBaseEntitiesAsync(setupDb, tenantA, userId);
            (_, siteB, woB, resB) = await SeedBaseEntitiesAsync(setupDb, tenantB, userId);

            var start = DateTime.UtcNow.AddHours(5);
            var end = start.AddHours(2);

            var bookingA = new Booking(tenantA, $"BKG-TA-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woA, siteA, start, end, "UTC", BookingStatus.Scheduled, 1, null, userId);
            var bookingB = new Booking(tenantB, $"BKG-TB-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woB, siteB, start, end, "UTC", BookingStatus.Scheduled, 1, null, userId);

            setupDb.Bookings.AddRange(bookingA, bookingB);
            await setupDb.SaveChangesAsync();

            bkgAId = bookingA.Id;
            bkgBId = bookingB.Id;
        }

        using var dbA = new ServexaDbContext(CreateSqlServerOptions());
        using var dbB = new ServexaDbContext(CreateSqlServerOptions());

        var handlerA = new AssignResourceToBookingCommandHandler(
            new BookingRepository(dbA), new ResourceRepository(dbA), new ResourceCommitmentRepository(dbA),
            new WorkOrderRepository(dbA), new SiteRepository(dbA), new AccountRepository(dbA),
            new UnitOfWork(dbA), new TestTenantContext(tenantA, userId));

        var handlerB = new AssignResourceToBookingCommandHandler(
            new BookingRepository(dbB), new ResourceRepository(dbB), new ResourceCommitmentRepository(dbB),
            new WorkOrderRepository(dbB), new SiteRepository(dbB), new AccountRepository(dbB),
            new UnitOfWork(dbB), new TestTenantContext(tenantB, userId));

        // Both run at the exact same time for the exact same wall-clock window in different tenants
        var taskA = handlerA.Handle(new AssignResourceToBookingCommand(bkgAId, resA, AssignmentRole.Lead), CancellationToken.None);
        var taskB = handlerB.Handle(new AssignResourceToBookingCommand(bkgBId, resB, AssignmentRole.Lead), CancellationToken.None);

        await Task.WhenAll(taskA, taskB);

        using var verifyDb = new ServexaDbContext(CreateSqlServerOptions());
        var countA = await verifyDb.ResourceCommitments.CountAsync(c => c.TenantId == tenantA && c.Status == CommitmentStatus.Active);
        var countB = await verifyDb.ResourceCommitments.CountAsync(c => c.TenantId == tenantB && c.Status == CommitmentStatus.Active);

        Assert.Equal(1, countA);
        Assert.Equal(1, countB);
    }

    [Fact]
    public async Task OverlappingReschedule_ConflictIsRejected()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        Guid woId, siteId, resourceId, bkg1Id, bkg2Id;
        using (var setupDb = new ServexaDbContext(CreateSqlServerOptions()))
        {
            (_, siteId, woId, resourceId) = await SeedBaseEntitiesAsync(setupDb, tenantId, userId);

            // Booking 1: 09:00 - 11:00
            var start1 = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
            var end1 = start1.AddHours(2);
            var booking1 = new Booking(tenantId, $"BKG-RSC1-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start1, end1, "UTC", BookingStatus.Scheduled, 1, null, userId);

            // Booking 2: 14:00 - 16:00
            var start2 = DateTime.UtcNow.Date.AddDays(3).AddHours(14);
            var end2 = start2.AddHours(2);
            var booking2 = new Booking(tenantId, $"BKG-RSC2-{Guid.NewGuid():N}"[..10].ToUpperInvariant(), woId, siteId, start2, end2, "UTC", BookingStatus.Scheduled, 1, null, userId);

            setupDb.Bookings.AddRange(booking1, booking2);
            await setupDb.SaveChangesAsync();

            bkg1Id = booking1.Id;
            bkg2Id = booking2.Id;

            // Assign resource to booking 1
            var assign1 = new ResourceAssignment(tenantId, booking1.Id, resourceId, start1, end1, AssignmentRole.Lead);
            booking1.AddAssignment(assign1);
            var commit1 = new ResourceCommitment(tenantId, resourceId, assign1.Id, start1, end1, CommitmentKind.Direct);
            setupDb.ResourceCommitments.Add(commit1);

            // Assign resource to booking 2
            var assign2 = new ResourceAssignment(tenantId, booking2.Id, resourceId, start2, end2, AssignmentRole.Lead);
            booking2.AddAssignment(assign2);
            var commit2 = new ResourceCommitment(tenantId, resourceId, assign2.Id, start2, end2, CommitmentKind.Direct);
            setupDb.ResourceCommitments.Add(commit2);

            await setupDb.SaveChangesAsync();
        }

        // Try to reschedule booking 2 into booking 1's window (09:30 - 11:30)
        using var rescheduleDb = new ServexaDbContext(CreateSqlServerOptions());
        var handler = new RescheduleBookingCommandHandler(
            new BookingRepository(rescheduleDb),
            new WorkOrderRepository(rescheduleDb),
            new SiteRepository(rescheduleDb),
            new AccountRepository(rescheduleDb),
            new ResourceRepository(rescheduleDb),
            new ResourceCommitmentRepository(rescheduleDb),
            new UnitOfWork(rescheduleDb),
            new TestTenantContext(tenantId, userId));

        var conflictStart = DateTime.UtcNow.Date.AddDays(3).AddHours(9).AddMinutes(30);
        var conflictEnd = conflictStart.AddHours(2);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new RescheduleBookingCommand(bkg2Id, conflictStart, conflictEnd, "Customer request morning"), CancellationToken.None));

        Assert.Contains("overlapping commitment", ex.Message);
    }

    [Fact]
    public async Task GlobalExceptionHandler_TranslatesDbUpdateConcurrencyException_ToHttp409ProblemDetails()
    {
        var httpContext = new DefaultHttpContext();
        var problemDetailsService = new TestProblemDetailsService();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, problemDetailsService);

        var concurrencyEx = new DbUpdateConcurrencyException("Store update, insert, or delete statement affected 0 rows.");
        var handled = await handler.TryHandleAsync(httpContext, concurrencyEx, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.NotNull(problemDetailsService.LastContext?.ProblemDetails);
        Assert.Equal("Concurrency Conflict", problemDetailsService.LastContext.ProblemDetails.Title);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetailsService.LastContext.ProblemDetails.Status);
    }

    private sealed class TestProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? LastContext { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            LastContext = context;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            LastContext = context;
            return ValueTask.CompletedTask;
        }
    }
}
