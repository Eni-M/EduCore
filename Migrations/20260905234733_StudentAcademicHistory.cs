using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class StudentAcademicHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentAcademicRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcademicYearName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    ClassName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnrolledAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LeftAt = table.Column<DateOnly>(type: "date", nullable: true),
                    ExitReason = table.Column<int>(type: "integer", nullable: true),
                    CompiledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAcademicRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAcademicRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentAcademicRecordSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentAcademicRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AnnualMark = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAcademicRecordSubjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAcademicRecordSubjects_StudentAcademicRecords_Studen~",
                        column: x => x.StudentAcademicRecordId,
                        principalTable: "StudentAcademicRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicRecords_StudentId_EnrolledAt",
                table: "StudentAcademicRecords",
                columns: new[] { "StudentId", "EnrolledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicRecords_StudentId_SchoolId_AcademicYearId",
                table: "StudentAcademicRecords",
                columns: new[] { "StudentId", "SchoolId", "AcademicYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicRecordSubjects_StudentAcademicRecordId_Subje~",
                table: "StudentAcademicRecordSubjects",
                columns: new[] { "StudentAcademicRecordId", "SubjectName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAcademicRecordSubjects");

            migrationBuilder.DropTable(
                name: "StudentAcademicRecords");
        }
    }
}
