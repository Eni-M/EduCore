using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class StatisticsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Marks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Marks");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Marks");

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Students",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Schools",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zone",
                table: "Schools",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstMark",
                table: "Marks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondMark",
                table: "Marks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExitReason",
                table: "Enrollments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Classes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExcused",
                table: "Attendances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Schools_City_Zone",
                table: "Schools",
                columns: new[] { "City", "Zone" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_EnrollmentId_SubjectId_GradingPeriodId",
                table: "Marks",
                columns: new[] { "EnrollmentId", "SubjectId", "GradingPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marks_SchoolId_GradingPeriodId_SubjectId",
                table: "Marks",
                columns: new[] { "SchoolId", "GradingPeriodId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_TeacherId_GradingPeriodId",
                table: "Marks",
                columns: new[] { "TeacherId", "GradingPeriodId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_SchoolId_ClassId_LeftAt",
                table: "Enrollments",
                columns: new[] { "SchoolId", "ClassId", "LeftAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_SchoolId_Date",
                table: "DiaryEntries",
                columns: new[] { "SchoolId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EnrollmentId",
                table: "Attendances",
                column: "EnrollmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schools_City_Zone",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Marks_EnrollmentId_SubjectId_GradingPeriodId",
                table: "Marks");

            migrationBuilder.DropIndex(
                name: "IX_Marks_SchoolId_GradingPeriodId_SubjectId",
                table: "Marks");

            migrationBuilder.DropIndex(
                name: "IX_Marks_TeacherId_GradingPeriodId",
                table: "Marks");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_SchoolId_ClassId_LeftAt",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_DiaryEntries_SchoolId_Date",
                table: "DiaryEntries");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EnrollmentId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Zone",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "FirstMark",
                table: "Marks");

            migrationBuilder.DropColumn(
                name: "SecondMark",
                table: "Marks");

            migrationBuilder.DropColumn(
                name: "ExitReason",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "IsExcused",
                table: "Attendances");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Marks",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Marks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "Marks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
