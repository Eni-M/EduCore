namespace EduCore.Models;

public class StateAdmin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StateId { get; set; }

    public User User { get; set; } = null!;
    public State State { get; set; } = null!;
}
