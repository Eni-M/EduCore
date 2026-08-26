using EduCore.Data;

namespace EduCore.Models;

public class Attendance : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid DiaryEntryId { get; set; }
    public Guid EnrollmentId { get; set; }

    // Denormalized from both DiaryEntry and Enrollment. Its only purpose is letting the DB
    // enforce -- via composite foreign keys -- that the enrollment's class actually matches
    // the diary entry's class, which two independent plain FKs can't express.
    public Guid ClassId { get; set; }
    public Guid SchoolId { get; set; }

    // Set only through Record, so "excused presence" is not a state this object can hold.
    // The rule lives here rather than in a validator or a check constraint: an invariant that
    // the type itself guarantees cannot be forgotten at one of the call sites.
    public bool IsPresent { get; private set; } = true;
    public bool IsExcused { get; private set; }

    public string? Comment { get; set; }
    public int? BehaviorMark { get; set; }

    // An excuse only means something for an absence, so it is dropped for a present student
    // rather than stored as a contradiction.
    public void Record(bool isPresent, bool isExcused)
    {
        IsPresent = isPresent;
        IsExcused = !isPresent && isExcused;
    }
}
