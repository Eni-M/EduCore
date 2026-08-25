using EduCore.Dtos.Admin;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.Admin;

public class AdminCreateDtoValidator : AbstractValidator<AdminCreateDto>
{
    public AdminCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.SchoolId).NotEmpty();
    }
}
