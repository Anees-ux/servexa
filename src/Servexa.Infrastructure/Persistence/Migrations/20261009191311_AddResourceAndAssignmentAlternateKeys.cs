using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servexa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceAndAssignmentAlternateKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Resources_TenantId_Id",
                schema: "scheduling",
                table: "Resources",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ResourceAssignments_TenantId_Id",
                schema: "scheduling",
                table: "ResourceAssignments",
                columns: new[] { "TenantId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "AK_Resources_TenantId_Id",
                schema: "scheduling",
                table: "Resources");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ResourceAssignments_TenantId_Id",
                schema: "scheduling",
                table: "ResourceAssignments");
        }
    }
}
