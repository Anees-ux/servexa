using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRegistryAndWorkOrderFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "assets");

            migrationBuilder.EnsureSchema(
                name: "service");

            migrationBuilder.CreateTable(
                name: "EquipmentModels",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManufacturerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ModelCode = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CategoryCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TrackingPolicy = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentModels", x => x.Id);
                    table.UniqueConstraint("AK_EquipmentModels_TenantId_Id", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ServiceRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServiceAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillToAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimarySiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkTypeCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Priority = table.Column<short>(type: "smallint", nullable: false),
                    OperationalStatus = table.Column<short>(type: "smallint", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PauseReasonCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    PauseNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillToSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OperationallyCompletedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.Id);
                    table.UniqueConstraint("AK_WorkOrders_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WorkOrders_Accounts_TenantId_BillToAccountId",
                        columns: x => new { x.TenantId, x.BillToAccountId },
                        principalSchema: "customers",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Accounts_TenantId_ServiceAccountId",
                        columns: x => new { x.TenantId, x.ServiceAccountId },
                        principalSchema: "customers",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrders_Sites_TenantId_PrimarySiteId",
                        columns: x => new { x.TenantId, x.PrimarySiteId },
                        principalSchema: "customers",
                        principalTable: "Sites",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    EquipmentModelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SerialNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    CurrentSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentOwnerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    InstalledAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    DecommissionedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.UniqueConstraint("AK_Assets_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Assets_Accounts_TenantId_CurrentOwnerAccountId",
                        columns: x => new { x.TenantId, x.CurrentOwnerAccountId },
                        principalSchema: "customers",
                        principalTable: "Accounts",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_EquipmentModels_TenantId_EquipmentModelId",
                        columns: x => new { x.TenantId, x.EquipmentModelId },
                        principalSchema: "assets",
                        principalTable: "EquipmentModels",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_Sites_TenantId_CurrentSiteId",
                        columns: x => new { x.TenantId, x.CurrentSiteId },
                        principalSchema: "customers",
                        principalTable: "Sites",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderStatusHistory",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<short>(type: "smallint", nullable: false),
                    ToStatus = table.Column<short>(type: "smallint", nullable: false),
                    PauseReasonCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderStatusHistory_WorkOrders_TenantId_WorkOrderId",
                        columns: x => new { x.TenantId, x.WorkOrderId },
                        principalSchema: "service",
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderAssets",
                schema: "service",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
                    SiteIdAtTime = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssets_Assets_TenantId_AssetId",
                        columns: x => new { x.TenantId, x.AssetId },
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrderAssets_WorkOrders_TenantId_WorkOrderId",
                        columns: x => new { x.TenantId, x.WorkOrderId },
                        principalSchema: "service",
                        principalTable: "WorkOrders",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_AssetNumber",
                schema: "assets",
                table: "Assets",
                columns: new[] { "TenantId", "AssetNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_CurrentOwnerAccountId",
                schema: "assets",
                table: "Assets",
                columns: new[] { "TenantId", "CurrentOwnerAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_CurrentSiteId",
                schema: "assets",
                table: "Assets",
                columns: new[] { "TenantId", "CurrentSiteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_EquipmentModelId",
                schema: "assets",
                table: "Assets",
                columns: new[] { "TenantId", "EquipmentModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_Status",
                schema: "assets",
                table: "Assets",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentModels_TenantId_CategoryCode",
                schema: "assets",
                table: "EquipmentModels",
                columns: new[] { "TenantId", "CategoryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentModels_TenantId_ManufacturerName_ModelCode",
                schema: "assets",
                table: "EquipmentModels",
                columns: new[] { "TenantId", "ManufacturerName", "ModelCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentModels_TenantId_Status",
                schema: "assets",
                table: "EquipmentModels",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssets_TenantId_AssetId",
                schema: "service",
                table: "WorkOrderAssets",
                columns: new[] { "TenantId", "AssetId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderAssets_TenantId_WorkOrderId_AssetId",
                schema: "service",
                table: "WorkOrderAssets",
                columns: new[] { "TenantId", "WorkOrderId", "AssetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_BillToAccountId",
                schema: "service",
                table: "WorkOrders",
                columns: new[] { "TenantId", "BillToAccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_OperationalStatus_Priority_CreatedAtUtc",
                schema: "service",
                table: "WorkOrders",
                columns: new[] { "TenantId", "OperationalStatus", "Priority", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_PrimarySiteId_OperationalStatus",
                schema: "service",
                table: "WorkOrders",
                columns: new[] { "TenantId", "PrimarySiteId", "OperationalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_ServiceAccountId_OperationalStatus",
                schema: "service",
                table: "WorkOrders",
                columns: new[] { "TenantId", "ServiceAccountId", "OperationalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrders_TenantId_WorkOrderNumber",
                schema: "service",
                table: "WorkOrders",
                columns: new[] { "TenantId", "WorkOrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderStatusHistory_TenantId_WorkOrderId_ChangedAtUtc",
                schema: "service",
                table: "WorkOrderStatusHistory",
                columns: new[] { "TenantId", "WorkOrderId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkOrderAssets",
                schema: "service");

            migrationBuilder.DropTable(
                name: "WorkOrderStatusHistory",
                schema: "service");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "assets");

            migrationBuilder.DropTable(
                name: "WorkOrders",
                schema: "service");

            migrationBuilder.DropTable(
                name: "EquipmentModels",
                schema: "assets");
        }
    }
}
