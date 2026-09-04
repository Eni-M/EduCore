using EduCore.Data;

namespace EduCore.Models;

// Which subjects a given grade is taught in a school. Drives the elementary homeroom
// teacher's automatic subject assignments: an elementary teacher teaches every subject
// in their class's grade curriculum, unless a specialist has taken one over.
public class GradeCurriculum : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public int Grade { get; set; }
    public Guid SubjectId { get; set; }

    public Subject Subject { get; set; } = null!;
}
