using EduCore.Data;

namespace EduCore.Models;

public class GradingPeriod : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Once a term is closed its marks are the record: teachers can no longer change them, and the
    // school office can only with a written reason, kept in MarkChange. Null while open.
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedByUserId { get; set; }

    public bool IsClosed => ClosedAt is not null;
}
