using EduCore.Dtos.Guardian;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.Guardian;

public class GuardianCreateDtoValidator : AbstractValidator<GuardianCreateDto>
{
    public GuardianCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.NationalId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.StudentIds).NotEmpty();
        RuleForEach(x => x.StudentIds).NotEmpty();
    }
}
