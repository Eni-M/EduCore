using EduCore.Dtos.Medical;
using FluentValidation;

namespace EduCore.Validation.Medical;

public class StudentMedicalRecordUpsertDtoValidator : AbstractValidator<StudentMedicalRecordUpsertDto>
{
    public StudentMedicalRecordUpsertDtoValidator()
    {
        RuleFor(x => x.BloodType).MaximumLength(10);
        RuleFor(x => x.Allergies).MaximumLength(2000);
        RuleFor(x => x.ChronicConditions).MaximumLength(2000);
        RuleFor(x => x.Medications).MaximumLength(2000);
        RuleFor(x => x.EmergencyContactName).MaximumLength(200);
        RuleFor(x => x.EmergencyContactPhone).MaximumLength(30);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
