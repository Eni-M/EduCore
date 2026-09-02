namespace EduCore.Dtos.Medical;

public record StudentMedicalRecordDto(
    Guid StudentId,
    string? BloodType,
    string? Allergies,
    string? ChronicConditions,
    string? Medications,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    DateTime? UpdatedAt);
