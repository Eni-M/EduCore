using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolShifts", x => x.Id);
                    table.UniqueConstraint("AK_SchoolShifts_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_SchoolShifts_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolShiftId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodNumber = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftPeriods", x => x.Id);
                    table.UniqueConstraint("AK_ShiftPeriods_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_ShiftPeriods_SchoolShifts_SchoolShiftId_SchoolId",
                        columns: x => new { x.SchoolShiftId, x.SchoolId },
                        principalTable: "SchoolShifts",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_AcademicYears_AcademicYearId_SchoolId",
                        columns: x => new { x.AcademicYearId, x.SchoolId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_ShiftPeriods_ShiftPeriodId_SchoolId",
                        columns: x => new { x.ShiftPeriodId, x.SchoolId },
                        principalTable: "ShiftPeriods",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_Subjects_SubjectId_SchoolId",
                        columns: x => new { x.SubjectId, x.SchoolId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleEntries_TeacherSubjects_TeacherId_SubjectId_SchoolId",
                        columns: x => new { x.TeacherId, x.SubjectId, x.SchoolId },
                        principalTable: "TeacherSubjects",
                        principalColumns: new[] { "TeacherId", "SubjectId", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_AcademicYearId_SchoolId",
                table: "ScheduleEntries",
                columns: new[] { "AcademicYearId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_ClassId_SchoolId",
                table: "ScheduleEntries",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_SchoolId_AcademicYearId_ClassId_DayOfWeek_S~",
                table: "ScheduleEntries",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassId", "DayOfWeek", "ShiftPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_SchoolId_AcademicYearId_TeacherId_DayOfWeek~",
                table: "ScheduleEntries",
                columns: new[] { "SchoolId", "AcademicYearId", "TeacherId", "DayOfWeek", "ShiftPeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_ShiftPeriodId_SchoolId",
                table: "ScheduleEntries",
                columns: new[] { "ShiftPeriodId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_SubjectId_SchoolId",
                table: "ScheduleEntries",
                columns: new[] { "SubjectId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleEntries_TeacherId_SubjectId_SchoolId",
                table: "ScheduleEntries",
                columns: new[] { "TeacherId", "SubjectId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolShifts_SchoolId_Name",
                table: "SchoolShifts",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPeriods_SchoolShiftId_PeriodNumber",
                table: "ShiftPeriods",
                columns: new[] { "SchoolShiftId", "PeriodNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftPeriods_SchoolShiftId_SchoolId",
                table: "ShiftPeriods",
                columns: new[] { "SchoolShiftId", "SchoolId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleEntries");

            migrationBuilder.DropTable(
                name: "ShiftPeriods");

            migrationBuilder.DropTable(
                name: "SchoolShifts");
        }
    }
}
