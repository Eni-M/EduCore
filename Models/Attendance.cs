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

    public bool IsPresent { get; set; }
    public string? Comment { get; set; }
    public int? BehaviorMark { get; set; }
}
