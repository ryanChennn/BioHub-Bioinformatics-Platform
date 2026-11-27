using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BIOHub.Migrations
{
    /// <inheritdoc />
    public partial class AlignBioHubSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalData_Subjects_SubjectsID",
                table: "ClinicalData");

            migrationBuilder.DropForeignKey(
                name: "FK_ResearchProjects_Users_UsersID",
                table: "ResearchProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Samples_Subjects_SubjectsID",
                table: "Samples");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_ResearchProjects_ResearchProjectsID",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Samples_SubjectsID",
                table: "Samples");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastAt",
                table: "Users",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<decimal>(
                name: "PSF_Month",
                table: "Subjects",
                type: "decimal(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.AlterColumn<byte>(
                name: "PFS_status",
                table: "Subjects",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "OS_status",
                table: "Subjects",
                type: "tinyint",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OS_Month",
                table: "Subjects",
                type: "decimal(6,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)");

            migrationBuilder.DropColumn(
                name: "Birth",
                table: "Subjects");

            // 建立新的 date Birth 欄位
            migrationBuilder.AddColumn<DateTime>(
                name: "Birth",
                table: "Subjects",
                type: "date",
                nullable: false);

            migrationBuilder.AlterColumn<int>(
                name: "Age",
                table: "Subjects",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Stage",
                table: "ClinicalData",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Samples_SubjectsID_SampleCode",
                table: "Samples",
                columns: new[] { "SubjectsID", "SampleCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalData_Subjects_SubjectsID",
                table: "ClinicalData",
                column: "SubjectsID",
                principalTable: "Subjects",
                principalColumn: "SubjectsID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResearchProjects_Users_UsersID",
                table: "ResearchProjects",
                column: "UsersID",
                principalTable: "Users",
                principalColumn: "UsersID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Samples_Subjects_SubjectsID",
                table: "Samples",
                column: "SubjectsID",
                principalTable: "Subjects",
                principalColumn: "SubjectsID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_ResearchProjects_ResearchProjectsID",
                table: "Subjects",
                column: "ResearchProjectsID",
                principalTable: "ResearchProjects",
                principalColumn: "ResearchProjectsID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalData_Subjects_SubjectsID",
                table: "ClinicalData");

            migrationBuilder.DropForeignKey(
                name: "FK_ResearchProjects_Users_UsersID",
                table: "ResearchProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Samples_Subjects_SubjectsID",
                table: "Samples");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_ResearchProjects_ResearchProjectsID",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Samples_SubjectsID_SampleCode",
                table: "Samples");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PSF_Month",
                table: "Subjects",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PFS_status",
                table: "Subjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OS_status",
                table: "Subjects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OS_Month",
                table: "Subjects",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldNullable: true);

            // 暫存出生年份
            migrationBuilder.AddColumn<int>(
                name: "Birth_Year",
                table: "Subjects",
                type: "int",
                nullable: true);

            // 將完整生日轉回年份
            migrationBuilder.Sql(
                "UPDATE [Subjects] " +
                "SET [Birth_Year] = DATEPART(year, [Birth]);");

            // 移除完整生日欄位
            migrationBuilder.DropColumn(
                name: "Birth",
                table: "Subjects");

            // 將暫存欄位改回 Birth
            migrationBuilder.RenameColumn(
                name: "Birth_Year",
                table: "Subjects",
                newName: "Birth");

            // 恢復舊欄位的 NOT NULL 設定
            migrationBuilder.AlterColumn<int>(
                name: "Birth",
                table: "Subjects",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Age",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "Stage",
                table: "ClinicalData",
                type: "int",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Samples_SubjectsID",
                table: "Samples",
                column: "SubjectsID");

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalData_Subjects_SubjectsID",
                table: "ClinicalData",
                column: "SubjectsID",
                principalTable: "Subjects",
                principalColumn: "SubjectsID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ResearchProjects_Users_UsersID",
                table: "ResearchProjects",
                column: "UsersID",
                principalTable: "Users",
                principalColumn: "UsersID");

            migrationBuilder.AddForeignKey(
                name: "FK_Samples_Subjects_SubjectsID",
                table: "Samples",
                column: "SubjectsID",
                principalTable: "Subjects",
                principalColumn: "SubjectsID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_ResearchProjects_ResearchProjectsID",
                table: "Subjects",
                column: "ResearchProjectsID",
                principalTable: "ResearchProjects",
                principalColumn: "ResearchProjectsID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
