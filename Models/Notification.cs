using EduCore.Data;
using EduCore.Enums;

namespace EduCore.Models;

// An in-app notification shown on a role's dashboard within one school. Recipients are addressed
// by role (e.g. every Directorate user of the school), scoped by the usual school filter.
public class Notification : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public UserRole RecipientRole { get; set; }
    public string Message { get; set; } = null!;

    // Optional in-app path the dashboard links to, e.g. "/students/{id}".
    public string? Link { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}
