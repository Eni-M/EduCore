using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <inheritdoc />
    public partial class AddClassShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "Classes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ShiftId",
                table: "Classes",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_SchoolShifts_ShiftId",
                table: "Classes",
                column: "ShiftId",
                principalTable: "SchoolShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_SchoolShifts_ShiftId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_ShiftId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Classes");
        }
    }
}
