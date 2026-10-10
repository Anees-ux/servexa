using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkOrderCompletionEvaluationAndScopeItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkOrderCompletionEvaluations",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    EvaluatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Outcome = table.Column<short>(type: "smallint", nullable: false),
                    GateResultsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PolicySnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggerBookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderCompletionEvaluations", x => x.Id);
                    table.UniqueConstraint("AK_WorkOrderCompletionEvaluations_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkOrderCompletionEvaluations_WorkOrders_TenantId_WorkOrderId",
                        columns: x => new { x.TenantId, x.WorkOrderId },
                        principalSchema: "service",
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderScopeItems",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ScopeType = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    IsRequiredForCompletion = table.Column<bool>(type: "bit", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FulfilledAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    FulfilledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderScopeItems", x => x.Id);
                    table.UniqueConstraint("AK_WorkOrderScopeItems_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkOrderScopeItems_WorkOrders_TenantId_WorkOrderId",
                        columns: x => new { x.TenantId, x.WorkOrderId },
                        principalSchema: "service",
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCompletionEvaluations_TenantId_CommandId",
                schema: "service",
                table: "WorkOrderCompletionEvaluations",
                columns: new[] { "TenantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderCompletionEvaluations_TenantId_WorkOrderId_EvaluatedAtUtc",
                schema: "service",
                table: "WorkOrderCompletionEvaluations",
                columns: new[] { "TenantId", "WorkOrderId", "EvaluatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderScopeItems_TenantId_AssetId",
                schema: "service",
                table: "WorkOrderScopeItems",
                columns: new[] { "TenantId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderScopeItems_TenantId_WorkOrderId_Sequence",
                schema: "service",
                table: "WorkOrderScopeItems",
                columns: new[] { "TenantId", "WorkOrderId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderScopeItems_TenantId_WorkOrderId_Status",
                schema: "service",
                table: "WorkOrderScopeItems",
                columns: new[] { "TenantId", "WorkOrderId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrderCompletionEvaluations",
                schema: "service");

            migrationBuilder.DropTable(
                name: "WorkOrderScopeItems",
                schema: "service");
        }
    }
}
