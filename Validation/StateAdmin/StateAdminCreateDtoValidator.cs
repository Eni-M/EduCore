using EduCore.Dtos.StateAdmin;
using EduCore.Validation;
using FluentValidation;

namespace EduCore.Validation.StateAdmin;

public class StateAdminCreateDtoValidator : AbstractValidator<StateAdminCreateDto>
{
    public StateAdminCreateDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.StateId).NotEmpty();
    }
}
