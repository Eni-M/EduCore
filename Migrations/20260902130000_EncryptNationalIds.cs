using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class EncryptNationalIds : Migration
    {
        // Encrypted values are stored as "enc:v1:BASE64(iv||ciphertext)", which is longer than
        // the 50-character plaintext, so the two national-identifier columns are widened to 256.
        // Values written from now on are encrypted by the EF value converter; any rows that
        // predate this migration stay readable (the decryptor passes legacy plaintext through)
        // and become encrypted the next time they are saved.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PersonalNumber",
                table: "Students",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Guardians",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PersonalNumber",
                table: "Students",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "NationalId",
                table: "Guardians",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);
        }
    }
}
