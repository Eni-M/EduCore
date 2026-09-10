using EduCore.Data;

namespace EduCore.Models;

public class Conversation : ISchoolScoped
{
    public Guid Id { get; set; }

    public Guid SchoolId { get; set; }

    public Guid StudentId { get; set; }

    public Guid GuardianId { get; set; }

    public Guid TeacherId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}