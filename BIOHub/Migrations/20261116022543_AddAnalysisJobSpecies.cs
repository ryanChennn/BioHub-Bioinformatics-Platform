using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisJobSpecies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Species",
                table: "AnalysisJob",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Species",
                table: "AnalysisJob");
        }
    }
}
