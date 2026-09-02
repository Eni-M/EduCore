using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class RoleHierarchyAndGuardians : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap existing user roles to the new hierarchy, high -> low so values never collide:
            //   SuperAdmin 3 -> StateAdmin 5, Admin 2 -> Directorate 3, Teacher 1 -> Teacher 2, Student 0 -> 0.
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = CASE \"Role\" " +
                "WHEN 3 THEN 5 WHEN 2 THEN 3 WHEN 1 THEN 2 ELSE \"Role\" END;");

            // Admin -> Directorate: rename the table in place so existing rows survive.
            migrationBuilder.RenameTable(name: "Admins", newName: "Directorates");
            migrationBuilder.RenameIndex(
                name: "IX_Admins_SchoolId", newName: "IX_Directorates_SchoolId", table: "Directorates");
            migrationBuilder.RenameIndex(
                name: "IX_Admins_UserId", newName: "IX_Directorates_UserId", table: "Directorates");
            migrationBuilder.Sql(
                "ALTER TABLE \"Directorates\" RENAME CONSTRAINT \"PK_Admins\" TO \"PK_Directorates\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Directorates\" RENAME CONSTRAINT \"FK_Admins_Schools_SchoolId\" " +
                "TO \"FK_Directorates_Schools_SchoolId\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Directorates\" RENAME CONSTRAINT \"FK_Admins_Users_UserId\" " +
                "TO \"FK_Directorates_Users_UserId\";");

            // The contact-only Parent record is replaced by the Guardian login entity + GuardianStudent
            // join. Existing Parent rows carry no login and cannot be migrated automatically; reseed
            // guardian data (Data/Seeds/00-full-dataset.sql) or recreate guardians via /api/guardians.
            migrationBuilder.DropTable(
                name: "Parents");

            migrationBuilder.CreateTable(
                name: "Guardians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guardians", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Guardians_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegionalAdmins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegionalAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegionalAdmins_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegionalAdmins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateAdmins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateAdmins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateAdmins_States_StateId",
                        column: x => x.StateId,
                        principalTable: "States",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateAdmins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuardianStudents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GuardianId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuardianStudents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuardianStudents_Guardians_GuardianId",
                        column: x => x.GuardianId,
                        principalTable: "Guardians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuardianStudents_Students_StudentId_SchoolId",
                        columns: x => new { x.StudentId, x.SchoolId },
                        principalTable: "Students",
                        principalColumns: new[] { "Id", "SchoolId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_UserId",
                table: "Guardians",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardianStudents_GuardianId_StudentId",
                table: "GuardianStudents",
                columns: new[] { "GuardianId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuardianStudents_StudentId_SchoolId",
                table: "GuardianStudents",
                columns: new[] { "StudentId", "SchoolId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegionalAdmins_CityId",
                table: "RegionalAdmins",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_RegionalAdmins_UserId",
                table: "RegionalAdmins",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateAdmins_StateId",
                table: "StateAdmins",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_StateAdmins_UserId",
                table: "StateAdmins",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuardianStudents");

            migrationBuilder.DropTable(
                name: "RegionalAdmins");

            migrationBuilder.DropTable(
                name: "StateAdmins");

            migrationBuilder.DropTable(
                name: "Guardians");

            migrationBuilder.Sql(
                "ALTER TABLE \"Directorates\" RENAME CONSTRAINT \"PK_Directorates\" TO \"PK_Admins\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Directorates\" RENAME CONSTRAINT \"FK_Directorates_Schools_SchoolId\" " +
                "TO \"FK_Admins_Schools_SchoolId\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Directorates\" RENAME CONSTRAINT \"FK_Directorates_Users_UserId\" " +
                "TO \"FK_Admins_Users_UserId\";");
            migrationBuilder.RenameIndex(
                name: "IX_Directorates_SchoolId", newName: "IX_Admins_SchoolId", table: "Directorates");
            migrationBuilder.RenameIndex(
                name: "IX_Directorates_UserId", newName: "IX_Admins_UserId", table: "Directorates");
            migrationBuilder.RenameTable(name: "Directorates", newName: "Admins");

            migrationBuilder.CreateTable(
                name: "Parents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_Parents_StudentId_SchoolId",
                table: "Parents",
                columns: new[] { "StudentId", "SchoolId" });

            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"Role\" = CASE \"Role\" " +
                "WHEN 5 THEN 3 WHEN 3 THEN 2 WHEN 2 THEN 1 ELSE \"Role\" END;");
        }
    }
}
