using EduCore.Enums;

namespace EduCore.Models;

public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public SchoolType Type { get; set; }

    public Guid? CityId { get; set; }
    public City? City { get; set; }

    public Guid? ZoneId { get; set; }
    public Zone? Zone { get; set; }

    public DateTime? DeactivatedAt { get; set; }
}
