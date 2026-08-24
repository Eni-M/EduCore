using EduCore.Dtos.Parent;
using FluentValidation;

namespace EduCore.Validation.Parent;

public class ParentCreateDtoValidator : AbstractValidator<ParentCreateDto>
{
    public ParentCreateDtoValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
    }
}
