using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                    table.UniqueConstraint("AK_AcademicYears_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_AcademicYears_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.UniqueConstraint("AK_Subjects_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_Subjects_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Admins_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    PersonalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GraduationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.UniqueConstraint("AK_Students_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_Students_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.UniqueConstraint("AK_Teachers_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_Teachers_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Teachers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradingPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingPeriods", x => x.Id);
                    table.UniqueConstraint("AK_GradingPeriods_Id_AcademicYearId_SchoolId", x => new { x.Id, x.AcademicYearId, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_GradingPeriods_AcademicYears_AcademicYearId_SchoolId",
                        columns: x => new { x.AcademicYearId, x.SchoolId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parents_Students_StudentId_SchoolId",
                        columns: x => new { x.StudentId, x.SchoolId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    Grade = table.Column<int>(type: "integer", nullable: false),
                    HomeroomTeacherId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                    table.UniqueConstraint("AK_Classes_Id_AcademicYearId", x => new { x.Id, x.AcademicYearId });
                    table.UniqueConstraint("AK_Classes_Id_SchoolId", x => new { x.Id, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_Classes_AcademicYears_AcademicYearId_SchoolId",
                        columns: x => new { x.AcademicYearId, x.SchoolId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Classes_Teachers_HomeroomTeacherId",
                        column: x => x.HomeroomTeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeacherSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSubjects", x => x.Id);
                    table.UniqueConstraint("AK_TeacherSubjects_TeacherId_SubjectId_SchoolId", x => new { x.TeacherId, x.SubjectId, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Subjects_SubjectId_SchoolId",
                        columns: x => new { x.SubjectId, x.SchoolId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSubjects_Teachers_TeacherId_SchoolId",
                        columns: x => new { x.TeacherId, x.SchoolId },
                        principalTable: "Teachers",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledAt = table.Column<DateOnly>(type: "date", nullable: false),
                    LeftAt = table.Column<DateOnly>(type: "date", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.UniqueConstraint("AK_Enrollments_Id_ClassId", x => new { x.Id, x.ClassId });
                    table.ForeignKey(
                        name: "FK_Enrollments_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentId_SchoolId",
                        columns: x => new { x.StudentId, x.SchoolId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassSubjectTeachers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSubjectTeachers", x => x.Id);
                    table.UniqueConstraint("AK_ClassSubjectTeachers_Id_ClassId_SubjectId_TeacherId_SchoolId", x => new { x.Id, x.ClassId, x.SubjectId, x.TeacherId, x.SchoolId });
                    table.ForeignKey(
                        name: "FK_ClassSubjectTeachers_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassSubjectTeachers_TeacherSubjects_TeacherId_SubjectId_Sc~",
                        columns: x => new { x.TeacherId, x.SubjectId, x.SchoolId },
                        principalTable: "TeacherSubjects",
                        principalColumns: new[] { "TeacherId", "SubjectId", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiaryEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    Topic = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiaryEntries", x => x.Id);
                    table.UniqueConstraint("AK_DiaryEntries_Id_ClassId", x => new { x.Id, x.ClassId });
                    table.ForeignKey(
                        name: "FK_DiaryEntries_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiaryEntries_Subjects_SubjectId_SchoolId",
                        columns: x => new { x.SubjectId, x.SchoolId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiaryEntries_TeacherSubjects_TeacherId_SubjectId_SchoolId",
                        columns: x => new { x.TeacherId, x.SubjectId, x.SchoolId },
                        principalTable: "TeacherSubjects",
                        principalColumns: new[] { "TeacherId", "SubjectId", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Marks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSubjectTeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marks_ClassSubjectTeachers_ClassSubjectTeacherId_ClassId_Su~",
                        columns: x => new { x.ClassSubjectTeacherId, x.ClassId, x.SubjectId, x.TeacherId, x.SchoolId },
                        principalTable: "ClassSubjectTeachers",
                        principalColumns: new[] { "Id", "ClassId", "SubjectId", "TeacherId", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_Classes_ClassId_AcademicYearId",
                        columns: x => new { x.ClassId, x.AcademicYearId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "AcademicYearId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_Enrollments_EnrollmentId_ClassId",
                        columns: x => new { x.EnrollmentId, x.ClassId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "Id", "ClassId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Marks_GradingPeriods_GradingPeriodId_AcademicYearId_SchoolId",
                        columns: x => new { x.GradingPeriodId, x.AcademicYearId, x.SchoolId },
                        principalTable: "GradingPeriods",
                        principalColumns: new[] { "Id", "AcademicYearId", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DiaryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPresent = table.Column<bool>(type: "boolean", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BehaviorMark = table.Column<int>(type: "integer", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attendances_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_DiaryEntries_DiaryEntryId_ClassId",
                        columns: x => new { x.DiaryEntryId, x.ClassId },
                        principalTable: "DiaryEntries",
                        principalColumns: new[] { "Id", "ClassId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Enrollments_EnrollmentId_ClassId",
                        columns: x => new { x.EnrollmentId, x.ClassId },
                        principalTable: "Enrollments",
                        principalColumns: new[] { "Id", "ClassId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_SchoolId_Name",
                table: "AcademicYears",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admins_SchoolId",
                table: "Admins",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Admins_UserId",
                table: "Admins",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ClassId_SchoolId",
                table: "Attendances",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_DiaryEntryId_ClassId",
                table: "Attendances",
                columns: new[] { "DiaryEntryId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_DiaryEntryId_EnrollmentId",
                table: "Attendances",
                columns: new[] { "DiaryEntryId", "EnrollmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EnrollmentId_ClassId",
                table: "Attendances",
                columns: new[] { "EnrollmentId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_AcademicYearId_SchoolId",
                table: "Classes",
                columns: new[] { "AcademicYearId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_HomeroomTeacherId",
                table: "Classes",
                column: "HomeroomTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_SchoolId_AcademicYearId_Name",
                table: "Classes",
                columns: new[] { "SchoolId", "AcademicYearId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjectTeachers_ClassId_SchoolId",
                table: "ClassSubjectTeachers",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjectTeachers_ClassId_SubjectId",
                table: "ClassSubjectTeachers",
                columns: new[] { "ClassId", "SubjectId" },
                unique: true,
                filter: "\"EndedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSubjectTeachers_TeacherId_SubjectId_SchoolId",
                table: "ClassSubjectTeachers",
                columns: new[] { "TeacherId", "SubjectId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_ClassId_Date_Period",
                table: "DiaryEntries",
                columns: new[] { "ClassId", "Date", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_ClassId_SchoolId",
                table: "DiaryEntries",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_SubjectId_SchoolId",
                table: "DiaryEntries",
                columns: new[] { "SubjectId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_DiaryEntries_TeacherId_SubjectId_SchoolId",
                table: "DiaryEntries",
                columns: new[] { "TeacherId", "SubjectId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ClassId_SchoolId",
                table: "Enrollments",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_ClassId",
                table: "Enrollments",
                columns: new[] { "StudentId", "ClassId" },
                unique: true,
                filter: "\"LeftAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_SchoolId",
                table: "Enrollments",
                columns: new[] { "StudentId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradingPeriods_AcademicYearId_Name",
                table: "GradingPeriods",
                columns: new[] { "AcademicYearId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingPeriods_AcademicYearId_SchoolId",
                table: "GradingPeriods",
                columns: new[] { "AcademicYearId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_ClassId_AcademicYearId",
                table: "Marks",
                columns: new[] { "ClassId", "AcademicYearId" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_ClassId_SchoolId",
                table: "Marks",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_ClassSubjectTeacherId_ClassId_SubjectId_TeacherId_Sch~",
                table: "Marks",
                columns: new[] { "ClassSubjectTeacherId", "ClassId", "SubjectId", "TeacherId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_EnrollmentId_ClassId",
                table: "Marks",
                columns: new[] { "EnrollmentId", "ClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_GradingPeriodId_AcademicYearId_SchoolId",
                table: "Marks",
                columns: new[] { "GradingPeriodId", "AcademicYearId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Parents_StudentId_SchoolId",
                table: "Parents",
                columns: new[] { "StudentId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Students_PersonalNumber",
                table: "Students",
                column: "PersonalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_SchoolId",
                table: "Students",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_UserId",
                table: "Students",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SchoolId_Name",
                table: "Subjects",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_SchoolId",
                table: "Teachers",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_UserId",
                table: "Teachers",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_SubjectId_SchoolId",
                table: "TeacherSubjects",
                columns: new[] { "SubjectId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSubjects_TeacherId_SchoolId",
                table: "TeacherSubjects",
                columns: new[] { "TeacherId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Marks");

            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.DropTable(
                name: "DiaryEntries");

            migrationBuilder.DropTable(
                name: "ClassSubjectTeachers");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "GradingPeriods");

            migrationBuilder.DropTable(
                name: "TeacherSubjects");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
