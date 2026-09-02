using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class Communication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Audience = table.Column<int>(type: "integer", nullable: false),
                    ClassId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Classes_ClassId_SchoolId",
                        columns: x => new { x.ClassId, x.SchoolId },
                        principalTable: "Classes",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_Students_StudentId_SchoolId",
                        columns: x => new { x.StudentId, x.SchoolId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Pinned = table.Column<bool>(type: "boolean", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notices_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_ClassId_SchoolId",
                table: "CalendarEvents",
                columns: new[] { "ClassId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_SchoolId_Date",
                table: "CalendarEvents",
                columns: new[] { "SchoolId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_StudentId_SchoolId",
                table: "CalendarEvents",
                columns: new[] { "StudentId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notices_SchoolId_Pinned_CreatedAt",
                table: "Notices",
                columns: new[] { "SchoolId", "Pinned", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "Notices");
        }
    }
}
