using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkTasks",
                schema: "field",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderScopeItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    TaskType = table.Column<short>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Gate = table.Column<short>(type: "smallint", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SkipReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.Id);
                    table.UniqueConstraint("AK_WorkTasks_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkTasks_WorkOrders_TenantId_WorkOrderId",
                        columns: x => new { x.TenantId, x.WorkOrderId },
                        principalSchema: "service",
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_TenantId_AssetId",
                schema: "field",
                table: "WorkTasks",
                columns: new[] { "TenantId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_TenantId_AssignmentId",
                schema: "field",
                table: "WorkTasks",
                columns: new[] { "TenantId", "AssignmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_TenantId_WorkOrderId_Sequence",
                schema: "field",
                table: "WorkTasks",
                columns: new[] { "TenantId", "WorkOrderId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_TenantId_WorkOrderId_Status",
                schema: "field",
                table: "WorkTasks",
                columns: new[] { "TenantId", "WorkOrderId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkTasks",
                schema: "field");
        }
    }
}
