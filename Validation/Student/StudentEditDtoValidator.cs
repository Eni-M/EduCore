using EduCore.Dtos.Student;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.Student;

public class StudentEditDtoValidator : AbstractValidator<StudentEditDto>
{
    public StudentEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.PersonalNumber).NationalIdentifier();
        RuleFor(x => x.DateOfBirth).LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Gender).IsInEnum();

        // Deliberately NOT required here, unlike on create: 5009 of the 5010 students already
        // in the database have no origin recorded, and requiring it on edit would make every
        // one of them unsavable until someone knew a value to put in. Backfilling those is a
        // data job, not a validation one.
    }
}
