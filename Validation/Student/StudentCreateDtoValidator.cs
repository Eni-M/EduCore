using EduCore.Dtos.Student;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.Student;

public class StudentCreateDtoValidator : AbstractValidator<StudentCreateDto>
{
    public StudentCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.PersonalNumber).NationalIdentifier();
        RuleFor(x => x.DateOfBirth).LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.Gender).IsInEnum();

        // Where the family comes from drives the demographics reporting, and a student
        // saved without it is a hole in every one of those figures that nothing goes back
        // to fill. Both halves or neither is not an option: a city without its country
        // cannot be resolved.
        RuleFor(x => x.CountryId).NotEmpty();
        RuleFor(x => x.OriginCityId).NotEmpty();
    }
}
