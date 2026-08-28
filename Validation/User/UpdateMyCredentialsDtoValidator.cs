using EduCore.Dtos.User;
using FluentValidation;

namespace EduCore.Validation.User;

public class UpdateMyCredentialsDtoValidator : AbstractValidator<UpdateMyCredentialsDto>
{
    public UpdateMyCredentialsDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}
