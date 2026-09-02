using EduCore.Data;

namespace EduCore.Models;

// A school the student attended before, or between spells at, this one. The previous school is
// free text on purpose: it is usually not an EduCore school, so there is no row to point at.
// School-scoped like everything else -- the record belongs to the school that keeps it.
public class SchoolingHistory : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SchoolId { get; set; }
    public string PreviousSchoolName { get; set; } = null!;
    public string? City { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? GradeOrLevel { get; set; }
    public string? ExitReason { get; set; }
    public string? Notes { get; set; }
}
