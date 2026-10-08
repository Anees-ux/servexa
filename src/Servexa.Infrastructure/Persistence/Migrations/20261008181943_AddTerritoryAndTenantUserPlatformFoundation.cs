using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTerritoryAndTenantUserPlatformFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantUsers",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalIssuer = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExternalSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "varchar(256)", unicode: false, maxLength: 256, nullable: true),
                    DefaultBranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => x.Id);
                    table.UniqueConstraint("AK_TenantUsers_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TenantUsers_Branches_TenantId_DefaultBranchId",
                        columns: x => new { x.TenantId, x.DefaultBranchId },
                        principalSchema: "platform",
                        principalTable: "Branches",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenantUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Territories",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentTerritoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Territories", x => x.Id);
                    table.UniqueConstraint("AK_Territories_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Territories_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Territories_Territories_TenantId_ParentTerritoryId",
                        columns: x => new { x.TenantId, x.ParentTerritoryId },
                        principalSchema: "platform",
                        principalTable: "Territories",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_DefaultBranchId",
                schema: "platform",
                table: "TenantUsers",
                columns: new[] { "TenantId", "DefaultBranchId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_ExternalIssuer_ExternalSubject",
                schema: "platform",
                table: "TenantUsers",
                columns: new[] { "TenantId", "ExternalIssuer", "ExternalSubject" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_TenantId_NormalizedEmail",
                schema: "platform",
                table: "TenantUsers",
                columns: new[] { "TenantId", "NormalizedEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_Territories_TenantId_Code",
                schema: "platform",
                table: "Territories",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Territories_TenantId_ParentTerritoryId",
                schema: "platform",
                table: "Territories",
                columns: new[] { "TenantId", "ParentTerritoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantUsers",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Territories",
                schema: "platform");
        }
    }
}
