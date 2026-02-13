using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentManagementAPI.Migrations.TenantDb
{
    /// <inheritdoc />
    public partial class v : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedSupportUserId",
                table: "Incidents",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedSupportUserId",
                table: "Incidents");
        }
    }
}
