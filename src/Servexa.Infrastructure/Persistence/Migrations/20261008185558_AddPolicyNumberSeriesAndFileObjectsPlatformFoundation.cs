using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyNumberSeriesAndFileObjectsPlatformFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileObjects",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: true),
                    StorageKey = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    Classification = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Visibility = table.Column<short>(type: "smallint", nullable: false),
                    UploadStatus = table.Column<short>(type: "smallint", nullable: false),
                    ScanStatus = table.Column<short>(type: "smallint", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    RetentionState = table.Column<short>(type: "smallint", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileObjects", x => x.Id);
                    table.UniqueConstraint("AK_FileObjects_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_FileObjects_TenantUsers_TenantId_UploadedByUserId",
                        columns: x => new { x.TenantId, x.UploadedByUserId },
                        principalSchema: "platform",
                        principalTable: "TenantUsers",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FileObjects_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NumberSeries",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesKey = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ScopeKey = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    PrefixPattern = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    NextValue = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSeries", x => x.Id);
                    table.UniqueConstraint("AK_NumberSeries_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_NumberSeries_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PolicySettings",
                schema: "platform",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyKey = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ScopeType = table.Column<short>(type: "smallint", nullable: false),
                    ScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ValueJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EffectiveToUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicySettings", x => x.Id);
                    table.UniqueConstraint("AK_PolicySettings_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PolicySettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "platform",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_TenantId_OwnerType_OwnerId",
                schema: "platform",
                table: "FileObjects",
                columns: new[] { "TenantId", "OwnerType", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_TenantId_StorageKey",
                schema: "platform",
                table: "FileObjects",
                columns: new[] { "TenantId", "StorageKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileObjects_TenantId_UploadedByUserId",
                schema: "platform",
                table: "FileObjects",
                columns: new[] { "TenantId", "UploadedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberSeries_TenantId_SeriesKey_ScopeKey",
                schema: "platform",
                table: "NumberSeries",
                columns: new[] { "TenantId", "SeriesKey", "ScopeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicySettings_TenantId_PolicyKey_ScopeType_ScopeId_EffectiveFromUtc",
                schema: "platform",
                table: "PolicySettings",
                columns: new[] { "TenantId", "PolicyKey", "ScopeType", "ScopeId", "EffectiveFromUtc" },
                unique: true,
                filter: "[ScopeId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileObjects",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "NumberSeries",
                schema: "platform");

            migrationBuilder.DropTable(
                name: "PolicySettings",
                schema: "platform");
        }
    }
}
