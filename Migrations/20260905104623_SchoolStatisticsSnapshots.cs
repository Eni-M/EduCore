using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class SchoolStatisticsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolStatisticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    From = table.Column<DateOnly>(type: "date", nullable: false),
                    To = table.Column<DateOnly>(type: "date", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradingPeriodId = table.Column<Guid>(type: "uuid", nullable: true),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangesSinceComputed = table.Column<int>(type: "integer", nullable: false),
                    StudentsTotal = table.Column<int>(type: "integer", nullable: false),
                    StudentsActive = table.Column<int>(type: "integer", nullable: false),
                    StudentsGraduated = table.Column<int>(type: "integer", nullable: false),
                    StudentsNewArrivals = table.Column<int>(type: "integer", nullable: false),
                    StudentsFemale = table.Column<int>(type: "integer", nullable: false),
                    StudentsMale = table.Column<int>(type: "integer", nullable: false),
                    StudentsGenderNotRecorded = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentsActive = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentsStarted = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentsExited = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentsActiveYearAgo = table.Column<int>(type: "integer", nullable: false),
                    AttendanceRecords = table.Column<int>(type: "integer", nullable: false),
                    AttendancePresent = table.Column<int>(type: "integer", nullable: false),
                    AttendanceAbsent = table.Column<int>(type: "integer", nullable: false),
                    AttendanceExcusedAbsences = table.Column<int>(type: "integer", nullable: false),
                    AttendanceAverageBehaviorMark = table.Column<double>(type: "double precision", nullable: true),
                    AttendanceBehaviorMarkCount = table.Column<int>(type: "integer", nullable: false),
                    AcademicGradedMarks = table.Column<int>(type: "integer", nullable: false),
                    AcademicIncompleteMarks = table.Column<int>(type: "integer", nullable: false),
                    AcademicAverageFinalMark = table.Column<double>(type: "double precision", nullable: true),
                    AcademicPassed = table.Column<int>(type: "integer", nullable: false),
                    AcademicFailed = table.Column<int>(type: "integer", nullable: false),
                    AcademicAtRisk = table.Column<int>(type: "integer", nullable: false),
                    Teachers = table.Column<int>(type: "integer", nullable: false),
                    Classes = table.Column<int>(type: "integer", nullable: false),
                    TeachersWithWorkload = table.Column<int>(type: "integer", nullable: false),
                    WorkloadClassesSum = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolStatisticsSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolStatisticsSnapshots_SchoolId_From_To_AcademicYearId_G~",
                table: "SchoolStatisticsSnapshots",
                columns: new[] { "SchoolId", "From", "To", "AcademicYearId", "GradingPeriodId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolStatisticsSnapshots");
        }
    }
}
