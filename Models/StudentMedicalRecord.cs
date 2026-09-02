using EduCore.Data;

namespace EduCore.Models;

// One row per student (unique on StudentId): the sensitive medical detail a school holds so it
// can keep a child safe. Who may read it is narrowed in AccessGuard, not by the school filter
// alone -- a subject teacher who teaches the class still cannot see it, only the homeroom one.
public class StudentMedicalRecord : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SchoolId { get; set; }
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? Medications { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? Notes { get; set; }
    public DateTime UpdatedAt { get; set; }
}
