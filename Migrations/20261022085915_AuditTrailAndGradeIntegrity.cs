using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AuditTrailAndGradeIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WeightedValue",
                table: "Marks",
                type: "double precision",
                nullable: true);

            // Every mark recorded so far was weighted under the committed Grading weights, 40/60.
            // Freezing them here is what stops a later change to those weights reaching back.
            migrationBuilder.Sql(
                "UPDATE \"Marks\" SET \"WeightedValue\" = \"FirstMark\" * 0.4 + \"SecondMark\" * 0.6 " +
                "WHERE \"FirstMark\" IS NOT NULL AND \"SecondMark\" IS NOT NULL;");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "GradingPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedByUserId",
                table: "GradingPeriods",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Changes",
                table: "AuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarkChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OldFirstMark = table.Column<int>(type: "integer", nullable: true),
                    OldSecondMark = table.Column<int>(type: "integer", nullable: true),
                    NewFirstMark = table.Column<int>(type: "integer", nullable: true),
                    NewSecondMark = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AfterClose = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarkChanges_Marks_MarkId",
                        column: x => x.MarkId,
                        principalTable: "Marks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // NOT VALID: enforced on every insert and update from now on, but existing rows are not
            // re-checked. Older seed data holds marks on a 0-100 scale from before the 1-5
            // validator; validating them would fail this deploy. Fix those rows, then run
            //   ALTER TABLE "Marks" VALIDATE CONSTRAINT "CK_Marks_FirstMark"; (and the others)
            migrationBuilder.Sql(
                "ALTER TABLE \"Marks\" ADD CONSTRAINT \"CK_Marks_FirstMark\" " +
                "CHECK (\"FirstMark\" IS NULL OR \"FirstMark\" BETWEEN 1 AND 5) NOT VALID;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Marks\" ADD CONSTRAINT \"CK_Marks_SecondMark\" " +
                "CHECK (\"SecondMark\" IS NULL OR \"SecondMark\" BETWEEN 1 AND 5) NOT VALID;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Enrollments\" ADD CONSTRAINT \"CK_Enrollments_ExitReasonWithLeftAt\" " +
                "CHECK ((\"LeftAt\" IS NULL) = (\"ExitReason\" IS NULL)) NOT VALID;");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SchoolId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "SchoolId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MarkChanges_MarkId_ChangedAt",
                table: "MarkChanges",
                columns: new[] { "MarkId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkChanges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Marks_FirstMark",
                table: "Marks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Marks_SecondMark",
                table: "Marks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Enrollments_ExitReasonWithLeftAt",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_SchoolId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "WeightedValue",
                table: "Marks");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "GradingPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "GradingPeriods");

            migrationBuilder.DropColumn(
                name: "Changes",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AuditLogs");
        }
    }
}
