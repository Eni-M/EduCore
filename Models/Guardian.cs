namespace EduCore.Models;

public class Guardian
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PhoneNumber { get; set; } = null!;

    public User User { get; set; } = null!;
    public ICollection<GuardianStudent> Children { get; set; } = new List<GuardianStudent>();
}
