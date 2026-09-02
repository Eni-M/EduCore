using EduCore.Dtos.StateAdmin;
using FluentValidation;

namespace EduCore.Validation.StateAdmin;

public class StateAdminEditDtoValidator : AbstractValidator<StateAdminEditDto>
{
    public StateAdminEditDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.StateId).NotEmpty();
    }
}
