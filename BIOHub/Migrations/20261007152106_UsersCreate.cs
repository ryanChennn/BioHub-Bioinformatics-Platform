using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class UsersCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_ResearchProjectsID",
                table: "Subjects");

            migrationBuilder.AddColumn<int>(
                name: "UsersID",
                table: "ResearchProjects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UsersID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UsersID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ResearchProjectsID_SubjectCode",
                table: "Subjects",
                columns: new[] { "ResearchProjectsID", "SubjectCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProjects_ProjectCode",
                table: "ResearchProjects",
                column: "ProjectCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResearchProjects_UsersID",
                table: "ResearchProjects",
                column: "UsersID");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserName",
                table: "Users",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ResearchProjects_Users_UsersID",
                table: "ResearchProjects",
                column: "UsersID",
                principalTable: "Users",
                principalColumn: "UsersID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResearchProjects_Users_UsersID",
                table: "ResearchProjects");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_ResearchProjectsID_SubjectCode",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_ResearchProjects_ProjectCode",
                table: "ResearchProjects");

            migrationBuilder.DropIndex(
                name: "IX_ResearchProjects_UsersID",
                table: "ResearchProjects");

            migrationBuilder.DropColumn(
                name: "UsersID",
                table: "ResearchProjects");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ResearchProjectsID",
                table: "Subjects",
                column: "ResearchProjectsID");
        }
    }
}
