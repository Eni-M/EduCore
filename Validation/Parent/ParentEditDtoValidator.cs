using EduCore.Dtos.Parent;
using FluentValidation;

namespace EduCore.Validation.Parent;

public class ParentEditDtoValidator : AbstractValidator<ParentEditDto>
{
    public ParentEditDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
    }
}
