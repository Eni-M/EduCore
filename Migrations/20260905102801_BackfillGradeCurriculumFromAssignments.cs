using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduCore.Migrations
{
    /// <summary>
    /// Teaching assignments are now restricted to the subjects on the class grade's
    /// curriculum. Nothing has ever written <c>GradeCurricula</c>, so on every existing
    /// database that rule would reject every assignment, including the ones already in
    /// place. Seed each school's curriculum from what its classes are demonstrably already
    /// teaching, so the rule starts out satisfied by the current data and only constrains
    /// what happens next.
    /// </summary>
    public partial class BackfillGradeCurriculumFromAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ended assignments count too: a subject taught earlier in the year is still part
            // of that grade's curriculum, and excluding it would strand any attempt to
            // reassign a teacher to it.
            migrationBuilder.Sql(
                """
                INSERT INTO "GradeCurricula" ("Id", "SchoolId", "Grade", "SubjectId")
                SELECT gen_random_uuid(), cst."SchoolId", c."Grade", cst."SubjectId"
                FROM "ClassSubjectTeachers" cst
                JOIN "Classes" c ON c."Id" = cst."ClassId"
                GROUP BY cst."SchoolId", c."Grade", cst."SubjectId"
                ON CONFLICT DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately not reversible. The rows are indistinguishable from curriculum
            // entries a directorate added by hand after this migration ran, so deleting
            // them on a rollback would destroy real work.
        }
    }
}
