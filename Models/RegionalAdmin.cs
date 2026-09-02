namespace EduCore.Models;

public class RegionalAdmin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CityId { get; set; }

    public User User { get; set; } = null!;
    public City City { get; set; } = null!;
}
