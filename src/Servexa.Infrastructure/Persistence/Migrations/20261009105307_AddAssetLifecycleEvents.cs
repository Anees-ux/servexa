using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetLifecycleEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetLifecycleEvents",
                schema: "assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<short>(type: "smallint", nullable: false),
                    PreviousStatus = table.Column<short>(type: "smallint", nullable: true),
                    NewStatus = table.Column<short>(type: "smallint", nullable: true),
                    FromSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToSiteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FromOwnerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToOwnerAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2(3)", precision: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetLifecycleEvents_Assets_TenantId_AssetId",
                        columns: x => new { x.TenantId, x.AssetId },
                        principalSchema: "assets",
                        principalTable: "Assets",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetLifecycleEvents_TenantId_AssetId_OccurredAtUtc",
                schema: "assets",
                table: "AssetLifecycleEvents",
                columns: new[] { "TenantId", "AssetId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetLifecycleEvents",
                schema: "assets");
        }
    }
}
