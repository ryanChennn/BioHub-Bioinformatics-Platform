using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class RenameSampleStorageTemperature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StorageTemprature",
                table: "Samples",
                newName: "StorageTemperature");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StorageTemperature",
                table: "Samples",
                newName: "StorageTemprature");
        }
    }
}
