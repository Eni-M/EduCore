using EduCore.Enums;

namespace EduCore.Models;

public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public SchoolType Type { get; set; }

    public string? City { get; set; }
    public string? Zone { get; set; }

    public Guid? CityId { get; set; }

    public DateTime? DeactivatedAt { get; set; }
}
