using EduCore.Dtos.Admin;
using FluentValidation;

namespace EduCore.Validation.Admin;

public class AdminEditDtoValidator : AbstractValidator<AdminEditDto>
{
    public AdminEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.SchoolId).NotEmpty();
    }
}
