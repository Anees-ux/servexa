using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingDispatchAndTechnicianExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "scheduling");

            migrationBuilder.EnsureSchema(
                name: "field");

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedStartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    PlannedEndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    SiteTimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "UTC"),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    DispatchStatus = table.Column<short>(type: "smallint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SchedulingNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.UniqueConstraint("AK_Bookings_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Bookings_Sites_TenantId_SiteId",
                        columns: x => new { x.TenantId, x.SiteId },
                        principalSchema: "customers",
                        principalTable: "Sites",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bookings_WorkOrders_TenantId_WorkOrderId",
                        columns: x => new { x.TenantId, x.WorkOrderId },
                        principalSchema: "service",
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionSessions",
                schema: "field",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    WorkSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionSessions", x => x.Id);
                    table.UniqueConstraint("AK_ExecutionSessions_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "ResourceCommitments",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitmentKind = table.Column<short>(type: "smallint", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    ReleaseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCommitments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResourceType = table.Column<short>(type: "smallint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HomeBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExclusiveCapacity = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceScheduleGuards",
                schema: "scheduling",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    TouchedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceScheduleGuards", x => new { x.TenantId, x.ResourceId });
                });

            migrationBuilder.CreateTable(
                name: "SchedulingConflictLogs",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptedStartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    AttemptedEndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    BlockingCommitmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConflictType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulingConflictLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BookingScheduleRevisions",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RevisionNo = table.Column<int>(type: "int", nullable: false),
                    PreviousStartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    PreviousEndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    NewStartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    NewEndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Initiator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingScheduleRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingScheduleRevisions_Bookings_TenantId_BookingId",
                        columns: x => new { x.TenantId, x.BookingId },
                        principalSchema: "scheduling",
                        principalTable: "Bookings",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingStatusHistories",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<short>(type: "smallint", nullable: false),
                    ToStatus = table.Column<short>(type: "smallint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Trigger = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_Bookings_TenantId_BookingId",
                        columns: x => new { x.TenantId, x.BookingId },
                        principalSchema: "scheduling",
                        principalTable: "Bookings",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceAssignments",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentRole = table.Column<short>(type: "smallint", nullable: false),
                    PlannedStartUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    PlannedEndUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    SelectionRationale = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DispatchedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceAssignments_Bookings_TenantId_BookingId",
                        columns: x => new { x.TenantId, x.BookingId },
                        principalSchema: "scheduling",
                        principalTable: "Bookings",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionIntervals",
                schema: "field",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntervalType = table.Column<short>(type: "smallint", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionIntervals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionIntervals_ExecutionSessions_TenantId_ExecutionSessionId",
                        columns: x => new { x.TenantId, x.ExecutionSessionId },
                        principalSchema: "field",
                        principalTable: "ExecutionSessions",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_BookingNumber",
                schema: "scheduling",
                table: "Bookings",
                columns: new[] { "TenantId", "BookingNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_SiteId_PlannedStartUtc",
                schema: "scheduling",
                table: "Bookings",
                columns: new[] { "TenantId", "SiteId", "PlannedStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_Status_PlannedStartUtc",
                schema: "scheduling",
                table: "Bookings",
                columns: new[] { "TenantId", "Status", "PlannedStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TenantId_WorkOrderId",
                schema: "scheduling",
                table: "Bookings",
                columns: new[] { "TenantId", "WorkOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingScheduleRevisions_TenantId_BookingId_RevisionNo",
                schema: "scheduling",
                table: "BookingScheduleRevisions",
                columns: new[] { "TenantId", "BookingId", "RevisionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_TenantId_BookingId_ChangedAtUtc",
                schema: "scheduling",
                table: "BookingStatusHistories",
                columns: new[] { "TenantId", "BookingId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionIntervals_TenantId_ExecutionSessionId_StartedAtUtc",
                schema: "field",
                table: "ExecutionIntervals",
                columns: new[] { "TenantId", "ExecutionSessionId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSessions_TenantId_BookingId",
                schema: "field",
                table: "ExecutionSessions",
                columns: new[] { "TenantId", "BookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSessions_TenantId_ResourceAssignmentId",
                schema: "field",
                table: "ExecutionSessions",
                columns: new[] { "TenantId", "ResourceAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSessions_TenantId_UserId_Status",
                schema: "field",
                table: "ExecutionSessions",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_TenantId_BookingId",
                schema: "scheduling",
                table: "ResourceAssignments",
                columns: new[] { "TenantId", "BookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_TenantId_ResourceId_Status_PlannedStartUtc",
                schema: "scheduling",
                table: "ResourceAssignments",
                columns: new[] { "TenantId", "ResourceId", "Status", "PlannedStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCommitments_TenantId_ResourceAssignmentId",
                schema: "scheduling",
                table: "ResourceCommitments",
                columns: new[] { "TenantId", "ResourceAssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCommitments_TenantId_ResourceId_Status_StartUtc_EndUtc",
                schema: "scheduling",
                table: "ResourceCommitments",
                columns: new[] { "TenantId", "ResourceId", "Status", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_ResourceCode",
                schema: "scheduling",
                table: "Resources",
                columns: new[] { "TenantId", "ResourceCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_Status_ResourceType",
                schema: "scheduling",
                table: "Resources",
                columns: new[] { "TenantId", "Status", "ResourceType" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_UserId",
                schema: "scheduling",
                table: "Resources",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchedulingConflictLogs_TenantId_ResourceId_AttemptedAtUtc",
                schema: "scheduling",
                table: "SchedulingConflictLogs",
                columns: new[] { "TenantId", "ResourceId", "AttemptedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingScheduleRevisions",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "BookingStatusHistories",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "ExecutionIntervals",
                schema: "field");

            migrationBuilder.DropTable(
                name: "ResourceAssignments",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "ResourceCommitments",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "Resources",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "ResourceScheduleGuards",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "SchedulingConflictLogs",
                schema: "scheduling");

            migrationBuilder.DropTable(
                name: "ExecutionSessions",
                schema: "field");

            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "scheduling");
        }
    }
}
