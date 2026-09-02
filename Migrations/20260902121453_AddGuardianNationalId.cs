using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddGuardianNationalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NationalId",
                table: "Guardians",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Give any pre-existing guardians a unique placeholder so the unique index below
            // does not collide on the empty default. They can be corrected via the guardian editor.
            migrationBuilder.Sql(
                "UPDATE \"Guardians\" SET \"NationalId\" = LEFT(REPLACE(\"Id\"::text, '-', ''), 50) " +
                "WHERE \"NationalId\" = '';");

            migrationBuilder.CreateIndex(
                name: "IX_Guardians_NationalId",
                table: "Guardians",
                column: "NationalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Guardians_NationalId",
                table: "Guardians");

            migrationBuilder.DropColumn(
                name: "NationalId",
                table: "Guardians");
        }
    }
}
