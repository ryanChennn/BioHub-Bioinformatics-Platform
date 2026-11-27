using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class AddRNAseqAnalysisTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysisJob",
                columns: table => new
                {
                    AnalysisJobID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchProjectsID = table.Column<int>(type: "int", nullable: false),
                    UsersID = table.Column<int>(type: "int", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnalysisMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ControlGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TreatmentGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinishAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisJob", x => x.AnalysisJobID);
                    table.ForeignKey(
                        name: "FK_AnalysisJob_ResearchProjects_ResearchProjectsID",
                        column: x => x.ResearchProjectsID,
                        principalTable: "ResearchProjects",
                        principalColumn: "ResearchProjectsID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisJob_Users_UsersID",
                        column: x => x.UsersID,
                        principalTable: "Users",
                        principalColumn: "UsersID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisFiles",
                columns: table => new
                {
                    AalysisFileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisJobID = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisFiles", x => x.AalysisFileID);
                    table.ForeignKey(
                        name: "FK_AnalysisFiles_AnalysisJob_AnalysisJobID",
                        column: x => x.AnalysisJobID,
                        principalTable: "AnalysisJob",
                        principalColumn: "AnalysisJobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisResults",
                columns: table => new
                {
                    AnalysisResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnalysisJobID = table.Column<int>(type: "int", nullable: false),
                    ResultFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VolcanPlotPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalGenes = table.Column<int>(type: "int", nullable: true),
                    SignificantGenes = table.Column<int>(type: "int", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisResults", x => x.AnalysisResultID);
                    table.ForeignKey(
                        name: "FK_AnalysisResults_AnalysisJob_AnalysisJobID",
                        column: x => x.AnalysisJobID,
                        principalTable: "AnalysisJob",
                        principalColumn: "AnalysisJobID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisFiles_AnalysisJobID",
                table: "AnalysisFiles",
                column: "AnalysisJobID");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJob_ResearchProjectsID",
                table: "AnalysisJob",
                column: "ResearchProjectsID");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisJob_UsersID",
                table: "AnalysisJob",
                column: "UsersID");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisResults_AnalysisJobID",
                table: "AnalysisResults",
                column: "AnalysisJobID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisFiles");

            migrationBuilder.DropTable(
                name: "AnalysisResults");

            migrationBuilder.DropTable(
                name: "AnalysisJob");
        }
    }
}
