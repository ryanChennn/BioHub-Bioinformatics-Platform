using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class AddSurvivalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SurvivalData",
                columns: table => new
                {
                    SurvivalDataID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectsID = table.Column<int>(type: "int", nullable: false),
                    OS_Month = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    OS_Status = table.Column<byte>(type: "tinyint", nullable: true),
                    PFS_Month = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    PFS_Status = table.Column<byte>(type: "tinyint", nullable: true),
                    LastFollowUpDate = table.Column<DateTime>(type: "date", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SurvivalData", x => x.SurvivalDataID);
                    table.ForeignKey(
                        name: "FK_SurvivalData_Subjects_SubjectsID",
                        column: x => x.SubjectsID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectsID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SurvivalData_SubjectsID",
                table: "SurvivalData",
                column: "SubjectsID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SurvivalData");
        }
    }
}
