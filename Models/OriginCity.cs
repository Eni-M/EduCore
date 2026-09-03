namespace EduCore.Models;

// A city a student's family comes from, within a Country. Separate from the
// school-side City (which hangs off a State) so student origin and school
// location never share a table.
public class OriginCity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid CountryId { get; set; }

    public Country Country { get; set; } = null!;
}
