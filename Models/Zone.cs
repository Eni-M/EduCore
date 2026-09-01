namespace EduCore.Models;

public class Zone
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid CityId { get; set; }

    public City City { get; set; } = null!;
}
