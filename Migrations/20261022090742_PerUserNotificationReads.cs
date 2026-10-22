using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class PerUserNotificationReads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_SchoolId_RecipientRole_ReadAt",
                table: "Notifications");


            migrationBuilder.CreateTable(
                name: "NotificationReads",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientRole = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationReads", x => new { x.NotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_NotificationReads_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationReads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SchoolId_RecipientRole_CreatedAt",
                table: "Notifications",
                columns: new[] { "SchoolId", "RecipientRole", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationReads_UserId",
                table: "NotificationReads",
                column: "UserId");

            // Before this, one ReadAt per notification meant "someone in the role read it". Carry
            // that over as read for every current member of the role at that school, so nothing
            // already dealt with lights the bell up again. Roles: 0 Student, 2 Teacher, 3 Directorate.
            migrationBuilder.Sql(@"
INSERT INTO ""NotificationReads"" (""NotificationId"", ""UserId"", ""ReadAt"", ""SchoolId"", ""RecipientRole"")
SELECT n.""Id"", m.""UserId"", n.""ReadAt"", n.""SchoolId"", n.""RecipientRole""
FROM ""Notifications"" n
JOIN (
    SELECT ""UserId"", ""SchoolId"", 3 AS ""Role"" FROM ""Directorates""
    UNION ALL SELECT ""UserId"", ""SchoolId"", 2 FROM ""Teachers""
    UNION ALL SELECT ""UserId"", ""SchoolId"", 0 FROM ""Students""
) m ON m.""SchoolId"" = n.""SchoolId"" AND m.""Role"" = n.""RecipientRole""
WHERE n.""ReadAt"" IS NOT NULL
ON CONFLICT DO NOTHING;");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Notifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationReads");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_SchoolId_RecipientRole_CreatedAt",
                table: "Notifications");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SchoolId_RecipientRole_ReadAt",
                table: "Notifications",
                columns: new[] { "SchoolId", "RecipientRole", "ReadAt" });
        }
    }
}
