using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResearchProjects",
                columns: table => new
                {
                    ResearchProjectsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrincipalInvestigator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Institution = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchProjects", x => x.ResearchProjectsID);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    SubjectsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResearchProjectsID = table.Column<int>(type: "int", nullable: false),
                    SubjectCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Age = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Birth = table.Column<int>(type: "int", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OS_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OS_Month = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    PFS_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PSF_Month = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.SubjectsID);
                    table.ForeignKey(
                        name: "FK_Subjects_ResearchProjects_ResearchProjectsID",
                        column: x => x.ResearchProjectsID,
                        principalTable: "ResearchProjects",
                        principalColumn: "ResearchProjectsID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalData",
                columns: table => new
                {
                    ClinicalDataID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectsID = table.Column<int>(type: "int", nullable: false),
                    Diagnosis = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Stage = table.Column<int>(type: "int", maxLength: 50, nullable: false),
                    Treatments = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Responses = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreateBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalData", x => x.ClinicalDataID);
                    table.ForeignKey(
                        name: "FK_ClinicalData_Subjects_SubjectsID",
                        column: x => x.SubjectsID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectsID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Samples",
                columns: table => new
                {
                    SamplesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectsID = table.Column<int>(type: "int", nullable: false),
                    SampleCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StorageTemprature = table.Column<decimal>(type: "decimal(5,1)", nullable: false),
                    SampleStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Samples", x => x.SamplesID);
                    table.ForeignKey(
                        name: "FK_Samples_Subjects_SubjectsID",
                        column: x => x.SubjectsID,
                        principalTable: "Subjects",
                        principalColumn: "SubjectsID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalData_SubjectsID",
                table: "ClinicalData",
                column: "SubjectsID");

            migrationBuilder.CreateIndex(
                name: "IX_Samples_SubjectsID",
                table: "Samples",
                column: "SubjectsID");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ResearchProjectsID",
                table: "Subjects",
                column: "ResearchProjectsID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalData");

            migrationBuilder.DropTable(
                name: "Samples");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "ResearchProjects");
        }
    }
}
