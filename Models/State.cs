namespace EduCore.Models;

// Reference data shared by every school, so deliberately NOT ISchoolScoped: a place exists
// independently of who happens to be looking at it, and two schools in the same city must
// resolve to the same row or nothing can be grouped by location.
//
// Maintained by a SuperAdmin. The point of holding these as rows rather than as free text is
// that "Prishtina", "prishtina" and "Prishtinë" would otherwise be three different places in
// every demographic report.
public class State
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    public ICollection<City> Cities { get; set; } = new List<City>();
}
