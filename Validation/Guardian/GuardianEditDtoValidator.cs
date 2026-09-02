using EduCore.Dtos.Guardian;
using FluentValidation;

namespace EduCore.Validation.Guardian;

public class GuardianEditDtoValidator : AbstractValidator<GuardianEditDto>
{
    public GuardianEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
    }
}
