using EduCore.Dtos.Directorate;
using FluentValidation;

namespace EduCore.Validation.Directorate;

public class DirectorateEditDtoValidator : AbstractValidator<DirectorateEditDto>
{
    public DirectorateEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.SchoolId).NotEmpty();
    }
}
