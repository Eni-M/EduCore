namespace EduCore.Dtos.Medical;

// Every field is optional: a school may know a child's allergies long before anything else, and
// the record is written a piece at a time. Identity (StudentId/SchoolId) is never on the body.
public record StudentMedicalRecordUpsertDto(
    string? BloodType,
    string? Allergies,
    string? ChronicConditions,
    string? Medications,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes);
