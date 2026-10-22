using EduCore.Data;

namespace EduCore.Models;

// One row per change to a recorded mark: who, when, from what, to what, and -- required once the
// term is closed -- why. A mark is a legal record; overwriting it in place left no trace of what
// it used to be. The first entry of a mark is not a change and has no row here.
public class MarkChange : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid MarkId { get; set; }
    public Guid SchoolId { get; set; }

    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; }

    public int? OldFirstMark { get; set; }
    public int? OldSecondMark { get; set; }
    public int? NewFirstMark { get; set; }
    public int? NewSecondMark { get; set; }

    public string? Reason { get; set; }

    // Whether the term was already closed when the change was made -- the changes an inspector
    // asks about first.
    public bool AfterClose { get; set; }
}
