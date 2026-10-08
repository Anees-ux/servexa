using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationAndScopedGrantsPlatformFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.UniqueConstraint("AK_Roles_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Roles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoleAssignments",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAssignments", x => x.Id);
                    table.UniqueConstraint("AK_RoleAssignments_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_RoleAssignments_Roles_TenantId_RoleId",
                        columns: x => new { x.TenantId, x.RoleId },
                        principalSchema: "platform",
                        principalTable: "Roles",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAssignments_TenantUsers_TenantId_UserId",
                        columns: x => new { x.TenantId, x.UserId },
                        principalSchema: "platform",
                        principalTable: "TenantUsers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "platform",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "platform",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "platform",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScopeAssignments",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<short>(type: "smallint", nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScopeAssignments_RoleAssignments_TenantId_RoleAssignmentId",
                        columns: x => new { x.TenantId, x.RoleAssignmentId },
                        principalSchema: "platform",
                        principalTable: "RoleAssignments",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScopeAssignments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                schema: "platform",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_TenantId_RoleId",
                schema: "platform",
                table: "RoleAssignments",
                columns: new[] { "TenantId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_TenantId_UserId_RoleId",
                schema: "platform",
                table: "RoleAssignments",
                columns: new[] { "TenantId", "UserId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "platform",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId_NormalizedName",
                schema: "platform",
                table: "Roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScopeAssignments_TenantId_RoleAssignmentId_ScopeType_ScopeId",
                schema: "platform",
                table: "ScopeAssignments",
                columns: new[] { "TenantId", "RoleAssignmentId", "ScopeType", "ScopeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "ScopeAssignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "RoleAssignments",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "platform");
        }
    }
}
