using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSubjectTeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.UniqueConstraint("AK_Assignments_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_Assignments_ClassSubjectTeachers_ClassSubjectTeacherId_Clas~",
                        columns: x => new { x.ClassSubjectTeacherId, x.ClassId, x.SubjectId, x.TeacherId, x.SchoolId },
                        principalTable: "ClassSubjectTeachers",
                        principalColumns: new[] { "Id", "ClassId", "SubjectId", "TeacherId", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    StorageKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<decimal>(type: "numeric", nullable: true),
                    Feedback = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentSubmissions_Assignments_AssignmentId_SchoolId",
                        columns: x => new { x.AssignmentId, x.SchoolId },
                        principalTable: "Assignments",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignmentSubmissions_Students_StudentId_SchoolId",
                        columns: x => new { x.StudentId, x.SchoolId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_ClassSubjectTeacherId_ClassId_SubjectId_Teacher~",
                table: "Assignments",
                columns: new[] { "ClassSubjectTeacherId", "ClassId", "SubjectId", "TeacherId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_SchoolId_ClassId_SubjectId_DueDate",
                table: "Assignments",
                columns: new[] { "SchoolId", "ClassId", "SubjectId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_SchoolId",
                table: "AssignmentSubmissions",
                columns: new[] { "AssignmentId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_AssignmentId_StudentId",
                table: "AssignmentSubmissions",
                columns: new[] { "AssignmentId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_SchoolId_StudentId",
                table: "AssignmentSubmissions",
                columns: new[] { "SchoolId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSubmissions_StudentId_SchoolId",
                table: "AssignmentSubmissions",
                columns: new[] { "StudentId", "SchoolId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentSubmissions");

            migrationBuilder.DropTable(
                name: "Assignments");
        }
    }
}
