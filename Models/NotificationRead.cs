using EduCore.Enums;

namespace EduCore.Models;

// One user having read one notification. Notifications are addressed to a role, but reading one
// is personal: a single shared ReadAt meant one office user clearing the bell cleared it for the
// whole office. SchoolId and RecipientRole are copied from the notification so the bell's
// stream for that role and school can be woken without a lookup.
public class NotificationRead
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public DateTime ReadAt { get; set; }

    public Guid SchoolId { get; set; }
    public UserRole RecipientRole { get; set; }
}
